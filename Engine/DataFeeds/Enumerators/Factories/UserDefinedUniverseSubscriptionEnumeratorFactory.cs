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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> to emit
    /// ticks based on <see cref="UserDefinedUniverse.GetTriggerTimes"/>, allowing universe
    /// selection to fire at planned times.
    /// </summary>
    public class UserDefinedUniverseSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly UserDefinedUniverse _universe;
        private readonly MarketHoursDatabase _marketHoursDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDefinedUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="universe">The user defined universe</param>
        /// <param name="marketHoursDatabase">The market hours database</param>
        public UserDefinedUniverseSubscriptionEnumeratorFactory(UserDefinedUniverse universe, MarketHoursDatabase marketHoursDatabase)
        {
            _universe = universe;
            _marketHoursDatabase = marketHoursDatabase;
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            return _universe.GetTriggerTimes(request.StartTimeUtc, request.EndTimeUtc, _marketHoursDatabase)
                .Select(x => new Tick {Time = x, Symbol = request.Configuration.Symbol}).GetEnumerator();
        }
    }
}