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
using System.Collections.Generic;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines a short list/easy-to-borrow provider
    /// </summary>
    public interface IShortableProvider
    {
        /// <summary>
        /// Gets all shortable Symbols at the given time
        /// </summary>
        /// <param name="localTime">Local time of the algorithm</param>
        /// <returns>All shortable Symbols including the quantity shortable as a positive number at the given time. Null if all Symbols are shortable without restrictions.</returns>
        Dictionary<Symbol, long> AllShortableSymbols(DateTime localTime);

        /// <summary>
        /// Gets the quantity shortable for a <see cref="Symbol"/>.
        /// </summary>
        /// <param name="symbol">Symbol to check shortable quantity</param>
        /// <param name="localTime">Local time of the algorithm</param>
        /// <returns>The quantity shortable for the given Symbol as a positive number. Null if the Symbol is shortable without restrictions.</returns>
        long? ShortableQuantity(Symbol symbol, DateTime localTime);
    }
}
