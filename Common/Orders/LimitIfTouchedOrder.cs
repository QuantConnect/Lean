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
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// In effect, a LimitIfTouchedOrder behaves opposite to the <see cref="StopLimitOrder"/>;
    /// after a trigger price is touched, a limit order is set for some user-defined value above (below)
    /// the trigger when selling (buying).
    /// https://www.interactivebrokers.ca/en/index.php?f=45318
    /// </summary>
    public class LimitIfTouchedOrder : Order
    {
        /// <summary>
        /// Order Type
        /// </summary>
        public override OrderType Type => OrderType.LimitIfTouched;

        /// <summary>
        /// The price which, when touched, will trigger the setting of a limit order at <see cref="LimitPrice"/>.
        /// </summary>
        [JsonProperty(PropertyName = "triggerPrice")]
        public decimal TriggerPrice { get; internal set; }

        /// <summary>
        /// The price at which to set the limit order following <see cref="TriggerPrice"/> being touched.
        /// </summary>
        [JsonProperty(PropertyName = "limitPrice")]
        public decimal LimitPrice { get; internal set; }

        /// <summary>
        /// Whether or not the <see cref="TriggerPrice"/> has been touched.
        /// </summary>
        [JsonProperty(PropertyName = "triggerTouched")]
        public bool TriggerTouched { get; internal set; }

        /// <summary>
        /// New <see cref="LimitIfTouchedOrder"/> constructor.
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="limitPrice">Maximum price to fill the order</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="triggerPrice">Price which must be touched in order to then set a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        public LimitIfTouchedOrder(
            Symbol symbol,
            decimal quantity,
            decimal? triggerPrice,
            decimal limitPrice,
            DateTime time,
            string tag = "",
            IOrderProperties properties = null
            )
            : base(symbol, quantity, time, tag, properties)
        {
            TriggerPrice = (decimal) triggerPrice;
            LimitPrice = limitPrice;
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public LimitIfTouchedOrder()
        {
        }

        /// <summary>
        /// Gets the default tag for this order
        /// </summary>
        /// <returns>The default tag</returns>
        public override string GetDefaultTag()
        {
            return Messages.LimitIfTouchedOrder.Tag(this);
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            base.ApplyUpdateOrderRequest(request);
            if (request.TriggerPrice.HasValue)
            {
                TriggerPrice = request.TriggerPrice.Value;
            }

            if (request.LimitPrice.HasValue)
            {
                LimitPrice = request.LimitPrice.Value;
            }
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new LimitIfTouchedOrder
                {TriggerPrice = TriggerPrice, LimitPrice = LimitPrice, TriggerTouched = TriggerTouched};
            CopyTo(order);
            return order;
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
            return Messages.LimitIfTouchedOrder.ToString(this);
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency for a single unit.
        /// A single unit here is a single share of stock, or a single barrel of oil, or the
        /// cost of a single share in an option contract.
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
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
    }
}
