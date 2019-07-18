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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities.Option;

namespace QuantConnect.Orders.OptionExercise
{
    /// <summary>
    /// Represents the default option exercise model (physical, cash settlement)
    /// </summary>
    public class DefaultExerciseModel : IOptionExerciseModel
    {
        /// <summary>
        /// Default option exercise model for the basic equity/index option security class.
        /// </summary>
        /// <param name="option">Option we're trading this order</param>
        /// <param name="order">Order to update</param>
        public IEnumerable<OrderEvent> OptionExercise(Option option, OptionExerciseOrder order)
        {
            var utcTime = option.LocalTime.ConvertToUtc(option.Exchange.TimeZone);

            var optionQuantity = order.Quantity;
            var assignment = order.Quantity < 0;
            var underlying = option.Underlying;
            var exercisePrice = order.Price;
            var fillQuantity = option.GetExerciseQuantity(order.Quantity);
            var exerciseQuantity =
                    option.Symbol.ID.OptionRight == OptionRight.Call ? fillQuantity : -fillQuantity;
            var exerciseDirection = assignment?
                    (option.Symbol.ID.OptionRight == OptionRight.Call ? OrderDirection.Sell : OrderDirection.Buy):
                    (option.Symbol.ID.OptionRight == OptionRight.Call ? OrderDirection.Buy : OrderDirection.Sell);

            var addUnderlyingEvent = new OrderEvent(order.Id,
                            underlying.Symbol,
                            utcTime,
                            OrderStatus.Filled,
                            exerciseDirection,
                            exercisePrice,
                            exerciseQuantity,
                            OrderFee.Zero,
                            "Option Exercise/Assignment");

            var optionRemoveEvent = new OrderEvent(order.Id,
                            option.Symbol,
                            utcTime,
                            OrderStatus.Filled,
                            assignment ? OrderDirection.Buy : OrderDirection.Sell,
                            0.0m,
                            -optionQuantity,
                            OrderFee.Zero,
                            "Adjusting(or removing) the exercised/assigned option");

            if (optionRemoveEvent.FillQuantity > 0)
            {
                optionRemoveEvent.IsAssignment = true;
            }

            if (option.ExerciseSettlement == SettlementType.PhysicalDelivery &&
                option.IsAutoExercised(underlying.Close))
            {
                return new[] { optionRemoveEvent, addUnderlyingEvent };
            }

            return new[] { optionRemoveEvent };
        }

    }
}
