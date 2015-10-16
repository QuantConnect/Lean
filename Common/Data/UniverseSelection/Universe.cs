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

using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a base class for all universes to derive from.
    /// </summary>
    public abstract class Universe
    {
        /// <summary>
        /// Gets the settings used for subscriptons added for this universe
        /// </summary>
        public abstract SubscriptionSettings SubscriptionSettings
        {
            get;
        }

        /// <summary>
        /// Gets the configuration used to get universe data
        /// </summary>
        public abstract SubscriptionDataConfig Configuration
        {
            get;
        }

        /// <summary>
        /// Performs universe selection using the data specified
        /// </summary>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public abstract IEnumerable<Symbol> SelectSymbols(IEnumerable<BaseData> data);
    }
}
