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
    /// Represents a fee model specific to Charles Schwab.
    /// </summary>
    /// <see href="https://www.schwab.com/pricing"/>
    public class CharlesSchwabFeeModel : FeeModel
    {
        /// <summary>
        /// The exchange processing fee for standard option securities.
        /// </summary>
        private const decimal _optionIndexFee = 1m;

        /// <summary>
        /// Represents the fee associated with equity options transactions (per contract).
        /// </summary>
        private const decimal _optionFee = 0.65m;

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
            if (parameters.Security.Type.IsOption())
            {
                var feeRate = parameters.Security.Type switch
                {
                    SecurityType.IndexOption => _optionIndexFee,
                    SecurityType.Option => _optionFee,
                    _ => 0m
                };
                return new OrderFee(new CashAmount(parameters.Order.AbsoluteQuantity * feeRate, Currencies.USD));
            }
            return OrderFee.Zero;
        }
    }
}
