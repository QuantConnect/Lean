/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.PsychSignalDataConverter
{
    /// <summary>
    /// Handles the conversion of data from raw PsychSignal data into a format usable by LEAN
    /// </summary>
    public class PsychSignalDataConverter : IDisposable
    {
        private Dictionary<string, TickerData> _fileHandles;
        private MapFileResolver _mapFileResolver;
        private readonly DirectoryInfo _rawSourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;

        /// <summary>
        /// Converts psychsignal raw data into a format usable by Lean
        /// </summary>
        /// <param name="sourceDirectory">Directory to source our raw data from</param>
        /// <param name="destinationDirectory">Directory to write formatted data to</param>
        public PsychSignalDataConverter(string sourceDirectory, string destinationDirectory)
        {
            _rawSourceDirectory = new DirectoryInfo(sourceDirectory);
            _destinationDirectory = new DirectoryInfo(destinationDirectory);

            _fileHandles = new Dictionary<string, TickerData>();
            _mapFileResolver = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"))
                .Get(Market.USA);

            _destinationDirectory.Create();
        }

        /// <summary>
        /// Converts a specific file to Lean alternative data format. Note that you must flush
        /// after you're done converting a file to ensure that all data gets written to disk.
        /// You can do that by calling <see cref="Dispose"/> once you've finished processing
        ///
        /// Note: Assumes that it will be given files in ascending order by date
        /// </summary>
        /// <param name="sourceFilePath">File to process and convert</param>
        public void Convert(Stream stream)
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("PsychSignalDataConverter has already been disposed");
            }

            var previousTicker = string.Empty;
            var currentLineCount = 0;

            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    currentLineCount++;

                    var csv = line.Split(',');
                    var ticker = csv[1].ToLowerInvariant();
                    DateTime timestamp;

                    if (csv[0] == "SOURCE")
                    {
                        Log.Trace($"PsychSignalDataConverter.Convert(): Skipping line {currentLineCount} - Line contains header information");
                        continue;
                    }
                    if (!DateTime.TryParseExact(csv[2], @"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out timestamp))
                    {
                        Log.Trace($"PsychSignalDataConverter.Convert(): Skipping line {currentLineCount} - Failed to parse date properly");
                        continue;
                    }
                    if (!_mapFileResolver.ResolveMapFile(ticker, timestamp).Any())
                    {
                        // Because all tickers are all clustered together, we can detect
                        // duplicate messages and prevent ourselves from spamming the status log
                        if (ticker != previousTicker)
                        {
                            Log.Trace($"PsychSignalDataDownloader.Convert(): Skipping line {currentLineCount} - Could not resolve map file for ticker {ticker}");
                        }
                        previousTicker = ticker;
                        continue;
                    }

                    TickerData handle;
                    if (!_fileHandles.TryGetValue(ticker, out handle))
                    {
                        handle = new TickerData(ticker, timestamp.Date, _destinationDirectory);
                        _fileHandles[ticker] = handle;
                    }

                    handle.Append(timestamp, csv);
                    previousTicker = ticker;
                }
            }
        }

        /// <summary>
        /// Filter raw files to be inclusive within the date range specified and converts those files.
        /// </summary>
        /// <param name="startDateUtc">Date to start parsing files from</param>
        /// <param name="endDateUtc">Date to stop parsing files</param>
        /// <remarks>Inclusive on the lower bound, and exclusive on the upper bound</remarks>
        public void ConvertFrom(DateTime startDateUtc, DateTime endDateUtc)
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("PsychSignalDataConverter has already been disposed");
            }

            // Filter for files by name and bounds, then order by date
            var files = _rawSourceDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly)
                .Where(
                    x =>
                    {
                        DateTime fileDate;
                        if (!DateTime.TryParseExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate))
                        {
                            return false;
                        }

                        return fileDate >= startDateUtc && fileDate < endDateUtc;
                    }
                )
                .OrderBy(x => Parse.DateTimeExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", DateTimeStyles.AdjustToUniversal))
                .ToList();

            var fileCount = files.Count();
            var i = 0;

            foreach (var rawFile in files)
            {
                i++;
                Log.Trace($"PsychSignalDataConverter.ConvertFrom(): Reading file {rawFile.Name} (file {i}/{fileCount})");
                Log.Trace($"PsychSignalDataConverter.ConvertFrom(): Begin converting {rawFile.Name}");

                using (var stream = rawFile.OpenRead())
                {
                    Convert(stream);
                }

                Log.Trace($"PsychSignalDataConverter.Convert(): Finished converting {rawFile.Name}");
            }

            Dispose();
        }

        /// <summary>
        /// Converts a single day's data. We start processing data
        /// from the 0th hour of the day and finish at the 23rd hour of the day
        /// </summary>
        /// <param name="date">Date to convert files for</param>
        public void ConvertDate(DateTime date)
        {
            ConvertFrom(date.Date, date.Date.AddDays(1));
        }

        /// <summary>
        /// Converts gzipped backfill data. Reads all *.gz files in the raw directory and attempts to convert them
        /// </summary>
        public void ConvertHistoricalData()
        {
            foreach (var archive in _rawSourceDirectory.GetFiles("*.gz", SearchOption.TopDirectoryOnly))
            {
                Log.Trace($"PsychSignalDataConverter.ConvertBackfill(): Begin converting historical data for file: {archive.FullName}");
                using (var archiveStream = StreamProvider.ForExtension(".gz").Open(archive.FullName).First())
                {
                    Convert(archiveStream);
                }

                Log.Trace($"PsychSignalDataConverter.ConvertBackfill(): Finished converting historical data for file: {archive.FullName}");
            }

            Dispose();
        }

        /// <summary>
        /// Iterate over the data, processing each data point before finally zipping the directories
        /// </summary>
        public void ConvertDirectory()
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("PsychSignalDataConverter has already been disposed");
            }

            // Filter for raw data with file names formatted as "yyyyMMdd_HH.*"
            // `GetDirectory()` doesn't guarantee file order, so we must order it manually ourselves
            var files = _rawSourceDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly)
                .Where(
                    x =>
                    {
                        DateTime fileDate;
                        return DateTime.TryParseExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out fileDate);
                    }
                )
                .OrderBy(x => Parse.DateTimeExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", DateTimeStyles.AdjustToUniversal))
                .ToList();

            var i = 0;
            var fileCount = files.Count();

            foreach (var rawFile in files)
            {
                i++;
                Log.Trace($"PsychSignalDataConverter.ConvertFrom(): Reading file {rawFile.Name} (file {i}/{fileCount})");
                Log.Trace($"PsychSignalDataConverter.ConvertDirectory(): Begin converting {rawFile.Name}");

                using (var stream = rawFile.OpenRead())
                {
                    Convert(stream);
                }

                Log.Trace("PsychSignalDataConverter.ConvertDirectory(): Finished converting {rawFile.Name}");
            }

            Dispose();
        }

        /// <summary>
        /// Utility method to compresses the data contained with the psychsignal alternative data folder
        /// to a structure similar to equity minute files (e.g. /[symbol]/20010101.zip#20010101.csv)
        /// </summary>
        private void CompressData()
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("PsychSignalDataConverter has already been disposed");
            }

            Log.Trace("PsychSignalDataConverter.CompressData(): Begin compressing PsychSignal data");
            var timer = Stopwatch.StartNew();

            foreach (var tickerFolder in _destinationDirectory.GetDirectories())
            {
                foreach (var dataFile in tickerFolder.GetFiles("*.csv", SearchOption.TopDirectoryOnly))
                {
                    Compression.Zip(dataFile.FullName, deleteOriginal: true);
                }
            }

            timer.Stop();
            Log.Trace($"PsychSignalDataConverter.CompressData(): Finished compressing PsychSignal data in {timer.Elapsed.TotalSeconds} seconds");
        }

        /// <summary>
        /// Handle to a file so that we don't have to open and close it every time we want
        /// to write to a file. This helps us speed up time spent processing massively.
        /// </summary>
        private class TickerData
        {
            private readonly DirectoryInfo _destinationDirectory;
            private readonly string _ticker;

            private StreamWriter _writer;
            private string _tempPath;
            private DateTime _date;

            /// <summary>
            /// Windows filesystem forbids the following names as directory or file names
            /// </summary>
            private readonly List<string> _forbiddenTickers = new List<string>
            {
                "con",
                "prn",
                "aux",
                "nul",
                "com1",
                "com2",
                "com3",
                "com4",
                "com5",
                "com6",
                "com7",
                "com8",
                "com9",
                "lpt1",
                "lpt2",
                "lpt3",
                "lpt4",
                "lpt5",
                "lpt6",
                "lpt7",
                "lpt8",
                "lpt9"
            };

            /// <summary>
            /// Creates writer instances and saves file path.
            /// Used to keep file open until the path changes for the symbol
            /// </summary>
            /// <param name="ticker"></param>
            /// <param name="date"></param>
            /// <param name="destinationDirectory"></param>
            public TickerData(string ticker, DateTime date, DirectoryInfo destinationDirectory)
            {
                _date = date;
                _ticker = ticker;
                // Using win32 Path.GetTempFileName can cause filename collisions
                _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                _writer = new StreamWriter(_tempPath);
                _destinationDirectory = destinationDirectory;

                if (OS.IsWindows && _forbiddenTickers.Contains(_ticker))
                {
                    _ticker += "_";
                }
            }

            /// <summary>
            /// Adds a new line to the writer
            /// </summary>
            /// <param name="timestamp">Event timestamp in UTC</param>
            /// <param name="csv">CSV enumerable</param>
            public void Append(DateTime timestamp, IEnumerable<string> csv)
            {
                var date = timestamp.Date;

                if (date != _date)
                {
                    Flush();

                    _date = date;
                    _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    _writer = new StreamWriter(_tempPath);
                }

                // Ignore the first three columns of the CSV line because we already
                // know the "source[0]", know the "symbol[1]", and have already parsed the "timestamp[2]"
                _writer.WriteLine(ToCsv(timestamp, csv.Skip(3)));
            }

            /// <summary>
            /// Moves the temporary file containing data to the final path, deleting
            /// any existing file to avoid conflicts when moving
            /// </summary>
            private void MoveTempFile()
            {
                var tickerDirectory = Path.Combine(_destinationDirectory.FullName, _ticker);
                var writePath = Path.Combine(tickerDirectory, $"{_date:yyyyMMdd}.csv");

                Directory.CreateDirectory(tickerDirectory);

                // We only want the latest version of the data
                if (File.Exists(writePath))
                {
                    File.Delete(writePath);
                    Log.Trace($"PsychSignalDataConverter.TickerData.MoveTempFile(): Deleted existing file: {writePath}");
                }

                File.Move(_tempPath, writePath);
            }

            /// <summary>
            /// Converts line of psychsignal data to LEAN's csv format
            /// </summary>
            /// <param name="timestamp">Timestamp as a string to use for filename</param>
            /// <param name="csvData">Data as it comes from data vendor</param>
            /// <returns>CSV formatted string</returns>
            private string ToCsv(DateTime timestamp, IEnumerable<string> csvData)
            {
                // SOURCE[0],SYMBOL[1],TIMESTAMP_UTC[2],BULLISH_INTENSITY[3],BEARISH_INTENSITY[4],BULL_MINUS_BEAR[5],BULL_SCORED_MESSAGES[6],BEAR_SCORED_MESSAGES[7],BULL_BEAR_MSG_RATIO[8],TOTAL_SCANNED_MESSAGES[9]
                // We should have skipped the first three entries, so our real starting index is "BULLISH_INTENSITY[3]"
                return $"{timestamp.TimeOfDay.TotalMilliseconds},{string.Join(",", csvData)}";
            }

            /// <summary>
            /// Flushes and closes the underlying <see cref="StreamWriter"/>
            /// and moves the temp file to its final path
            /// </summary>
            public void Flush()
            {
                _writer.Flush();
                _writer.Close();

                MoveTempFile();
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false;

        /// <summary>
        /// Disposes the object. Any additional calls to any method will yield an <see cref="ObjectDisposedException" />
        /// </summary>
        /// <param name="disposing">Flag to indicate whether we are disposing the object</param>
        public void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    var count = _fileHandles.Count;
                    var percentage = 0.05m;
                    var fivePercent = Math.Ceiling(count * percentage);
                    var step = 1;
                    var i = 1;

                    // Flush each handle to ensure all data gets written
                    foreach (var handle in _fileHandles.Values)
                    {
                        if (i % fivePercent == 0m || count == i)
                        {
                            Log.Trace($"PsychSignalDataConverter.Dispose(): Flushing {percentage * step * 100}% complete");
                            step++;
                        }

                        handle.Flush();
                        i++;
                    }
                    CompressData();
                }

                _fileHandles = null;
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Default dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
