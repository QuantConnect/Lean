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
using System.ComponentModel;
using Newtonsoft.Json;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Serialization;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order Event - Messaging class signifying a change in an order state and record the change in the user's algorithm portfolio
    /// </summary>
    public class OrderEvent
    {
        private decimal _fillPrice;
        private decimal _fillQuantity;
        private decimal _quantity;
        private decimal? _limitPrice;
        private decimal? _stopPrice;

        /// <summary>
        /// Id of the order this event comes from.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// The unique order event id for each order
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Easy access to the order symbol associated with this event.
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The date and time of this event (UTC).
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// Status message of the order.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The fee associated with the order
        /// </summary>
        public OrderFee OrderFee { get; set; }

        /// <summary>
        /// Fill price information about the order
        /// </summary>
        public decimal FillPrice
        {
            get { return _fillPrice; }
            set { _fillPrice = value.Normalize(); }
        }

        /// <summary>
        /// Currency for the fill price
        /// </summary>
        public string FillPriceCurrency { get; set; }

        /// <summary>
        /// Number of shares of the order that was filled in this event.
        /// </summary>
        public decimal FillQuantity
        {
            get { return _fillQuantity; }
            set { _fillQuantity = value.Normalize(); }
        }

        /// <summary>
        /// Public Property Absolute Getter of Quantity -Filled
        /// </summary>
        [JsonIgnore]
        public decimal AbsoluteFillQuantity => Math.Abs(FillQuantity);

        /// <summary>
        /// Order direction.
        /// </summary>
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Any message from the exchange.
        /// </summary>
        [DefaultValue(""), JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message { get; set; }

        /// <summary>
        /// True if the order event is an assignment
        /// </summary>
        public bool IsAssignment { get; set; }

        /// <summary>
        /// The current stop price
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? StopPrice
        {
            get { return _stopPrice; }
            set
            {
                if (value.HasValue)
                {
                    _stopPrice = value.Value.Normalize();
                }
            }
        }

        /// <summary>
        /// The current limit price
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal? LimitPrice
        {
            get { return _limitPrice; }
            set
            {
                if (value.HasValue)
                {
                    _limitPrice = value.Value.Normalize();
                }
            }
        }

        /// <summary>
        /// The current order quantity
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value.Normalize(); }
        }

        /// <summary>
        /// Order Event empty constructor required for json converter
        /// </summary>
        public OrderEvent()
        {
        }

        /// <summary>
        /// Order Event Constructor.
        /// </summary>
        /// <param name="orderId">Id of the parent order</param>
        /// <param name="symbol">Asset Symbol</param>
        /// <param name="utcTime">Date/time of this event</param>
        /// <param name="status">Status of the order</param>
        /// <param name="direction">The direction of the order this event belongs to</param>
        /// <param name="fillPrice">Fill price information if applicable.</param>
        /// <param name="fillQuantity">Fill quantity</param>
        /// <param name="orderFee">The order fee</param>
        /// <param name="message">Message from the exchange</param>
        public OrderEvent(int orderId,
            Symbol symbol,
            DateTime utcTime,
            OrderStatus status,
            OrderDirection direction,
            decimal fillPrice,
            decimal fillQuantity,
            OrderFee orderFee,
            string message = ""
            )
        {
            OrderId = orderId;
            Symbol = symbol;
            UtcTime = utcTime;
            Status = status;
            Direction = direction;
            FillPrice = fillPrice;
            FillPriceCurrency = string.Empty;
            FillQuantity = fillQuantity;
            OrderFee = orderFee;
            Message = message;
            IsAssignment = false;
        }

        /// <summary>
        /// Helper Constructor using Order to Initialize.
        /// </summary>
        /// <param name="order">Order for this order status</param>
        /// <param name="utcTime">Date/time of this event</param>
        /// <param name="orderFee">The order fee</param>
        /// <param name="message">Message from exchange or QC.</param>
        public OrderEvent(Order order, DateTime utcTime, OrderFee orderFee, string message = "")
        {
            OrderId = order.Id;
            Symbol = order.Symbol;
            Status = order.Status;
            Direction = order.Direction;

            //Initialize to zero, manually set fill quantity
            FillQuantity = 0;
            FillPrice = 0;
            FillPriceCurrency = order.PriceCurrency;

            UtcTime = utcTime;
            OrderFee = orderFee;
            Message = message;
            IsAssignment = false;
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
            var message = Invariant($"Time: {UtcTime} OrderID: {OrderId} EventID: {Id} Symbol: {Symbol.Value} Status: {Status} Quantity: {Quantity}");
            if (FillQuantity != 0)
            {
                message += Invariant($" FillQuantity: {FillQuantity} FillPrice: {FillPrice.SmartRounding()} {FillPriceCurrency}");
            }

            if (LimitPrice.HasValue)
            {
                message += Invariant($" LimitPrice: {LimitPrice.Value.SmartRounding()}");
            }
            if (StopPrice.HasValue)
            {
                message += Invariant($" StopPrice: {StopPrice.Value.SmartRounding()}");
            }

            // attach the order fee so it ends up in logs properly.
            if (OrderFee.Value.Amount != 0m) message += Invariant($" OrderFee: {OrderFee}");

            // add message from brokerage
            if (!string.IsNullOrEmpty(Message))
            {
                message += Invariant($" Message: {Message}");
            }

            return message;
        }

        /// <summary>
        /// Returns a clone of the current object.
        /// </summary>
        /// <returns>The new clone object</returns>
        public OrderEvent Clone()
        {
            return (OrderEvent) MemberwiseClone();
        }

        /// <summary>
        /// Creates a new instance based on the provided serialized order event
        /// </summary>
        public static OrderEvent FromSerialized(SerializedOrderEvent serializedOrderEvent)
        {
            var sid = SecurityIdentifier.Parse(serializedOrderEvent.Symbol);
            var symbol = new Symbol(sid, sid.Symbol);

            var orderFee = OrderFee.Zero;
            if (serializedOrderEvent.OrderFeeAmount.HasValue)
            {
                orderFee = new OrderFee(new CashAmount(serializedOrderEvent.OrderFeeAmount.Value,
                    serializedOrderEvent.OrderFeeCurrency));
            }

            var orderEvent = new OrderEvent(serializedOrderEvent.OrderId,
                symbol,
                DateTime.SpecifyKind(Time.UnixTimeStampToDateTime(serializedOrderEvent.Time), DateTimeKind.Utc),
                serializedOrderEvent.Status,
                serializedOrderEvent.Direction,
                serializedOrderEvent.FillPrice,
                serializedOrderEvent.FillQuantity,
                orderFee,
                serializedOrderEvent.Message)
            {
                IsAssignment = serializedOrderEvent.IsAssignment,
                LimitPrice = serializedOrderEvent.LimitPrice,
                StopPrice = serializedOrderEvent.StopPrice,
                FillPriceCurrency = serializedOrderEvent.FillPriceCurrency,
                Id = serializedOrderEvent.OrderEventId,
                Quantity = serializedOrderEvent.Quantity
            };

            return orderEvent;
        }
    }
}
