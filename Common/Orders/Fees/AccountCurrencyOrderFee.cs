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

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// An order fee that is applied to the portfolio in the account currency, converting the fee
    /// from the currency it was computed in.
    /// </summary>
    /// <remarks>
    /// Brokerages that hold a single account currency and convert on every trade charge their fees
    /// in that currency, so a fee computed in the instrument's quote currency should not leave a
    /// balance there.
    /// </remarks>
    public class AccountCurrencyOrderFee : OrderFee
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountCurrencyOrderFee"/> class
        /// </summary>
        /// <param name="orderFee">The order fee, in the currency it was computed in</param>
        public AccountCurrencyOrderFee(CashAmount orderFee)
            : base(orderFee)
        {
        }

        /// <summary>
        /// Applies the order fee to the given portfolio, in the account currency
        /// </summary>
        /// <param name="portfolio">The portfolio instance</param>
        /// <param name="fill">The order fill event</param>
        public override void ApplyToPortfolio(SecurityPortfolioManager portfolio, OrderEvent fill)
        {
            var amountInAccountCurrency = portfolio.CashBook.ConvertToAccountCurrency(Value.Amount, Value.Currency);
            portfolio.CashBook[portfolio.CashBook.AccountCurrency].AddAmount(-amountInAccountCurrency);
        }
    }
}
