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
    /// Provides an implementation of <see cref="FeeModel"/> that models Binance order fees
    /// </summary>
    public class BinanceFeeModel : FeeModel
    {
        /// <summary>
        /// Tier 1 maker fees
        /// https://www.binance.com/en/fee/schedule
        /// </summary>
        public const decimal MakerFee = 0.001m;
        /// <summary>
        /// Tier 1 taker fees
        /// https://www.binance.com/en/fee/schedule
        /// </summary>
        public const decimal TakerFee = 0.001m;

        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var security = parameters.Security;
            var order = parameters.Order;

            decimal fee = TakerFee;
            var props = order.Properties as BinanceOrderProperties;

            if (order.Type == OrderType.Limit &&
                (props?.PostOnly == true || !order.IsMarketable))
            {
                // limit order posted to the order book
                fee = MakerFee;
            }

            // get order value in quote currency
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            if (order.Type == OrderType.Limit)
            {
                // limit order posted to the order book
                unitPrice = ((LimitOrder)order).LimitPrice;
            }

            unitPrice *= security.SymbolProperties.ContractMultiplier;

            // apply fee factor, currently we do not model 30-day volume, so we use the first tier
            return new OrderFee(new CashAmount(
                unitPrice * order.AbsoluteQuantity * fee,
                security.QuoteCurrency.Symbol));
        }
    }
}
