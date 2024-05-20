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

using System;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Represents a fee model specific to TradeStation.
    /// </summary>
    /// <see href="https://www.tradestation.com/pricing"/>
    /// <remarks>
    /// It is $0 for domestic and $5 for international clients for normal equities trades up to 10,000 shares, then $0.005 per share after.
    /// Options are $0.60 per contract, per side, and an extra $1 for index options
    /// </remarks>
    public class TradeStationFeeModel : FeeModel
    {
        /// <summary>
        /// Represents the fee associated with equity options transactions (per contract).
        /// </summary>
        private const decimal _equityOptionFee = 0.6m;

        /// <summary>
        /// Represents the fee associated with futures transactions (per contract, per side).
        /// </summary>
        private const decimal _futuresFee = 1.5m;

        /// <summary>
        /// Gets the commission per trade based on the residency status of the entity or person.
        /// </summary>
        private decimal CommissionPerTrade => USResident ? 0m : 5.0m;

        /// <summary>
        /// Gets or sets a value indicating whether the entity or person is a resident of the United States.
        /// </summary>
        /// <value>
        /// <c>true</c> if the entity or person is a US resident; otherwise, <c>false</c>.
        /// </value>
        public bool USResident { get; set; } = true;

        /// <summary>
        /// Calculates the order fee based on the security type and order parameters.
        /// </summary>
        /// <param name="parameters">The parameters for the order fee calculation, which include security and order details.</param>
        /// <returns>
        /// An <see cref="OrderFee"/> instance representing the calculated order fee.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameters"/> is <c>null</c>.
        /// </exception>
        public override OrderFee GetOrderFee(OrderFeeParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "Order fee parameters cannot be null.");
            }

            switch (parameters.Security.Type)
            {
                case SecurityType.Option:
                    return new OrderFee(new CashAmount(CommissionPerTrade + parameters.Order.AbsoluteQuantity * _equityOptionFee, Currencies.USD));
                case SecurityType.Future:
                    return new OrderFee(new CashAmount(parameters.Order.AbsoluteQuantity * _futuresFee, Currencies.USD));
                default:
                    return new OrderFee(new CashAmount(CommissionPerTrade, Currencies.USD));
            }
        }
    }
}
