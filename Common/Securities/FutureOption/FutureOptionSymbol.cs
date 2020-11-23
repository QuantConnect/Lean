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

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Static helper methods to resolve Futures Options Symbol-related tasks.
    /// </summary>
    public static class FutureOptionSymbol
    {
        /// <summary>
        /// Detects if the future option contract is standard, i.e. not weekly, not short-term, not mid-sized, etc.
        /// </summary>
        /// <param name="_">Symbol</param>
        /// <returns>true</returns>
        /// <remarks>
        /// We have no way of identifying the type of FOP contract based on the properties contained within the Symbol.
        /// </remarks>
        public static bool IsStandard(Symbol _) => true;

        /// <summary>
        /// Gets the last day of trading, aliased to be the Futures options' expiry
        /// </summary>
        /// <param name="symbol">Futures Options Symbol</param>
        /// <returns>Last day of trading date</returns>
        public static DateTime GetLastDayOfTrading(Symbol symbol) => symbol.ID.Date.Date;
    }
}
