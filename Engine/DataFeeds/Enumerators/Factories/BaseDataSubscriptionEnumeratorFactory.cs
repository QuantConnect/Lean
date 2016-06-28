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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides a default implementation of <see cref="ISubscriptionEnumeratorFactory"/> that uses
    /// <see cref="BaseData"/> factory methods for reading sources
    /// </summary>
    public class BaseDataSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly Func<SubscriptionRequest, IEnumerable<DateTime>> _tradableDaysProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="tradableDaysProvider">Function used to provide the tradable dates to be enumerator.
        /// Specify null to default to <see cref="SubscriptionRequest.TradableDays"/></param>
        public BaseDataSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerable<DateTime>> tradableDaysProvider = null)
        {
            _tradableDaysProvider = tradableDaysProvider ?? (request => request.TradableDays);
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            var sourceFactory = (BaseData)Activator.CreateInstance(request.Configuration.Type);

            return (
                from date in _tradableDaysProvider(request)
                let source = sourceFactory.GetSource(request.Configuration, date, false)
                let factory = SubscriptionDataSourceReader.ForSource(source, request.Configuration, date, false)
                let entriesForDate = factory.Read(source)
                from entry in entriesForDate
                select entry
                )
                .GetEnumerator();
        }
    }
}