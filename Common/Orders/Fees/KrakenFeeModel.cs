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


using System.Collections.Generic;
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Provides an implementation of <see cref="FeeModel"/> that models Kraken order fees
    /// </summary>
    public class KrakenFeeModel : FeeModel
    {
        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#kraken-pro
        /// </summary>
        public const decimal MakerTier1CryptoFee = 0.0016m;

        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#kraken-pro
        /// </summary>
        public const decimal TakerTier1CryptoFee = 0.0026m;

        /// <summary>
        /// We don't use 30 day model, so using only tier1 fees.
        /// https://www.kraken.com/features/fee-schedule#stablecoin-fx-pairs
        /// </summary>
        public const decimal Tier1FxFee = 0.002m;

        /// <summary>
        /// Fiats and stablecoins list that have own fee.
        /// </summary>
        public List<string> FxStablecoinList { get; init; } =
            new() { "CAD", "EUR", "GBP", "JPY", "USD", "USDT", "DAI", "USDC" };

        /// <summary>
        /// Get the fee for this order.
        /// If sell - fees in base currency
        /// If buy - fees in quote currency
        /// It can be defined manually in <see cref="KrakenOrderProperties"/>
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The fee of the order</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            var isBuy = order.Direction == OrderDirection.Buy;
            var unitPrice = isBuy ? security.AskPrice : security.BidPrice;

            if (order.Type == OrderType.Limit)
            {
                // limit order posted to the order book
                unitPrice = ((LimitOrder)order).LimitPrice;
            }

            unitPrice *= security.SymbolProperties.ContractMultiplier;

            var fee = TakerTier1CryptoFee;

            var props = order.Properties as KrakenOrderProperties;

            if (order.Type == OrderType.Limit && (props?.PostOnly == true || !order.IsMarketable))
            {
                // limit order posted to the order book
                fee = MakerTier1CryptoFee;
            }

            if (isBuy && props?.FeeInBase == true)
            {
                isBuy = false;
            }

            if (!isBuy && props?.FeeInQuote == true)
            {
                isBuy = true;
            }

            if (FxStablecoinList.Any(i => security.Symbol.Value.StartsWith(i)))
            {
                fee = Tier1FxFee;
            }
            string actualBaseCurrency;
            string actualQuoteCurrency;

            CurrencyPairUtil.DecomposeCurrencyPair(
                security.Symbol,
                out actualBaseCurrency,
                out actualQuoteCurrency
            );

            return new OrderFee(
                new CashAmount(
                    isBuy
                        ? unitPrice * order.AbsoluteQuantity * fee
                        : 1 * order.AbsoluteQuantity * fee,
                    isBuy ? actualQuoteCurrency : actualBaseCurrency
                )
            );
        }
    }
}
