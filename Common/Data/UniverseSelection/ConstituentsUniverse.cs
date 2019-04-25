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
using System.Linq;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// ConstituentsUniverse allows to perform universe selection based on an
    /// already pre selected set of <see cref="Symbol"/>.
    /// </summary>
    /// <remarks>Using this class allows a performance improvement, since there is no
    /// runtime logic computation required for selecting the <see cref="Symbol"/></remarks>
    public class ConstituentsUniverse : FuncUniverse
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="type">The <see cref="ConstituentsUniverseData"/> type to use</param>
        public ConstituentsUniverse(Symbol symbol, UniverseSettings universeSettings, Type type = null)
           : this(new SubscriptionDataConfig(type ?? typeof(ConstituentsUniverseData),
                   symbol,
                   Resolution.Daily,
                   TimeZones.NewYork,
                   TimeZones.NewYork,
                   false,
                   false,
                   true,
                   true),
               universeSettings)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="subscriptionDataConfig">The universe configuration to use</param>
        /// <param name="universeSettings">The universe settings to use</param>
        public ConstituentsUniverse(SubscriptionDataConfig subscriptionDataConfig, UniverseSettings universeSettings)
            : base(subscriptionDataConfig,
                universeSettings,
                constituents =>
                {
                    return constituents
                        .Where(data => data.Symbol != Symbol.Empty) // allow no selection
                        .Select(baseData => baseData.Symbol);
                })
        {
        }
    }
}
