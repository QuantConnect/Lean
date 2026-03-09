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
using QuantConnect.Securities.Crypto;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents the fee model specific to Alpaca trading platform.
    /// </summary>
    /// <remarks>This class inherits from <see cref="FeeModel"/> and provides the fee structure for Alpaca trades.
    /// It implements the <see cref="IFeeModel"/> interface and should be used for calculating fees on the Alpaca platform.</remarks>
    public class AlpacaFeeModel : FeeModel
    {
        /// <summary>
        /// The fee percentage for a maker transaction in cryptocurrency.
        /// </summary>
        /// <see href="https://docs.alpaca.markets/docs/crypto-fees"/>
        public const decimal MakerCryptoFee = 0.0015m;

        /// <summary>
        /// The fee percentage for a taker transaction in cryptocurrency.
        /// </summary>
        public const decimal TakerCryptoFee = 0.0025m;

        /// <summary>
        /// Gets the order fee associated with the specified order.
        /// </summary>
        /// <param name="parameters">A <see cref="OrderFeeParameters"/> object
        /// containing the security and order</param>
        /// <returns>The cost of the order in a <see cref="CashAmount"/> instance</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var security = parameters.Security;
            if (security.Symbol.ID.SecurityType == SecurityType.Crypto)
            {
                var order = parameters.Order;
                var fee = GetFee(order, MakerCryptoFee, TakerCryptoFee);
                CashAmount cashAmount;
                Crypto.DecomposeCurrencyPair(security.Symbol, security.SymbolProperties, out var baseCurrency, out var quoteCurrency);
                if (order.Direction == OrderDirection.Buy)
                {
                    // base currency, deducted from what we bought
                    cashAmount = new CashAmount(order.AbsoluteQuantity * fee, baseCurrency);
                }
                else
                {
                    // quote currency
                    var positionValue = order.AbsoluteQuantity * security.Price;
                    cashAmount = new CashAmount(positionValue * fee, quoteCurrency);
                }
                return new OrderFee(cashAmount);
            }
            return new OrderFee(new CashAmount(0, Currencies.USD));
        }

        /// <summary>
        /// Calculates the fee for a given order based on whether it is a maker or taker order.
        /// </summary>
        /// <param name="order">The order for which the fee is being calculated.</param>
        /// <param name="makerFee">The fee percentage for maker orders.</param>
        /// <param name="takerFee">The fee percentage for taker orders.</param>
        /// <returns>The calculated fee for the given order.</returns>
        private static decimal GetFee(Order order, decimal makerFee, decimal takerFee)
        {
            if (order.Type == OrderType.Limit && !order.IsMarketable)
            {
                return makerFee;
            }
            return takerFee;
        }
    }
}
