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
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    public class CoverOrder : Order
    {
        private readonly IOrderProperties Properties;

        public CoverOrder()
        {
        }

        public CoverOrder(Symbol symbol, decimal quantity, decimal entryPrice, decimal stopPrice, decimal limitPrice, decimal trailingStopLoss, DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, time, tag, properties)
        {
            Symbol = symbol;
            Quantity = quantity;
            EntryPrice = entryPrice;
            StopPrice = stopPrice;
            LimitPrice = limitPrice;
            TrailingStopLoss = trailingStopLoss;
            Time = time;
            Tag = tag;
            Properties = properties;
            if (tag == "")
            {
                //Default tag values to display stop price in GUI.
                Tag = Invariant($"Stop Price: {stopPrice:C} Limit Price: {limitPrice:C}");
            }
        }

        /// <summary>
        /// Stop price for this stop loss order.
        /// </summary>
        public decimal StopPrice { get; internal set; }

        /// <summary>
        /// Signal showing the "StopLimitOrder" has been converted into a Limit Order
        /// </summary>
        public bool StopTriggered { get; internal set; }

        /// <summary>
        /// Limit price for the stop limit order
        /// </summary>
        public decimal LimitPrice { get; internal set; }
        public decimal TrailingStopLoss { get; internal set; }

        /// <summary>
        /// Limit price for the entry limit order
        /// </summary>
        public decimal EntryPrice { get; internal set; }


        public override OrderType Type
        {
            get { return OrderType.Cover; }
        }

        public override Order Clone()
        {
            var order = new CoverOrder { EntryPrice = EntryPrice, StopPrice = StopPrice, LimitPrice = LimitPrice };
            CopyTo(order);
            return order;
        }

        protected override decimal GetValueImpl(Security security)
        {
            // selling, so higher price will be used
            if (Quantity < 0)
            {
                return Quantity * Math.Max(LimitPrice, security.Price);
            }

            // buying, so lower price will be used
            if (Quantity > 0)
            {
                return Quantity * Math.Min(LimitPrice, security.Price);
            }

            return 0m;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Invariant($"{base.ToString()} at entry limit {EntryPrice.SmartRounding()} stop {StopPrice.SmartRounding()} limit {LimitPrice.SmartRounding()}");
        }
        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            base.ApplyUpdateOrderRequest(request);
            if (request.EntryPrice.HasValue)
            {
                EntryPrice = request.EntryPrice.Value;
            }
            if (request.StopPrice.HasValue)
            {
                StopPrice = request.StopPrice.Value;
            }
            if (request.LimitPrice.HasValue)
            {
                LimitPrice = request.LimitPrice.Value;
            }
            if (request.TrailingStopLoss.HasValue)
            {
                TrailingStopLoss = request.TrailingStopLoss.Value;
            }
        }
    }
}
