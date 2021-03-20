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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> that reads
    /// an entire <see cref="SubscriptionDataSource"/> into a single <see cref="FineFundamental"/>
    /// to be emitted on the tradable date at midnight
    /// </summary>
    public class FineFundamentalSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private static readonly ConcurrentDictionary<int, List<DateTime>> FineFilesCache
            = new ConcurrentDictionary<int, List<DateTime>>();
        // creating a fine fundamental instance is expensive (its massive) so we keep our factory instance
        private static readonly FineFundamental FineFundamental = new FineFundamental();

        private readonly bool _isLiveMode;
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FineFundamentalSubscriptionEnumeratorFactory"/> class.
        /// </summary>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to the enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public FineFundamentalSubscriptionEnumeratorFactory(bool isLiveMode, Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null)
        {
            _isLiveMode = isLiveMode;
            _tradableDaysProvider = tradableDaysProvider ?? (request => request.TradableDays);
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            using (var dataCacheProvider = new SingleEntryDataCacheProvider(dataProvider))
            {
                var tradableDays = _tradableDaysProvider(request);

                var fineFundamentalConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(FineFundamental), request.Security.Symbol);

                foreach (var date in tradableDays)
                {
                    var fineFundamentalSource = GetSource(FineFundamental, fineFundamentalConfiguration, date);
                    var fineFundamentalFactory = SubscriptionDataSourceReader.ForSource(fineFundamentalSource, dataCacheProvider, fineFundamentalConfiguration, date, _isLiveMode, FineFundamental);
                    var fineFundamentalForDate = (FineFundamental)fineFundamentalFactory.Read(fineFundamentalSource).FirstOrDefault();

                    // directly do not emit null points. Null points won't happen when used with Coarse data since we are pre filtering based on Coarse.HasFundamentalData
                    // but could happen when fine filtering custom universes
                    if (fineFundamentalForDate != null)
                    {
                        yield return new FineFundamental
                        {
                            DataType = MarketDataType.Auxiliary,
                            Symbol = request.Configuration.Symbol,
                            Time = date,
                            CompanyReference = fineFundamentalForDate.CompanyReference,
                            SecurityReference = fineFundamentalForDate.SecurityReference,
                            FinancialStatements = fineFundamentalForDate.FinancialStatements,
                            EarningReports = fineFundamentalForDate.EarningReports,
                            OperationRatios = fineFundamentalForDate.OperationRatios,
                            EarningRatios = fineFundamentalForDate.EarningRatios,
                            ValuationRatios = fineFundamentalForDate.ValuationRatios,
                            AssetClassification = fineFundamentalForDate.AssetClassification,
                            CompanyProfile = fineFundamentalForDate.CompanyProfile
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Returns a SubscriptionDataSource for the FineFundamental class,
        /// returning data from a previous date if not available for the requested date
        /// </summary>
        private SubscriptionDataSource GetSource(FineFundamental fine, SubscriptionDataConfig config, DateTime date)
        {
            var source = fine.GetSource(config, date, _isLiveMode);

            if (File.Exists(source.Source))
            {
                return source;
            }

            if (_isLiveMode)
            {
                var result = DailyBackwardsLoop(fine, config, date, source);
                // if we didn't fine any file we just fallback into listing the directory
                if (result != null)
                {
                    return result;
                }
            }

            var cacheKey = config.Symbol.Value.ToLowerInvariant().GetHashCode();
            List<DateTime> availableDates;

            // only use cache in backtest, since in live mode new fine files are added
            // we still didn't load available fine dates for this symbol
            if (_isLiveMode || !FineFilesCache.TryGetValue(cacheKey, out availableDates))
            {
                try
                {
                    var path = Path.GetDirectoryName(source.Source) ?? string.Empty;
                    availableDates = Directory.GetFiles(path, "*.zip")
                        .Select(
                            filePath =>
                            {
                                try
                                {
                                    return DateTime.ParseExact(
                                        Path.GetFileNameWithoutExtension(filePath),
                                        "yyyyMMdd",
                                        CultureInfo.InvariantCulture
                                    );
                                }
                                catch
                                {
                                    // just in case...
                                    return DateTime.MaxValue;
                                }
                            }
                        )
                        .Where(time => time != DateTime.MaxValue)
                        .OrderBy(x => x)
                        .ToList();
                }
                catch
                {
                    // directory doesn't exist or path is null
                    if (!_isLiveMode)
                    {
                        // only add to cache if not live mode
                        FineFilesCache[cacheKey] = new List<DateTime>();
                    }
                    return source;
                }

                if (!_isLiveMode)
                {
                    // only add to cache if not live mode
                    FineFilesCache[cacheKey] = availableDates;
                }
            }

            // requested date before first date, return null source
            if (availableDates.Count == 0 || date < availableDates[0])
            {
                return source;
            }
            for (var i = availableDates.Count - 1; i >= 0; i--)
            {
                // we iterate backwards ^ and find the first data point before 'date'
                if (availableDates[i] <= date)
                {
                    return fine.GetSource(config, availableDates[i], _isLiveMode);
                }
            }

            return source;
        }

        private SubscriptionDataSource DailyBackwardsLoop(FineFundamental fine, SubscriptionDataConfig config, DateTime date, SubscriptionDataSource source)
        {
            var path = Path.GetDirectoryName(source.Source) ?? string.Empty;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                // directory does not exist
                return source;
            }

            // loop back in time, for 10 days, until we find an existing file
            var count = 10;
            do
            {
                // get previous date
                date = date.AddDays(-1);

                // get file name for this date
                source = fine.GetSource(config, date, _isLiveMode);
                if (File.Exists(source.Source))
                {
                    break;
                }
            }
            while (--count > 0);

            return count == 0 ? null : source;
        }
    }
}