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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides a default implementation of <see cref="ISubscriptionEnumeratorFactory"/> that uses
    /// <see cref="BaseData"/> factory methods for reading sources
    /// </summary>
    public class BaseDataSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;
        private readonly IMapFileProvider _mapFileProvider;
        private readonly bool _isLiveMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <param name="mapFileProvider">Used for resolving the correct map files</param>
        /// <param name="factorFileProvider">Used for getting factor files</param>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to be enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public BaseDataSubscriptionEnumeratorFactory(bool isLiveMode, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null)
        {
            _isLiveMode = isLiveMode;
            _tradableDaysProvider = tradableDaysProvider ?? (request => request.TradableDays);
            _mapFileProvider = mapFileProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to be enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public BaseDataSubscriptionEnumeratorFactory(bool isLiveMode, Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null)
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
            // We decide to use the ZipDataCacheProvider instead of the SingleEntryDataCacheProvider here
            // for resiliency and as a fix for an issue preventing us from reading non-equity options data.
            // It has the added benefit of caching any zip files that we request from the filesystem, and reading
            // files contained within the zip file, which the SingleEntryDataCacheProvider does not support.
            var sourceFactory = request.Configuration.GetBaseDataInstance();
            using (var dataCacheProvider = new ZipDataCacheProvider(dataProvider))
            {
                foreach (var date in _tradableDaysProvider(request))
                {
                    if (sourceFactory.RequiresMapping() && _mapFileProvider != null)
                    {
                        request.Configuration.MappedSymbol = GetMappedSymbol(request.Configuration, date);
                    }

                    var source = sourceFactory.GetSource(request.Configuration, date, _isLiveMode);
                    var factory = SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, request.Configuration, date, _isLiveMode, sourceFactory, dataProvider);
                    var entriesForDate = factory.Read(source);
                    foreach (var entry in entriesForDate)
                    {
                        // Fix for Daily/Hour options cases when reading in all equity data from daily/hourly file
                        if (entry.Time.Date != date)
                        {
                            continue;
                        }

                        yield return entry;
                    }
                }
            }
        }

        private string GetMappedSymbol(SubscriptionDataConfig config, DateTime date)
        {
            return _mapFileProvider.ResolveMapFile(config).GetMappedSymbol(date, config.MappedSymbol);
        }
    }
}
