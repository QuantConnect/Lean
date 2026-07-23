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
    /// Represents a fee model specific to Public.com.
    /// </summary>
    /// <see href="https://public.com/disclosures/fee-schedule"/>
    /// <remarks>
    /// Equity trades are free during regular market hours and $2.99 per trade during extended hours.
    /// Options on stocks and ETFs are free; index options cost $0.50 per contract.
    /// Crypto trades carry a fee that depends on the order amount in USD.
    /// The model uses the regular member tier and does not detect OTC stocks.
    /// </remarks>
    public class PublicFeeModel : FeeModel
    {
        /// <summary>
        /// Flat per-trade fee for US-listed equity trades placed during extended market hours.
        /// </summary>
        private const decimal _extendedHoursEquityFee = 2.99m;

        /// <summary>
        /// Per-contract fee for index options (regular member tier).
        /// </summary>
        private const decimal _indexOptionContractFee = 0.50m;

        /// <summary>
        /// Crypto fee charged on orders above the flat-tier range: 1.25% of the order amount.
        /// </summary>
        private const decimal _cryptoPercentFee = 0.0125m;

        /// <summary>
        /// Gets the order fee for a given security and order.
        /// </summary>
        /// <param name="parameters">The parameters including the security and order details.</param>
        /// <returns>A <see cref="OrderFee"/> in USD for the order.</returns>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            var order = parameters.Order;
            var security = parameters.Security;

            decimal fee;
            switch (security.Type)
            {
                case SecurityType.Equity:
                    fee = GetEquityFee(security, order);
                    break;

                case SecurityType.IndexOption:
                    fee = order.AbsoluteQuantity * _indexOptionContractFee;
                    break;

                case SecurityType.Crypto:
                    fee = GetCryptoFee(security, order);
                    break;

                default:
                    // Options on stocks and ETFs are commission-free on Public.com.
                    return OrderFee.Zero;
            }

            return new OrderFee(new CashAmount(fee, Currencies.USD));
        }

        /// <summary>
        /// Returns the equity fee: free during regular market hours, a flat fee during extended hours.
        /// </summary>
        /// <param name="security">The traded security.</param>
        /// <param name="order">The order, whose time decides whether the trade is during regular hours.</param>
        /// <returns>The equity fee in USD.</returns>
        private static decimal GetEquityFee(Security security, Order order)
        {
            var localOrderTime = order.Time.ConvertFromUtc(security.Exchange.TimeZone);
            var isRegularHours = security.Exchange.Hours.IsOpen(localOrderTime, extendedMarketHours: false);
            return isRegularHours ? 0m : _extendedHoursEquityFee;
        }

        /// <summary>
        /// Returns the crypto fee for the order, tiered by the order amount in USD.
        /// </summary>
        /// <param name="security">The traded security.</param>
        /// <param name="order">The order being placed.</param>
        /// <returns>The crypto fee in USD.</returns>
        private static decimal GetCryptoFee(Security security, Order order)
        {
            var orderAmount = security.Price * security.SymbolProperties.ContractMultiplier * order.AbsoluteQuantity;

            if (orderAmount <= 10m)
            {
                return 0.49m;
            }
            if (orderAmount <= 25m)
            {
                return 0.69m;
            }
            if (orderAmount <= 50m)
            {
                return 1.19m;
            }
            if (orderAmount <= 100m)
            {
                return 1.69m;
            }
            if (orderAmount <= 250m)
            {
                return 3.29m;
            }
            if (orderAmount <= 500m)
            {
                return 6.29m;
            }
            return orderAmount * _cryptoPercentFee;
        }
    }
}
