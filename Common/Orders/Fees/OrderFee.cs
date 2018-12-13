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
    /// Defines the result for <see cref="IFeeModel.GetOrderFee"/>
    /// </summary>
    public class OrderFee
    {
        /// <summary>
        /// Gets the order fee
        /// </summary>
        public CashAmount Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderFee"/> class
        /// </summary>
        /// <param name="orderFee">The order fee</param>
        public OrderFee(CashAmount orderFee)
        {
            Value = new CashAmount(
                orderFee.Amount.Normalize(),
                orderFee.Currency);
        }

        /// <summary>
        /// This is for backward compatibility with old 'decimal' order fee
        /// </summary>
        public override string ToString()
        {
            return $"{Value.Amount} {Value.Currency}";
        }

        /// <summary>
        /// This is for backward compatibility with old 'decimal' order fee
        /// </summary>
        public static implicit operator decimal(OrderFee m)
        {
            return m.Value.Amount;
        }

        /// <summary>
        /// Gets an instance of <see cref="OrderFee"/> that represents zero.
        /// </summary>
        public static readonly OrderFee Zero =
            new OrderFee(new CashAmount(0, Currencies.NullCurrency));
    }
}