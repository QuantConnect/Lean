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
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Shortable
{
    /// <summary>
    /// Defines the default shortable provider in the case that no local data exists.
    /// This will allow for all assets to be infinitely shortable, with no restrictions.
    /// </summary>
    public class NullShortableProvider : IShortableProvider
    {
        /// <summary>
        /// Gets the quantity shortable for the Symbol at the given time.
        /// </summary>
        /// <param name="symbol">Symbol to check</param>
        /// <param name="localTime">Local time of the algorithm</param>
        /// <returns>null, indicating that it is infinitely shortable</returns>
        public long? ShortableQuantity(Symbol symbol, DateTime localTime)
        {
            return null;
        }
    }
}
