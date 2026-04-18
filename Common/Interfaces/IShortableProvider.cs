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

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines a short list/easy-to-borrow provider
    /// </summary>
    [StubsAvoidImplicits]
    public interface IShortableProvider
    {
        /// <summary>
        /// Gets interest rate charged on borrowed shares for a given asset.
        /// </summary>
        /// <param name="symbol">Symbol to lookup fee rate</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Fee rate. Zero if the data for the brokerage/date does not exist.</returns>
        decimal FeeRate(Symbol symbol, DateTime localTime);

        /// <summary>
        /// Gets the Fed funds or other currency-relevant benchmark rate minus the interest rate charged on borrowed shares for a given asset.
        /// Interest rate - borrow fee rate = borrow rebate rate: 5.32% - 0.25% = 5.07%
        /// </summary>
        /// <param name="symbol">Symbol to lookup rebate rate</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Rebate fee. Zero if the data for the brokerage/date does not exist.</returns>
        decimal RebateRate(Symbol symbol, DateTime localTime);

        /// <summary>
        /// Gets the quantity shortable for a <see cref="Symbol"/>.
        /// </summary>
        /// <param name="symbol">Symbol to check shortable quantity</param>
        /// <param name="localTime">Local time of the algorithm</param>
        /// <returns>The quantity shortable for the given Symbol as a positive number. Null if the Symbol is shortable without restrictions.</returns>
        long? ShortableQuantity(Symbol symbol, DateTime localTime);
    }
}
