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

using System;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Option exercise order type definition
    /// </summary>
    public class OptionExerciseOrder : Order
    {
        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public OptionExerciseOrder()
        {
        }

        /// <summary>
        /// New option exercise order constructor. We model option exercising as an underlying asset long/short order with strike equal to limit price.
        /// This means that by exercising a call we get into long asset position, by exercising a put we get into short asset position.
        /// </summary>
        /// <param name="symbol">Option symbol we're seeking to exercise</param>
        /// <param name="quantity">Quantity of the option we're seeking to exercise. Must be a positive value.</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        public OptionExerciseOrder(Symbol symbol, decimal quantity, DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, time, tag, properties)
        {
            Price = Symbol.ID.StrikePrice;
        }

        /// <summary>
        /// Option Exercise Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.OptionExercise; }
        }

        /// <summary>
        /// Gets the order value in option contracts quoted in options's currency
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected override decimal GetValueImpl(Security security)
        {
            var option = (Option)security;

            return option.GetExerciseQuantity(Quantity) * Price  / option.SymbolProperties.ContractMultiplier;
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new OptionExerciseOrder();
            CopyTo(order);
            return order;
        }
    }
}