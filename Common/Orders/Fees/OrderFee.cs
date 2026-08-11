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

using Newtonsoft.Json;
using ProtoBuf;
using QuantConnect.Securities;

namespace QuantConnect.Orders.Fees
{
    /// <summary>
    /// Defines the result for <see cref="IFeeModel.GetOrderFee"/>
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class OrderFee
    {
        /// <summary>
        /// Gets the order fee
        /// </summary>
        [ProtoMember(1)]
        public CashAmount Value { get; set; }

        /// <summary>
        /// Gets the order fee amount, shortcut for the <see cref="CashAmount.Amount"/> of <see cref="Value"/>.
        /// The two-level 'Value.Amount' is hard to discover, especially from Python ('order_fee.value.amount')
        /// </summary>
        [JsonIgnore]
        public decimal Amount => Value.Amount;

        /// <summary>
        /// Gets the order fee currency, shortcut for the <see cref="CashAmount.Currency"/> of <see cref="Value"/>
        /// </summary>
        [JsonIgnore]
        public string Currency => Value.Currency;

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
        /// Applies the order fee to the given portfolio
        /// </summary>
        /// <param name="portfolio">The portfolio instance</param>
        /// <param name="fill">The order fill event</param>
        public virtual void ApplyToPortfolio(SecurityPortfolioManager portfolio, OrderEvent fill)
        {
            portfolio.CashBook[Value.Currency].AddAmount(-Value.Amount);
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

        // Numeric operators delegating to the fee amount. In C# these mirror what the implicit
        // decimal conversion above already allowed, so semantics are unchanged. Their real purpose
        // is Python: pythonnet maps C# operators to __add__/__radd__/__gt__/... so summing or
        // comparing fees works instead of raising TypeError. Note float(fee) is still not supported
        // (pythonnet does not wire the nb_float slot for CLR types), use the 'Amount' property instead.

        /// <summary>Adds two order fee amounts</summary>
        public static decimal operator +(OrderFee a, OrderFee b) => a.Value.Amount + b.Value.Amount;
        /// <summary>Adds a value to the order fee amount</summary>
        public static decimal operator +(OrderFee fee, decimal value) => fee.Value.Amount + value;
        /// <summary>Adds the order fee amount to a value</summary>
        public static decimal operator +(decimal value, OrderFee fee) => value + fee.Value.Amount;

        /// <summary>Subtracts two order fee amounts</summary>
        public static decimal operator -(OrderFee a, OrderFee b) => a.Value.Amount - b.Value.Amount;
        /// <summary>Subtracts a value from the order fee amount</summary>
        public static decimal operator -(OrderFee fee, decimal value) => fee.Value.Amount - value;
        /// <summary>Subtracts the order fee amount from a value</summary>
        public static decimal operator -(decimal value, OrderFee fee) => value - fee.Value.Amount;

        /// <summary>Multiplies two order fee amounts</summary>
        public static decimal operator *(OrderFee a, OrderFee b) => a.Value.Amount * b.Value.Amount;
        /// <summary>Multiplies the order fee amount by a value</summary>
        public static decimal operator *(OrderFee fee, decimal value) => fee.Value.Amount * value;
        /// <summary>Multiplies a value by the order fee amount</summary>
        public static decimal operator *(decimal value, OrderFee fee) => value * fee.Value.Amount;

        /// <summary>Divides two order fee amounts</summary>
        public static decimal operator /(OrderFee a, OrderFee b) => a.Value.Amount / b.Value.Amount;
        /// <summary>Divides the order fee amount by a value</summary>
        public static decimal operator /(OrderFee fee, decimal value) => fee.Value.Amount / value;
        /// <summary>Divides a value by the order fee amount</summary>
        public static decimal operator /(decimal value, OrderFee fee) => value / fee.Value.Amount;

        /// <summary>Determines whether one order fee amount is less than another</summary>
        public static bool operator <(OrderFee a, OrderFee b) => a.Value.Amount < b.Value.Amount;
        /// <summary>Determines whether one order fee amount is greater than another</summary>
        public static bool operator >(OrderFee a, OrderFee b) => a.Value.Amount > b.Value.Amount;
        /// <summary>Determines whether one order fee amount is less than or equal to another</summary>
        public static bool operator <=(OrderFee a, OrderFee b) => a.Value.Amount <= b.Value.Amount;
        /// <summary>Determines whether one order fee amount is greater than or equal to another</summary>
        public static bool operator >=(OrderFee a, OrderFee b) => a.Value.Amount >= b.Value.Amount;

        /// <summary>Determines whether the order fee amount is less than the given value</summary>
        public static bool operator <(OrderFee fee, decimal value) => fee.Value.Amount < value;
        /// <summary>Determines whether the order fee amount is greater than the given value</summary>
        public static bool operator >(OrderFee fee, decimal value) => fee.Value.Amount > value;
        /// <summary>Determines whether the order fee amount is less than or equal to the given value</summary>
        public static bool operator <=(OrderFee fee, decimal value) => fee.Value.Amount <= value;
        /// <summary>Determines whether the order fee amount is greater than or equal to the given value</summary>
        public static bool operator >=(OrderFee fee, decimal value) => fee.Value.Amount >= value;

        /// <summary>
        /// Gets an instance of <see cref="OrderFee"/> that represents zero.
        /// </summary>
        public static readonly OrderFee Zero =
            new OrderFee(new CashAmount(0, Currencies.NullCurrency));
    }
}
