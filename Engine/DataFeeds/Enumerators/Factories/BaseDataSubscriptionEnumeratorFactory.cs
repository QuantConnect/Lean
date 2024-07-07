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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides a default implementation of <see cref="ISubscriptionEnumeratorFactory"/> that uses
    /// <see cref="BaseData"/> factory methods for reading sources
    /// </summary>
    public class BaseDataSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly IOptionChainProvider _optionChainProvider;
        private readonly IFutureChainProvider _futureChainProvider;
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="optionChainProvider">The option chain provider</param>
        /// <param name="futureChainProvider">The future chain provider</param>
        public BaseDataSubscriptionEnumeratorFactory(
            IOptionChainProvider optionChainProvider,
            IFutureChainProvider futureChainProvider
        )
        {
            _futureChainProvider = futureChainProvider;
            _optionChainProvider = optionChainProvider;
            _tradableDaysProvider = (request => request.TradableDaysInDataTimeZone);
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(
            SubscriptionRequest request,
            IDataProvider dataProvider
        )
        {
            // We decide to use the ZipDataCacheProvider instead of the SingleEntryDataCacheProvider here
            // for resiliency and as a fix for an issue preventing us from reading non-equity options data.
            // It has the added benefit of caching any zip files that we request from the filesystem, and reading
            // files contained within the zip file, which the SingleEntryDataCacheProvider does not support.
            var sourceFactory = request.Configuration.GetBaseDataInstance();
            foreach (var date in _tradableDaysProvider(request))
            {
                IEnumerable<Symbol> symbols;
                if (request.Configuration.SecurityType.IsOption())
                {
                    symbols = _optionChainProvider.GetOptionContractList(
                        request.Configuration.Symbol,
                        date
                    );
                }
                else if (request.Configuration.SecurityType == SecurityType.Future)
                {
                    symbols = _futureChainProvider.GetFutureContractList(
                        request.Configuration.Symbol,
                        date
                    );
                }
                else
                {
                    throw new NotImplementedException(
                        $"{request.Configuration.SecurityType} is not supported"
                    );
                }

                // we are going to use these symbols to create a collection that for options will also have the underlying that will be emitted in exchange time zone
                // note the merging of the data will happen based on their end time so time zones are important to respect
                var exchangeTimeZoneDate = date.ConvertTo(
                    request.Configuration.DataTimeZone,
                    request.ExchangeHours.TimeZone
                );
                foreach (var symbol in symbols)
                {
                    yield return new ZipEntryName { Symbol = symbol, Time = exchangeTimeZoneDate };
                }
            }
        }
    }
}
