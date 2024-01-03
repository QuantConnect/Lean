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
        /// Level Advanced 1 maker fee
        /// Tab "Fee tiers" on <see href="https://www.coinbase.com/advanced-fees"/>
        /// </summary>
        public const decimal MakerAdvanced1 = 0.006m;

        /// <summary>
        /// Level Advanced 1 taker fee
        /// Tab "Fee tiers" on <see href="https://www.coinbase.com/advanced-fees"/>
        /// </summary>
        public const decimal TakerAdvanced1 = 0.008m;

        /// <summary>
        /// Stable Pairs maker fee
        /// Tab "Stable pairs" on <see href="https://www.coinbase.com/advanced-fees"/>
        /// </summary>
        public const decimal MakerStablePairs = 0m;

        /// <summary>
        /// Stable Pairs taker fee
        /// Tab "Stable pairs" on <see href="https://www.coinbase.com/advanced-fees"/>
        /// </summary>
        public const decimal TakerStableParis = 0.00001m;

        private readonly decimal _makerFee;

        private readonly decimal _takerFee;

        /// <summary>
        /// Create Coinbase Fee model setting fee values
        /// </summary>
        /// <param name="makerFee">Maker fee value</param>
        /// <param name="takerFee">Taker fee value</param>
        /// <remarks>By default: use Level Advanced 1 fees</remarks>
        public CoinbaseFeeModel(decimal makerFee = MakerAdvanced1, decimal takerFee = TakerAdvanced1)
        {
            _makerFee = makerFee;
            _takerFee = takerFee;
        }

        /// <summary>
        /// Get the fee for this order in quote currency
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in quote currency</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "The 'parameters' argument cannot be null.");
            }

            var order = parameters.Order;
            var security = parameters.Security;
            var props = order.Properties as CoinbaseOrderProperties;

            // marketable limit orders are considered takers
            var isMaker = order.Type == OrderType.Limit && ((props != null && props.PostOnly) || !order.IsMarketable);

            // Check if the current symbol is a StableCoin
            var isStableCoin = Currencies.StablePairsCoinbase.Contains(security.Symbol.Value);

            var feePercentage = GetFeePercentage(order.Time, isMaker, isStableCoin, _makerFee, _takerFee);

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
        /// <param name="makerFee">maker fee amount</param>
        /// <param name="takerFee">taker fee amount</param>
        /// <returns>The fee percentage</returns>
        protected static decimal GetFeePercentage(DateTime utcTime, bool isMaker, bool isStableCoin, decimal makerFee, decimal takerFee)
        {
            if (isStableCoin && utcTime < new DateTime(2022, 6, 1))
            {                
                return isMaker ? 0m : 0.001m;
            }
            else if(isStableCoin)
            {
                return isMaker ? MakerStablePairs : TakerStableParis;
            }
            else if (utcTime < new DateTime(2019, 3, 23, 1, 30, 0))
            {
                return isMaker ? 0m : 0.003m;
            }
            else if (utcTime < new DateTime(2019, 10, 8, 0, 30, 0))
            {
                return isMaker ? 0.0015m : 0.0025m;
            }

            // https://www.coinbase.com/advanced-fees
            // Level      | Trading amount  | Spot fees (Maker | Taker)
            // Advanced 1 |     >= $0       |       0.60% | 0.80%
            return isMaker ? makerFee : takerFee;
        }
    }
}
