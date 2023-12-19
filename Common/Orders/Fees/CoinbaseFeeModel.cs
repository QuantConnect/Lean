/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014-2023 QuantConnect Corporation.
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
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to Coinbase.
    /// This class extends the base fee model.
    /// </summary>
    public class CoinbaseFeeModel : FeeModel
    {
        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if(parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "The 'parameters' argument cannot be null.");
            }

            var order = parameters.Order;
            var security = parameters.Security;

            // marketable limit orders are considered takers
            var isMaker = order.Type == OrderType.Limit && !order.IsMarketable;

            // Check if the current symbol is a StableCoin
            var isStableCoin = Currencies.StablePairsCoinbase.Contains(security.Symbol.Value);

            var feePercentage = GetFeePercentage(order.Time, isMaker, isStableCoin);

            // get order value in quote currency, then apply maker/taker fee factor
            var unitPrice = order.Direction == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
            unitPrice *= security.SymbolProperties.ContractMultiplier;

            // currently we do not model 30-day volume, so we use the first tier

            var fee = unitPrice * order.AbsoluteQuantity * feePercentage;

            return new OrderFee(new CashAmount(fee, security.QuoteCurrency.Symbol));
        }

        /// <summary>
        /// Returns the maker/taker fee percentage effective at the requested date.
        /// </summary>
        /// <param name="utcTime">The date/time requested (UTC)</param>
        /// <param name="isMaker">true if the maker percentage fee is requested, false otherwise</param>
        /// <param name="isStableCoin">true if the order security symbol is a StableCoin, false otherwise</param>
        /// <returns>The fee percentage effective at the requested date</returns>
        public static decimal GetFeePercentage(DateTime utcTime, bool isMaker, bool isStableCoin)
        {
            // Tier 1 fees
            // https://pro.coinbase.com/orders/fees
            // https://blog.coinbase.com/coinbase-pro-market-structure-update-fbd9d49f43d7
            // https://blog.coinbase.com/updates-to-coinbase-pro-fee-structure-b3d9ee586108
            if (isStableCoin)
                return isMaker ? 0m : 0.001m;

            else if (utcTime < new DateTime(2019, 3, 23, 1, 30, 0))
                return isMaker ? 0m : 0.003m;

            else if (utcTime < new DateTime(2019, 10, 8, 0, 30, 0))
                return isMaker ? 0.0015m : 0.0025m;

            return isMaker ? 0.005m : 0.005m;
        }
    }
}
