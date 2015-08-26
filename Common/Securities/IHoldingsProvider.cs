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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a type capable of fetching the holdings for the specified symbol
    /// </summary>
    public interface IHoldingsProvider
    {
        /// <summary>
        /// Retrieves a summary of the holdings for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to get holdings for</param>
        /// <returns>The holdings for the symbol or null if the symbol is invalid and/or not in the portfolio</returns>
        Holding GetHoldings(Symbol symbol);
    }

    /// <summary>
    /// Provides extension methods for the <see cref="IHoldingsProvider"/> interface.
    /// </summary>
    public static class HoldingsProviderExtensions
    {
        /// <summary>
        /// Extension method to return the quantity of holdings, if no holdings are present, then zero is returned.
        /// </summary>
        /// <param name="provider">The <see cref="IHoldingsProvider"/></param>
        /// <param name="symbol">The symbol we want holdings quantity for</param>
        /// <returns>The quantity of holdings for the specified symbol</returns>
        public static decimal GetHoldingsQuantity(this IHoldingsProvider provider, Symbol symbol)
        {
            var holding = provider.GetHoldings(symbol);
            return holding == null ? 0 : holding.Quantity;
        }
    }
}