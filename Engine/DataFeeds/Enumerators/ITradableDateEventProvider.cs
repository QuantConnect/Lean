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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Interface for event providers for new tradable dates
    /// </summary>
    public interface ITradableDateEventProvider
    {
        /// <summary>
        /// Called each time there is a new tradable day
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New corporate event if any</returns>
        IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs);

        /// <summary>
        /// Initializes the event provider instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        void Initialize(
            SubscriptionDataConfig config,
            FactorFile factorFile,
            MapFile mapFile
            );
    }
}
