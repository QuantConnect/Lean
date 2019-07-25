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
        private readonly bool _isLiveMode;
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;
        private DateTime _lastUsedDate;

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

                var fineFundamental = new FineFundamental();
                var fineFundamentalConfiguration = new SubscriptionDataConfig(request.Configuration, typeof(FineFundamental), request.Security.Symbol);

                foreach (var date in tradableDays)
                {
                    var fineFundamentalSource = GetSource(fineFundamental, fineFundamentalConfiguration, date);
                    var fineFundamentalFactory = SubscriptionDataSourceReader.ForSource(fineFundamentalSource, dataCacheProvider, fineFundamentalConfiguration, date, _isLiveMode);
                    var fineFundamentalForDate = (FineFundamental)fineFundamentalFactory.Read(fineFundamentalSource).FirstOrDefault();

                    yield return new FineFundamental
                    {
                        DataType = MarketDataType.Auxiliary,
                        Symbol = request.Configuration.Symbol,
                        Time = date,
                        CompanyReference = fineFundamentalForDate != null ? fineFundamentalForDate.CompanyReference : new CompanyReference(),
                        SecurityReference = fineFundamentalForDate != null ? fineFundamentalForDate.SecurityReference : new SecurityReference(),
                        FinancialStatements = fineFundamentalForDate != null ? fineFundamentalForDate.FinancialStatements : new FinancialStatements(),
                        EarningReports = fineFundamentalForDate != null ? fineFundamentalForDate.EarningReports : new EarningReports(),
                        OperationRatios = fineFundamentalForDate != null ? fineFundamentalForDate.OperationRatios : new OperationRatios(),
                        EarningRatios = fineFundamentalForDate != null ? fineFundamentalForDate.EarningRatios : new EarningRatios(),
                        ValuationRatios = fineFundamentalForDate != null ? fineFundamentalForDate.ValuationRatios : new ValuationRatios(),
                        AssetClassification = fineFundamentalForDate != null ? fineFundamentalForDate.AssetClassification : new AssetClassification(),
                        CompanyProfile = fineFundamentalForDate != null ? fineFundamentalForDate.CompanyProfile : new CompanyProfile()
                    };
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

            if (!File.Exists(source.Source))
            {
                if (_lastUsedDate == default(DateTime))
                {
                    // find first file date
                    List<string> availableFiles;
                    try
                    {
                        availableFiles = Directory.GetFiles(Path.GetDirectoryName(source.Source),
                            "*.zip").OrderBy(x => x).ToList();
                    }
                    catch
                    {
                        // argument null exception or directory doesn't exist
                        return source;
                    }

                    // no files or requested date before first date, return null source
                    if (availableFiles.Count == 0 || date < DateTime.ParseExact(
                            Path.GetFileNameWithoutExtension(availableFiles[0]),
                            "yyyyMMdd",
                            CultureInfo.InvariantCulture))
                    {
                        return source;
                    }

                    // take advantage of the order and avoid parsing all files to get the date
                    var value = availableFiles.BinarySearch(source.Source,
                        new FileDateComparer());

                    if (value < 0)
                    {
                        // if negative returns the complement of the index of the next element that is larger than Date
                        // so subtract 1 to get the previous item which is less than Date
                        value = ~value - 1;
                    }

                    var current = DateTime.ParseExact(
                        Path.GetFileNameWithoutExtension(availableFiles[value]),
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture);

                    if (current <= date)
                    {
                        // we found the file, save its date and return the source
                        _lastUsedDate = current;
                        return fine.GetSource(config, current, _isLiveMode);
                    }

                    // this shouldn't ever happen so throw
                    throw new InvalidOperationException("FineFundamentalSubscriptionEnumeratorFactory.GetSource(): " +
                                                        $"failed to resolve source for {config.Symbol} on {date}");
                }
                else
                {
                    // return source for last existing file date
                    source = fine.GetSource(config, _lastUsedDate, _isLiveMode);
                }
            }
            else
            {
                _lastUsedDate = date;
            }

            return source;
        }

        /// <summary>
        /// Helper class to compare to file names which are named as a date
        /// </summary>
        private class FileDateComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var dateX = x == null ? DateTime.MaxValue : DateTime.ParseExact(
                    Path.GetFileNameWithoutExtension(x),
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture
                );

                var dateY = y == null ? DateTime.MaxValue : DateTime.ParseExact(
                    Path.GetFileNameWithoutExtension(y),
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture
                );

                return dateX.CompareTo(dateY);
            }
        }
    }
}