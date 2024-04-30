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
 *
*/

using System;
using NodaTime;
using System.IO;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Configuration;
using System.Collections.Concurrent;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Data provider which downloads data using an <see cref="IDataDownloader"/> or <see cref="IBrokerage"/> implementation
    /// </summary>
    public class DownloaderDataProvider : BaseDownloaderDataProvider
    {
        /// <summary>
        /// Synchronizer in charge of guaranteeing a single operation per file path
        /// </summary>
        private readonly static KeyStringSynchronizer DiskSynchronizer = new();

        private bool _customDataDownloadError;
        private readonly ConcurrentDictionary<Symbol, Symbol> _marketHoursWarning = new();
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        private readonly IDataDownloader _dataDownloader;
        private readonly IDataCacheProvider _dataCacheProvider = new DiskDataCacheProvider(DiskSynchronizer);
        private readonly IMapFileProvider _mapFileProvider = Composer.Instance.GetPart<IMapFileProvider>();

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DownloaderDataProvider()
        {
            var dataDownloaderConfig = Config.Get("data-downloader");
            if (!string.IsNullOrEmpty(dataDownloaderConfig))
            {
                _dataDownloader = Composer.Instance.GetExportedValueByTypeName<IDataDownloader>(dataDownloaderConfig);
            }
            else
            {
                throw new ArgumentException("DownloaderDataProvider(): requires 'data-downloader' to be set with a valid type name");
            }
        }

        /// <summary>
        /// Creates a new instance using a target data downloader used for testing
        /// </summary>
        public DownloaderDataProvider(IDataDownloader dataDownloader)
        {
            _dataDownloader = dataDownloader;
        }

        /// <summary>
        /// Determines if it should downloads new data and retrieves data from disc
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public override Stream Fetch(string key)
        {
            return DownloadOnce(key, s =>
            {
                if (LeanData.TryParsePath(key, out var symbol, out var date, out var resolution, out var tickType, out var dataType))
                {
                    if (symbol.SecurityType == SecurityType.Base)
                    {
                        if (!_customDataDownloadError)
                        {
                            _customDataDownloadError = true;
                            // lean data writter doesn't support it
                            Log.Trace($"DownloaderDataProvider.Get(): custom data is not supported, requested: {symbol}");
                        }
                        return;
                    }

                    MarketHoursDatabase.Entry entry;
                    try
                    {
                        entry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
                    }
                    catch
                    {
                        // this could happen for some sources using the data provider but with not market hours data base entry, like interest rates
                        if (_marketHoursWarning.TryAdd(symbol, symbol))
                        {
                            // log once
                            Log.Trace($"DownloaderDataProvider.Get(): failed to find market hours for {symbol}, skipping");
                        }
                        // this shouldn't happen for data we want can download
                        return;
                    }

                    var dataTimeZone = entry.DataTimeZone;
                    var exchangeTimeZone = entry.ExchangeHours.TimeZone;
                    DateTime startTimeUtc;
                    DateTime endTimeUtc;
                    // we will download until yesterday so we are sure we don't get partial data
                    var endTimeUtcLimit = DateTime.UtcNow.Date.AddDays(-1);
                    if (resolution < Resolution.Hour)
                    {
                        // we can get the date from the path
                        startTimeUtc = date.ConvertToUtc(dataTimeZone);
                        // let's get the whole day
                        endTimeUtc = date.AddDays(1).ConvertToUtc(dataTimeZone);
                        if (endTimeUtc > endTimeUtcLimit)
                        {
                            // we are at the limit, avoid getting partial data
                            return;
                        }
                    }
                    else
                    {
                        // since hourly & daily are a single file we fetch the whole file
                        endTimeUtc = endTimeUtcLimit;
                        try
                        {
                            // we don't really know when Futures, FutureOptions, Cryptos, etc, start date so let's give it a good guess
                            if (symbol.SecurityType == SecurityType.Crypto)
                            {
                                // bitcoin start
                                startTimeUtc = new DateTime(2009, 1, 1);
                            }
                            else if (symbol.SecurityType.IsOption() && symbol.SecurityType != SecurityType.FutureOption)
                            {
                                // For options, an hourly or daily file contains a year of data, so we need to get the year of the date
                                startTimeUtc = new DateTime(date.Year, 1, 1);
                                endTimeUtc = startTimeUtc.AddYears(1);
                            }
                            else
                            {
                                startTimeUtc = symbol.ID.Date;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            startTimeUtc = Time.Start;
                        }

                        if (startTimeUtc < Time.Start)
                        {
                            startTimeUtc = Time.Start;
                        }

                        if (endTimeUtc > endTimeUtcLimit)
                        {
                            endTimeUtc = endTimeUtcLimit;
                        }
                    }

                    try
                    {
                        LeanDataWriter writer = null;
                        var getParams = new DataDownloaderGetParameters(symbol, resolution, startTimeUtc, endTimeUtc, tickType);

                        var downloaderDataParameters = getParams.GetDataDownloaderParameterForAllMappedSymbols(_mapFileProvider, exchangeTimeZone);

                        var downloadedData = GetDownloadedData(downloaderDataParameters, symbol, exchangeTimeZone, dataTimeZone, dataType);

                        foreach (var dataPerSymbol in downloadedData)
                        {
                            if (writer == null)
                            {
                                writer = new LeanDataWriter(resolution, symbol, Globals.DataFolder, tickType, mapSymbol: true, dataCacheProvider: _dataCacheProvider);
                            }
                            // Save the data
                            writer.Write(dataPerSymbol);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            });
        }

        /// <summary>
        /// Retrieves downloaded data grouped by symbol based on <see cref="IDownloadProvider"/>.
        /// </summary>
        /// <param name="downloaderDataParameters">Parameters specifying the data to be retrieved.</param>
        /// <param name="symbol">Represents a unique security identifier, generate by ticker name.</param>
        /// <param name="exchangeTimeZone">The time zone of the exchange where the symbol is traded.</param>
        /// <param name="dataTimeZone">The time zone in which the data is represented.</param>
        /// <param name="dataType">The type of data to be retrieved. (e.g. <see cref="Data.Market.TradeBar"/>)</param>
        /// <returns>An IEnumerable containing groups of data grouped by symbol. Each group contains data related to a specific symbol.</returns>
        /// <exception cref="ArgumentException"> Thrown when the downloaderDataParameters collection is null or empty.</exception>
        public IEnumerable<IGrouping<Symbol, BaseData>> GetDownloadedData(
            IEnumerable<DataDownloaderGetParameters> downloaderDataParameters,
            Symbol symbol,
            DateTimeZone exchangeTimeZone,
            DateTimeZone dataTimeZone,
            Type dataType)
        {
            if (downloaderDataParameters.IsNullOrEmpty())
            {
                throw new ArgumentException($"{nameof(DownloaderDataProvider)}.{nameof(GetDownloadedData)}: DataDownloaderGetParameters are empty or equal to null.");
            }

            foreach (var downloaderDataParameter in downloaderDataParameters)
            {
                var downloadedData = _dataDownloader.Get(downloaderDataParameter);

                if (downloadedData == null)
                {
                    // doesn't support this download request, that's okay
                    continue;
                }

                var groupedData = FilterAndGroupDownloadDataBySymbol(
                    downloadedData,
                    symbol,
                    dataType,
                    exchangeTimeZone,
                    dataTimeZone,
                    downloaderDataParameter.StartUtc,
                    downloaderDataParameter.EndUtc);

                foreach (var data in groupedData)
                {
                    yield return data;
                }
            }
        }

        /// <summary>
        /// Get's the stream for a given file path
        /// </summary>
        protected override Stream GetStream(string key)
        {
            if (LeanData.TryParsePath(key, out var symbol, out var date, out var resolution) && resolution > Resolution.Minute && symbol.RequiresMapping())
            {
                // because the file could be updated even after it's created because of symbol mapping we can't stream from disk
                return DiskSynchronizer.Execute(key, () =>
                {
                    var baseStream = base.Fetch(key);
                    if (baseStream != null)
                    {
                        var result = new MemoryStream();
                        baseStream.CopyTo(result);
                        baseStream.Dispose();
                        // move position back to the start
                        result.Position = 0;

                        return result;
                    }
                    return null;
                });
            }

            return base.Fetch(key);
        }

        /// <summary>
        /// Main filter to determine if this file needs to be downloaded
        /// </summary>
        /// <param name="filePath">File we are looking at</param>
        /// <returns>True if should download</returns>
        protected override bool NeedToDownload(string filePath)
        {
            // Ignore null and invalid data requests
            if (filePath == null
                || filePath.Contains("fine", StringComparison.InvariantCultureIgnoreCase) && filePath.Contains("fundamental", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("map_files", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("factor_files", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("margins", StringComparison.InvariantCultureIgnoreCase) && filePath.Contains("future", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            // Only download if it doesn't exist or is out of date.
            // Files are only "out of date" for non date based files (hour, daily, margins, etc.) because this data is stored all in one file
            return !File.Exists(filePath) || filePath.IsOutOfDate();
        }

        /// <summary>
        /// Filters and groups the provided download data by symbol, based on specified criteria.
        /// </summary>
        /// <param name="downloadData">The collection of download data to process.</param>
        /// <param name="symbol">The symbol to filter the data for.</param>
        /// <param name="dataType">The type of data to filter for.</param>
        /// <param name="exchangeTimeZone">The time zone of the exchange.</param>
        /// <param name="dataTimeZone">The desired time zone for the data.</param>
        /// <param name="downloaderStartTimeUtc">The start time of data downloading in UTC.</param>
        /// <param name="downloaderEndTimeUtc">The end time of data downloading in UTC.</param>
        /// <returns>
        /// An enumerable collection of groupings of download data, grouped by symbol.
        /// </returns>
        public static IEnumerable<IGrouping<Symbol, BaseData>> FilterAndGroupDownloadDataBySymbol(
            IEnumerable<BaseData> downloadData,
            Symbol symbol,
            Type dataType,
            DateTimeZone exchangeTimeZone,
            DateTimeZone dataTimeZone,
            DateTime downloaderStartTimeUtc,
            DateTime downloaderEndTimeUtc)
        {
            var startDateTimeInExchangeTimeZone = downloaderStartTimeUtc.ConvertFromUtc(exchangeTimeZone);
            var endDateTimeInExchangeTimeZone = downloaderEndTimeUtc.ConvertFromUtc(exchangeTimeZone);

            return downloadData
                .Where(baseData =>
                {
                    // Sometimes, external Downloader provider returns excess data
                    if (baseData.Time < startDateTimeInExchangeTimeZone || baseData.Time > endDateTimeInExchangeTimeZone)
                    {
                        return false;
                    }

                    if (symbol.SecurityType == SecurityType.Base || baseData.GetType() == dataType)
                    {
                        // we need to store the data in data time zone
                        baseData.Time = baseData.Time.ConvertTo(exchangeTimeZone, dataTimeZone);
                        baseData.EndTime = baseData.EndTime.ConvertTo(exchangeTimeZone, dataTimeZone);
                        return true;
                    }
                    return false;
                })
                // for canonical symbols, downloader will return data for all of the chain
                .GroupBy(baseData => baseData.Symbol);
        }
    }
}
