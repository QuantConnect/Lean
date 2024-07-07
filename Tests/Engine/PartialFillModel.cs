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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Implements a custom fill model that inherit from FillModel. Override the MarketFill method to simulate partially fill orders
    /// </summary>
    internal class PartialFillModel : FillModel
    {
        private readonly QCAlgorithm _algorithm;
        private readonly Dictionary<int, decimal> _absoluteRemainingByOrderId;
        private readonly decimal _rate;

        public PartialFillModel(QCAlgorithm algorithm, decimal rate = 1.0m)
        {
            _algorithm = algorithm;
            _rate = rate;
            _absoluteRemainingByOrderId = new Dictionary<int, decimal>();
        }

        public override OrderEvent MarketFill(Security asset, MarketOrder order)
        {
            decimal absoluteRemaining;
            if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
            {
                absoluteRemaining = order.AbsoluteQuantity;
                _absoluteRemainingByOrderId.Add(order.Id, order.AbsoluteQuantity);
            }

            // Create the object
            var fill = base.MarketFill(asset, order);

            // Set this fill amount
            var absoluteFillQuantity = (int)(
                Math.Min(absoluteRemaining, _rate * (int)order.AbsoluteQuantity)
            );
            fill.FillQuantity = Math.Sign(order.Quantity) * absoluteFillQuantity;

            if (absoluteRemaining == absoluteFillQuantity)
            {
                fill.Status = OrderStatus.Filled;
                _absoluteRemainingByOrderId.Remove(order.Id);
            }
            else
            {
                fill.Status = OrderStatus.PartiallyFilled;
                _absoluteRemainingByOrderId[order.Id] = absoluteRemaining - absoluteFillQuantity;

                _algorithm.Debug(
                    $"{_algorithm.Time} - Partial Fill - Remaining {absoluteRemaining} Price - {fill.FillPrice}"
                );
            }
            return fill;
        }
    }
}
