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

using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm that implements a fill model with partial fills
    /// </summary>
    /// <meta name="tag" content="transaction fees and slippage" />
    /// <meta name="tag" content="custom fill models" />
    public class CustomPartialFillModelAlgorithm : QCAlgorithm
    {
        private Symbol _spy;
        private SecurityHolding _holdings;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 3, 1);

            var equity = AddEquity("SPY", Resolution.Hour);
            _spy = equity.Symbol;
            _holdings = equity.Holdings;

            // Set the fill model
            equity.SetFillModel(new CustomPartialFillModel(this));
        }

        public override void OnData(Slice data)
        {
            var openOrders = Transactions.GetOpenOrders(_spy);
            if (openOrders.Count != 0) return;

            if (Time.Day > 10 && _holdings.Quantity <= 0)
            {
                MarketOrder(_spy, 100, true);
            }
            else if (Time.Day > 20 && _holdings.Quantity >= 0)
            {
                MarketOrder(_spy, -100, true);
            }
        }

        /// <summary>
        /// Implements a custom fill model that inherit from FillModel. Override the MarketFill method to simulate partially fill orders
        /// </summary>
        internal class CustomPartialFillModel : FillModel
        {
            private readonly QCAlgorithm _algorithm;
            private readonly Dictionary<int, decimal> _absoluteRemainingByOrderId;

            public CustomPartialFillModel(QCAlgorithm algorithm)
                : base()
            {
                _algorithm = algorithm;
                _absoluteRemainingByOrderId = new Dictionary<int, decimal>();
            }

            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                decimal absoluteRemaining;
                if (!_absoluteRemainingByOrderId.TryGetValue(order.Id, out absoluteRemaining))
                {
                    absoluteRemaining = order.AbsoluteQuantity;
                }

                // Create the object
                var fill = base.MarketFill(asset, order);

                // Set this fill amount
                fill.FillQuantity = Math.Sign(order.Quantity) * 10;

                if (absoluteRemaining == fill.FillQuantity)
                {
                    fill.Status = OrderStatus.Filled;
                    _absoluteRemainingByOrderId.Remove(order.Id);
                }
                else
                {
                    fill.Status = OrderStatus.PartiallyFilled;
                    _absoluteRemainingByOrderId[order.Id] = absoluteRemaining - fill.FillQuantity;
                    var price = fill.FillPrice;
                    _algorithm.Debug($"{_algorithm.Time} - Partial Fill - Remaining {absoluteRemaining} Price - {price}");
                }
                return fill;
            }
        }
    }
}