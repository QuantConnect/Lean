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

using static System.Math;

namespace QuantConnect.Securities.Forex
{
    /// <summary>
    /// FOREX holdings implementation of the base securities class
    /// </summary>
    /// <seealso cref="SecurityHolding"/>
    public class ForexHolding : SecurityHolding
    {
        /// <summary>
        /// Forex Holding Class
        /// </summary>
        /// <param name="security">The forex security being held</param>
        /// <param name="currencyConverter">A currency converter instance</param>
        public ForexHolding(Forex security, ICurrencyConverter currencyConverter)
            : base(security, currencyConverter)
        {
        }

        /// <summary>
        /// Profit in pips if we closed the holdings right now including the approximate fees
        /// </summary>
        public decimal TotalCloseProfitPips()
        {
            var pipDecimal = Security.SymbolProperties.MinimumPriceVariation * 10;
            var exchangeRate = Security.QuoteCurrency.ConversionRate;

            var pipCashCurrencyValue = (pipDecimal * AbsoluteQuantity * exchangeRate);
            return Round((TotalCloseProfit() / pipCashCurrencyValue), 1);
        }
    }
}