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
    /// Provides an implementation of <see cref="FeeModel"/> that models GDAX order fees
    /// </summary>
    public class GDAXFeeModel : FeeModel
    {
        /// <summary>
        /// Tier 1 taker fees
        /// https://www.gdax.com/fees
        /// </summary>
        public const decimal TakerFee = 0.003m;

        /// <summary>
        /// Get the fee for this order in units of the account currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // marketable limit orders are considered takers
            decimal fee = 0;
            // check limit order posted to the order book, 0% maker fee
            if (!(order.Type == OrderType.Limit && !order.IsMarketable))
            {
                // get order value in account currency, then apply taker fee factor
                var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
                unitPrice *= security.QuoteCurrency.ConversionRate * security.SymbolProperties.ContractMultiplier;

                // currently we do not model 30-day volume, so we use the first tier

                fee = unitPrice * order.AbsoluteQuantity * TakerFee;
            }
            return new OrderFee(new CashAmount(
                fee,
                security.QuoteCurrency.AccountCurrency));
        }
    }
}
