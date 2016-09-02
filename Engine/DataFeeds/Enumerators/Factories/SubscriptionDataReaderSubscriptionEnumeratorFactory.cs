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
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> that used the
    /// <see cref="SubscriptionDataReader"/>
    /// </summary>
    public class SubscriptionDataReaderSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly bool _isLiveMode;
        private readonly bool _includeAuxiliaryData;
        private readonly IResultHandler _resultHandler;
        private readonly MapFileResolver _mapFileResolver;
        private readonly IFactorFileProvider _factorFileProvider;
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionDataReaderSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="resultHandler">The result handler for the algorithm</param>
        /// <param name="mapFileResolver">The map file resolver</param>
        /// <param name="factorFileProvider">The factory file provider</param>
        /// <param name="isLiveMode">True if runnig live algorithm, false otherwise</param>
        /// <param name="includeAuxiliaryData">True to check for auxiliary data, false otherwise</param>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to be enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public SubscriptionDataReaderSubscriptionEnumeratorFactory(IResultHandler resultHandler,
            MapFileResolver mapFileResolver,
            IFactorFileProvider factorFileProvider,
            bool isLiveMode,
            bool includeAuxiliaryData,
            Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null
            )
        {
            _resultHandler = resultHandler;
            _mapFileResolver = mapFileResolver;
            _factorFileProvider = factorFileProvider;
            _isLiveMode = isLiveMode;
            _includeAuxiliaryData = includeAuxiliaryData;
            _tradableDaysProvider = tradableDaysProvider ?? (request => request.TradableDays);
        }

        /// <summary>
        /// Creates a <see cref="SubscriptionDataReader"/> to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            return new SubscriptionDataReader(request.Configuration, 
                request.StartTimeLocal, 
                request.EndTimeLocal, 
                _resultHandler, 
                _mapFileResolver,
                _factorFileProvider, 
                _tradableDaysProvider(request), 
                _isLiveMode, 
                _includeAuxiliaryData
                );
        }
    }
}
