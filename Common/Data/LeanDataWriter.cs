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
using System.IO;
using Ionic.Zip;
using System.Linq;
using System.Text;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace QuantConnect.Data
{
    /// <summary>
    /// Data writer for saving an IEnumerable of BaseData into the LEAN data directory.
    /// </summary>
    public class LeanDataWriter
    {
        private readonly Symbol _symbol;
        private readonly string _dataDirectory;
        private readonly TickType _tickType;
        private readonly bool _appendToZips;
        private readonly Resolution _resolution;
        private readonly SecurityType _securityType;
        private readonly IDataCacheProvider _dataCacheProvider;

        /// <summary>
        /// Create a new lean data writer to this base data directory.
        /// </summary>
        /// <param name="symbol">Symbol string</param>
        /// <param name="dataDirectory">Base data directory</param>
        /// <param name="resolution">Resolution of the desired output data</param>
        /// <param name="tickType">The tick type</param>
        public LeanDataWriter(Resolution resolution, Symbol symbol, string dataDirectory, TickType tickType = TickType.Trade, IDataCacheProvider dataCacheProvider = null) : this(
            dataDirectory,
            resolution,
            symbol.ID.SecurityType,
            tickType,
            dataCacheProvider
        )
        {
            _symbol = symbol;
            // All fx data is quote data.
            if (_securityType == SecurityType.Forex || _securityType == SecurityType.Cfd)
            {
                _tickType = TickType.Quote;
            }

            if (_securityType != SecurityType.Equity && _securityType != SecurityType.Forex && _securityType != SecurityType.Cfd && _securityType != SecurityType.Crypto && _securityType != SecurityType.Future && _securityType != SecurityType.Option && _securityType != SecurityType.FutureOption && _securityType != SecurityType.Index && _securityType != SecurityType.IndexOption)
            {
                throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }
        }

        /// <summary>
        /// Create a new lean data writer to this base data directory.
        /// </summary>
        /// <param name="dataDirectory">Base data directory</param>
        /// <param name="resolution">Resolution of the desired output data</param>
        /// <param name="securityType">The security type</param>
        /// <param name="tickType">The tick type</param>
        public LeanDataWriter(string dataDirectory, Resolution resolution, SecurityType securityType, TickType tickType, IDataCacheProvider dataCacheProvider = null)
        {
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _securityType = securityType;
            _tickType = tickType;
            _appendToZips = securityType == SecurityType.Future || securityType.IsOption();
            _dataCacheProvider = dataCacheProvider ?? new DiskDataCacheProvider();
        }

        /// <summary>
        /// Given the constructor parameters, write out the data in LEAN format.
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        public void Write(IEnumerable<BaseData> source)
        {
            var lastTime = DateTime.MinValue;
            var outputFile = string.Empty;
            var currentFileDictionary = new SortedDictionary<DateTime, string>();
            var writeTasks = new Queue<Task>();

            foreach (var data in source)
            {
                // Ensure the data is sorted as a safety check
                if (data.Time < lastTime) throw new Exception("The data must be pre-sorted from oldest to newest");

                // Update our output file
                // Only do this on date change, because we know we don't have a any data zips smaller than a day, saves time
                if (data.Time.Date != lastTime.Date)
                {
                    // Get the latest file name, if it has changed, we have entered a new file, write our current data to file
                    var latestOutputFile = GetZipOutputFileName(_dataDirectory, data.Time);
                    if (outputFile.IsNullOrEmpty() || outputFile != latestOutputFile)
                    {
                        if (!currentFileDictionary.IsNullOrEmpty())
                        {
                            // Launch a write task for the current file and data set
                            var file = outputFile;
                            var dictionary = currentFileDictionary;
                            writeTasks.Enqueue(Task.Run(() =>
                            {
                                WriteFile(file, dictionary, data.Time);
                            }));
                        }

                        // Reset our dictionary and store new output file
                        currentFileDictionary = new SortedDictionary<DateTime, string>();
                        outputFile = latestOutputFile;
                    }
                }

                // Add data to our current dictionary
                var line = LeanData.GenerateLine(data, _securityType, _resolution);
                currentFileDictionary.Add(data.Time, line);

                // Update our time
                lastTime = data.Time;
            }

            // Finish off my processing the last file as well
            if (!currentFileDictionary.IsNullOrEmpty())
            {
                writeTasks.Enqueue(Task.Run(() =>
                {
                    WriteFile(outputFile, currentFileDictionary, lastTime);
                }));
            }

            // Wait for all our write tasks to finish
            while (writeTasks.Count > 0)
            {
                var task = writeTasks.Dequeue();
                task.Wait();
            }
        }

        /// <summary>
        /// Downloads historical data from the brokerage and saves it in LEAN format.
        /// </summary>
        /// <param name="brokerage">The brokerage from where to fetch the data</param>
        /// <param name="symbols">The list of symbols</param>
        /// <param name="startTimeUtc">The starting date/time (UTC)</param>
        /// <param name="endTimeUtc">The ending date/time (UTC)</param>
        public void DownloadAndSave(IBrokerage brokerage, List<Symbol> symbols, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            if (symbols.Count == 0)
            {
                throw new ArgumentException("DownloadAndSave(): The symbol list cannot be empty.");
            }

            if (_tickType != TickType.Trade && _tickType != TickType.Quote)
            {
                throw new ArgumentException("DownloadAndSave(): The tick type must be Trade or Quote.");
            }

            if (_securityType != SecurityType.Future && _securityType != SecurityType.Option && _securityType != SecurityType.FutureOption)
            {
                throw new ArgumentException($"DownloadAndSave(): The security type must be {SecurityType.Future} or {SecurityType.Option}.");
            }

            if (symbols.Any(x => x.SecurityType != _securityType))
            {
                throw new ArgumentException($"DownloadAndSave(): All symbols must have {_securityType} security type.");
            }

            if (symbols.DistinctBy(x => x.ID.Symbol).Count() > 1)
            {
                throw new ArgumentException("DownloadAndSave(): All symbols must have the same root ticker.");
            }

            var dataType = LeanData.GetDataType(_resolution, _tickType);

            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            var ticker = symbols.First().ID.Symbol;
            var market = symbols.First().ID.Market;

            var canonicalSymbol = Symbol.Create(ticker, _securityType, market);

            var exchangeHours = marketHoursDatabase.GetExchangeHours(canonicalSymbol.ID.Market, canonicalSymbol, _securityType);
            var dataTimeZone = marketHoursDatabase.GetDataTimeZone(canonicalSymbol.ID.Market, canonicalSymbol, _securityType);

            var historyBySymbol = new Dictionary<Symbol, List<IGrouping<DateTime, BaseData>>>();
            var historyBySymbolDailyOrHour = new Dictionary<Symbol, List<BaseData>>();

            foreach (var symbol in symbols)
            {
                var historyRequest = new HistoryRequest(
                    startTimeUtc,
                    endTimeUtc,
                    dataType,
                    symbol,
                    _resolution,
                    exchangeHours,
                    dataTimeZone,
                    _resolution,
                    true,
                    false,
                    DataNormalizationMode.Raw,
                    _tickType
                );

                var history = brokerage.GetHistory(historyRequest)
                    .Select(
                        x =>
                        {
                            x.Time = x.Time.ConvertTo(exchangeHours.TimeZone, dataTimeZone);
                            return x;
                        })
                    .ToList();

                if (_resolution == Resolution.Daily || _resolution == Resolution.Hour)
                {
                    historyBySymbolDailyOrHour.Add(symbol, history);
                }
                else
                {
                    // group by date in DataTimeZone
                    var historyByDate = history.GroupBy(x => x.Time.Date).ToList();
                    historyBySymbol.Add(symbol, historyByDate);
                }
            }

            if (_resolution == Resolution.Daily || _resolution == Resolution.Hour)
            {
                SaveDailyOrHour(symbols, canonicalSymbol, historyBySymbolDailyOrHour);
            }
            else
            {
                SaveMinuteOrSecondOrTick(symbols, startTimeUtc, endTimeUtc, canonicalSymbol, historyBySymbol);
            }
        }

        /// <summary>
        /// TODO: MERGE THESE TOGETHER DUPLICATED AS HELL
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="canonicalSymbol"></param>
        /// <param name="historyBySymbol"></param>
        private void SaveDailyOrHour(
            List<Symbol> symbols,
            Symbol canonicalSymbol,
            IReadOnlyDictionary<Symbol, List<BaseData>> historyBySymbol)
        {
            var zipFileName = Path.Combine(
                _dataDirectory,
                LeanData.GenerateRelativeZipFilePath(canonicalSymbol, DateTime.MinValue, _resolution, _tickType));

            var folder = Path.GetDirectoryName(zipFileName);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (var zip = new ZipFile(zipFileName))
            {
                foreach (var symbol in symbols)
                {
                    // Load new data rows into a SortedDictionary for easy merge/update
                    var newRows = new SortedDictionary<DateTime, string>(historyBySymbol[symbol]
                        .ToDictionary(x => x.Time, x => LeanData.GenerateLine(x, _securityType, _resolution)));

                    var rows = new SortedDictionary<DateTime, string>();

                    var zipEntryName = LeanData.GenerateZipEntryName(symbol, DateTime.MinValue, _resolution, _tickType);

                    if (zip.ContainsEntry(zipEntryName))
                    {
                        // If file exists, we load existing data and perform merge
                        using (var stream = new MemoryStream())
                        {
                            zip[zipEntryName].Extract(stream);
                            stream.Seek(0, SeekOrigin.Begin);

                            using (var reader = new StreamReader(stream))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    var time = Parse.DateTimeExact(line.Substring(0, DateFormat.TwelveCharacter.Length), DateFormat.TwelveCharacter);
                                    rows[time] = line;
                                }
                            }
                        }

                        foreach (var kvp in newRows)
                        {
                            rows[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        // No existing file, just use the new data
                        rows = newRows;
                    }

                    // Loop through the SortedDictionary and write to zip entry
                    var sb = new StringBuilder();
                    foreach (var kvp in rows)
                    {
                        // Build the line and append it to the file
                        sb.AppendLine(kvp.Value);
                    }

                    // Write the zip entry
                    if (sb.Length > 0)
                    {
                        if (zip.ContainsEntry(zipEntryName))
                        {
                            zip.RemoveEntry(zipEntryName);
                        }

                        zip.AddEntry(zipEntryName, sb.ToString());
                    }
                }

                if (zip.Count > 0)
                {
                    zip.Save();
                }
            }
        }

        private void SaveMinuteOrSecondOrTick(
            List<Symbol> symbols,
            DateTime startTimeUtc,
            DateTime endTimeUtc,
            Symbol canonicalSymbol,
            IReadOnlyDictionary<Symbol, List<IGrouping<DateTime, BaseData>>> historyBySymbol)
        {
            var date = startTimeUtc;
            while (date <= endTimeUtc)
            {
                var zipFileName = Path.Combine(
                    _dataDirectory,
                    LeanData.GenerateRelativeZipFilePath(canonicalSymbol, date, _resolution, _tickType));

                var folder = Path.GetDirectoryName(zipFileName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                if (File.Exists(zipFileName) && !_appendToZips)
                {
                    File.Delete(zipFileName);
                }

                using (var zip = new ZipFile(zipFileName))
                {
                    foreach (var symbol in symbols)
                    {
                        var zipEntryName = LeanData.GenerateZipEntryName(symbol, date, _resolution, _tickType);

                        foreach (var group in historyBySymbol[symbol])
                        {
                            if (group.Key == date.Date)
                            {
                                var sb = new StringBuilder();
                                foreach (var row in group)
                                {
                                    var line = LeanData.GenerateLine(row, _securityType, _resolution);
                                    sb.AppendLine(line);
                                }

                                if (_appendToZips && zip.ContainsEntry(zipEntryName))
                                {
                                    zip.RemoveEntry(zipEntryName);
                                }

                                zip.AddEntry(zipEntryName, sb.ToString());
                                break;
                            }
                        }
                    }

                    if (zip.Count > 0)
                    {
                        zip.Save();
                    }
                }

                date = date.AddDays(1);
            }
        }

        /// <summary>
        /// Loads an existing hourly or daily Lean zip file into a SortedDictionary
        /// </summary>
        protected virtual bool TryLoadFile(string fileName, string entryName, out SortedDictionary<DateTime, string> rows)
        {
            rows = new SortedDictionary<DateTime, string>();

            using (var stream = _dataCacheProvider.Fetch($"{fileName}#{entryName}"))
            {
                if (stream == null)
                {
                    return false;
                }

                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var time = DateTime.ParseExact(line.AsSpan(0, DateFormat.TwelveCharacter.Length), DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                        rows[time] = line;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Write this file to disk.
        /// </summary>
        /// <param name="filePath">The full path to the new file</param>
        /// <param name="data">The data to write as a string</param>
        /// <param name="date">The date the data represents</param>
        protected virtual void WriteFile(string filePath, SortedDictionary<DateTime, string> data, DateTime date)
        {
            // Generate this csv entry name
            var entryName = LeanData.GenerateZipEntryName(_symbol, date, _resolution, _tickType);
            
            // Check disk once for this file ahead of time, reuse where possible
            var fileExists = File.Exists(filePath);

            // Handle merging of files
            // Only merge on files with hour/daily resolution, that exist, and can be loaded
            if (_resolution >= Resolution.Hour && fileExists && TryLoadFile(filePath, entryName, out var rows))
            {
                // Preform merge on loaded rows
                foreach (var entry in data)
                {
                    rows[entry.Key] = entry.Value;
                }
            }
            else
            {
                // No need to merge for one of the reasons above, just write these rows
                rows = data;
            }

            if (!_appendToZips && fileExists)
            {
                File.Delete(filePath);
                Log.Trace("LeanDataWriter.Write(): Existing deleted: " + filePath);
            }

            // If our file doesn't exist its possible the directory doesn't exist, make sure at least the directory exists
            if (!fileExists)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            if (_appendToZips)
            {
                var bytes = Encoding.UTF8.GetBytes(string.Join("\n", rows.Values));
                _dataCacheProvider.Store($"{filePath}#{entryName}", bytes);

                Log.Trace($"LeanDataWriter.Write(): Appended: {filePath} @ {entryName}");
            }
            else
            {
                // Write out this data string to a zip file
                var tempFilePath = filePath + ".tmp";
                Compression.ZipData(tempFilePath, entryName, rows.Values);

                // Move temp file to the final destination with the appropriate name
                File.Move(tempFilePath, filePath);
                Log.Trace("LeanDataWriter.Write(): Created: " + filePath);
            }
        }

        /// <summary>
        /// Get the output zip file
        /// </summary>
        /// <param name="baseDirectory">Base output directory for the zip file</param>
        /// <param name="time">Date/time for the data we're writing</param>
        /// <returns>The full path to the output zip file</returns>
        private string GetZipOutputFileName(string baseDirectory, DateTime time)
        {
            return LeanData.GenerateZipFilePath(baseDirectory, _symbol, time, _resolution, _tickType);
        }

    }

    /// <summary>
    /// Simple data cache provider, writes and reads directly from disk
    /// Used as default for <see cref="LeanDataWriter"/>
    /// </summary>
    public class DiskDataCacheProvider : IDataCacheProvider
    { 
        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral { get; }

        /// <summary>
        /// Simple data cache provider, writes and reads directly from disk
        /// Used as default for <see cref="LeanDataWriter"/>
        /// </summary>
        public DiskDataCacheProvider()
        {
            IsDataEphemeral = false;
        }

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream Fetch(string key)
        {
            DataCacheProviderExtensions.ParseKey(key, out var filePath, out var entryName);
            using (var zip = ZipFile.Read(filePath))
            {
                if (!zip.ContainsEntry(entryName))
                {
                    return null;
                }

                var stream = new MemoryStream();
                zip[entryName].Extract(stream);
                return stream;
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            DataCacheProviderExtensions.ParseKey(key, out var filePath, out var entryName);
            Compression.ZipCreateAppendData(filePath, entryName, data);
        }

        /// <summary>
        /// Dispose for this class
        /// </summary>
        public void Dispose()
        {
            //NOP
        }
    }
}
