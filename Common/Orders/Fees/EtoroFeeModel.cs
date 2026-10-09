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
    /// Represents a fee model specific to eToro.
    /// </summary>
    /// <see href="https://www.etoro.com/en-us/trading/fees/"/>
    /// <remarks>
    /// Stock and ETF trades are commission free. Crypto trades pay 1% of the trade value on every buy and sell.
    /// The stock commission on the global fees page (https://www.etoro.com/trading/fees/) depends on the country
    /// of residence and the exchange; the model uses the US page, which charges nothing.
    /// </remarks>
    public class EtoroFeeModel : FeeModel
    {
        /// <summary>
        /// Crypto fee rate on the trade value, charged on every buy and sell.
        /// From https://www.etoro.com/en-us/trading/fees/:
        /// "A standardized 1% commission is charged each time you buy or sell a cryptoasset on etoro."
        /// </summary>
        private const decimal _cryptoFeeRate = 0.01m;

        /// <summary>
        /// Gets the order fee for a given security and order.
        /// </summary>
        /// <param name="parameters">The parameters including the security and order details.</param>
        /// <returns>A <see cref="OrderFee"/> in USD for the order.</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            // From https://www.etoro.com/en-us/trading/fees/: "We charge zero commissions when you buy or sell a stock."
            if (security.Type != SecurityType.Crypto)
            {
                return OrderFee.Zero;
            }

            var tradeValue = security.Price * security.SymbolProperties.ContractMultiplier * order.AbsoluteQuantity;
            return new OrderFee(new CashAmount(tradeValue * _cryptoFeeRate, Currencies.USD));
        }
    }
}
