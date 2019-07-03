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
using System.Globalization;
using System.IO;
using System.Linq;
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
        private HashSet<string> _knownTickers;
        private readonly DirectoryInfo _rawSourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;

        /// <summary>
        /// Converts psychsignal raw data into a format usable by Lean
        /// </summary>
        /// <param name="sourceDirectory">Directory to source our raw data from</param>
        /// <param name="destinationDirectory">Directory to write formatted data to</param>
        /// <param name="knownTickerFolder">Directory where we source the ticker list</param>
        public PsychSignalDataConverter(string sourceDirectory, string destinationDirectory, string knownTickerFolder)
        {
            _rawSourceDirectory = new DirectoryInfo(sourceDirectory);
            _destinationDirectory = new DirectoryInfo(destinationDirectory);

            _fileHandles = new Dictionary<string, TickerData>();
            _knownTickers = Directory.GetDirectories(knownTickerFolder)
                .Select(Path.GetFileName)
                .ToHashSet();

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
        public void Convert(FileInfo sourceFilePath)
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("PsychSignalDataConverter has already been disposed");
            }

            Log.Trace($"PsychSignalDataConverter.Convert(): Begin converting {sourceFilePath.FullName}");
            var file = File.ReadLines(sourceFilePath.FullName);

            foreach (var line in file)
            {
                var csv = line.Split(',');
                var ticker = csv[1].ToLower();
                DateTime timestamp;

                if (csv[0] == "SOURCE" ||
                    !_knownTickers.Contains(ticker) ||
                    !DateTime.TryParseExact(csv[2], @"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out timestamp))
                {
                    continue;
                }

                TickerData handle;
                if (!_fileHandles.TryGetValue(ticker, out handle))
                {
                    handle = new TickerData(ticker, timestamp.Date, _destinationDirectory);
                    _fileHandles[ticker] = handle;
                }
                
                handle.Append(timestamp, csv);
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
                .OrderBy(x => DateTime.ParseExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));

            foreach (var rawFile in files)
            {
                Convert(rawFile);
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
                .OrderBy(x => DateTime.ParseExact(x.Name.Substring(0, 11), "yyyyMMdd_HH", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal));
            
            foreach (var rawFile in files)
            {
                Convert(rawFile);
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

            foreach (var tickerFolder in _destinationDirectory.GetDirectories())
            {
                foreach (var dataFile in tickerFolder.GetFiles("*.csv", SearchOption.TopDirectoryOnly))
                {
                    Compression.Zip(dataFile.FullName, deleteOriginal: true);
                    Log.Trace($"PsychSignalDataConverter.CompressData(): Successfully compressed: {tickerFolder}/{dataFile}");
                }
            }
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
            private List<string> _forbiddenTickers = new List<string>
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
                
                // Ignore the first three entries of the CSV line
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
                Log.Trace($"PsychSignalDataConverter.TickerData.MoveTempFile(): Finished writing file: {writePath}");
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
        /// Disposes the object
        /// </summary>
        /// <param name="disposing">Flag to indicate whether we are disposing the object</param>
        public virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Flush each handle to ensure all data gets written
                    foreach (var handle in _fileHandles.Values)
                    {
                        handle.Flush();
                    }
                    CompressData();
                }

                _fileHandles = null;
                _knownTickers = null;

                _disposedValue = true;
            }
        }
        
        /// <summary>
        /// Default Dispose pattern
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
