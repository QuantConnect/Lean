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

using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// An order fee where the fee quantity has already been subtracted from the filled quantity
    /// </summary>
    /// <remarks>
    /// This type of order fee is returned by some crypto brokerages (e.g. Bitfinex and Binance)
    /// with buy orders with cash accounts.
    /// </remarks>
    public class ModifiedFillQuantityOrderFee : OrderFee
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifiedFillQuantityOrderFee"/> class
        /// </summary>
        /// <param name="orderFee">The order fee</param>
        public ModifiedFillQuantityOrderFee(CashAmount orderFee)
            : base(orderFee)
        {
        }

        /// <summary>
        /// Applies the order fee to the given portfolio
        /// </summary>
        /// <param name="portfolio">The portfolio instance</param>
        /// <param name="fill">The order fill event</param>
        public override void ApplyToPortfolio(SecurityPortfolioManager portfolio, OrderEvent fill)
        {
            // do not apply the fee twice
        }
    }
}
