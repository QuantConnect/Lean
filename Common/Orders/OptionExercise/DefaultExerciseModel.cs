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
using static QuantConnect.Extensions;

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
            var underlying = option.Underlying;
            var utcTime = option.LocalTime.ConvertToUtc(option.Exchange.TimeZone);

            var inTheMoney = option.IsAutoExercised(underlying.Close);
            var isAssignment = inTheMoney && option.Holdings.IsShort;

            yield return new OrderEvent(
                order.Id,
                option.Symbol,
                utcTime,
                OrderStatus.Filled,
                GetOrderDirection(order.Quantity),
                0.0m,
                order.Quantity,
                OrderFee.Zero,
                GetContractHoldingsAdjustmentFillTag(inTheMoney, isAssignment)
            ) { IsAssignment = isAssignment };

            // TODO : Support Manual Exercise of OTM contracts [ inTheMoney = false ]
            if (inTheMoney && option.ExerciseSettlement == SettlementType.PhysicalDelivery)
            {
                var exerciseQuantity = option.GetExerciseQuantity(order.Quantity);

                yield return new OrderEvent(
                    order.Id,
                    underlying.Symbol,
                    utcTime,
                    OrderStatus.Filled,
                    GetOrderDirection(exerciseQuantity),
                    order.Price,
                    exerciseQuantity,
                    OrderFee.Zero,
                    isAssignment ? "Option Assignment" : "Option Exercise"
                );
            }
        }

        private static string GetContractHoldingsAdjustmentFillTag(bool inTheMoney, bool isAssignment)
        {
            var action = isAssignment ? "Assignment" : "Exercise";
            if (inTheMoney)
            {
                return $"Automatic {action}";
            }

            return "OTM";
        }
    }
}
