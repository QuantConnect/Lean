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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public abstract class Order
    {
        private decimal _quantity;
        private decimal _price;

        /// <summary>
        /// Order ID.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId { get; internal set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public List<string> BrokerId { get; internal set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public Symbol Symbol { get; internal set; }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price
        {
            get { return _price; }
            internal set { _price = value.Normalize(); }
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        public string PriceCurrency { get; internal set; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        public DateTime Time { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for <see cref="Time"/>
        /// </summary>
        public DateTime CreatedTime => Time;

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastFillTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastUpdateTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CanceledTime { get; internal set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public decimal Quantity
        {
            get { return _quantity; }
            internal set { _quantity = value.Normalize(); }
        }

        /// <summary>
        /// Order Type
        /// </summary>
        public abstract OrderType Type { get; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status { get; internal set; }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        public TimeInForce TimeInForce => Properties.TimeInForce;

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag { get; internal set; }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        public IOrderProperties Properties { get; private set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType => Symbol.ID.SecurityType;

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public OrderDirection Direction
        {
            get
            {
                if (Quantity > 0)
                {
                    return OrderDirection.Buy;
                }
                if (Quantity < 0)
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        public decimal AbsoluteQuantity => Math.Abs(Quantity);

        /// <summary>
        /// Gets the executed value of this order. If the order has not yet filled,
        /// then this will return zero.
        /// </summary>
        public decimal Value => Quantity * Price;

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        public OrderSubmissionData OrderSubmissionData { get; internal set; }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        public bool IsMarketable
        {
            get
            {
                if (Type == OrderType.Limit)
                {
                    // check if marketable limit order using bid/ask prices
                    var limitOrder = (LimitOrder)this;
                    return OrderSubmissionData != null &&
                           (Direction == OrderDirection.Buy && limitOrder.LimitPrice >= OrderSubmissionData.AskPrice ||
                            Direction == OrderDirection.Sell && limitOrder.LimitPrice <= OrderSubmissionData.BidPrice);
                }

                return Type == OrderType.Market;
            }
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        protected Order()
        {
            Time = new DateTime();
            Price = 0;
            PriceCurrency = string.Empty;
            Quantity = 0;
            Symbol = Symbol.Empty;
            Status = OrderStatus.None;
            Tag = "";
            BrokerId = new List<string>();
            ContingentId = 0;
            Properties = new OrderProperties();
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        protected Order(Symbol symbol, decimal quantity, DateTime time, string tag = "", IOrderProperties properties = null)
        {
            Time = time;
            Price = 0;
            PriceCurrency = string.Empty;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            BrokerId = new List<string>();
            ContingentId = 0;
            Properties = properties ?? new OrderProperties();
        }

        /// <summary>
        /// Gets the value of this order at the given market price in units of the account currency
        /// NOTE: Some order types derive value from other parameters, such as limit prices
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        /// <returns>The value of this order given the current market price</returns>
        public decimal GetValue(Security security)
        {
            var value = GetValueImpl(security);
            return value*security.QuoteCurrency.ConversionRate*security.SymbolProperties.ContractMultiplier;
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency for a single unit.
        /// A single unit here is a single share of stock, or a single barrel of oil, or the
        /// cost of a single share in an option contract.
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected abstract decimal GetValueImpl(Security security);

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public virtual void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != Id)
            {
                throw new ArgumentException("Attempted to apply updates to the incorrect order!");
            }
            if (request.Quantity.HasValue)
            {
                Quantity = request.Quantity.Value;
            }
            if (request.Tag != null)
            {
                Tag = request.Tag;
            }
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
            return Invariant($"OrderId: {Id} {Status} {Type} order for {Quantity} unit{(Quantity == 1 ? "" : "s")} of {Symbol}");
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public abstract Order Clone();

        /// <summary>
        /// Copies base Order properties to the specified order
        /// </summary>
        /// <param name="order">The target of the copy</param>
        protected void CopyTo(Order order)
        {
            order.Id = Id;
            order.Time = Time;
            order.LastFillTime = LastFillTime;
            order.LastUpdateTime = LastUpdateTime;
            order.CanceledTime = CanceledTime;
            order.BrokerId = BrokerId.ToList();
            order.ContingentId = ContingentId;
            order.Price = Price;
            order.PriceCurrency = PriceCurrency;
            order.Quantity = Quantity;
            order.Status = Status;
            order.Symbol = Symbol;
            order.Tag = Tag;
            order.Properties = Properties.Clone();
            order.OrderSubmissionData = OrderSubmissionData?.Clone();
        }

        /// <summary>
        /// Creates an <see cref="Order"/> to match the specified <paramref name="request"/>
        /// </summary>
        /// <param name="request">The <see cref="SubmitOrderRequest"/> to create an order for</param>
        /// <returns>The <see cref="Order"/> that matches the request</returns>
        public static Order CreateOrder(SubmitOrderRequest request)
        {
            Order order;
            switch (request.OrderType)
            {
                case OrderType.Market:
                    order = new MarketOrder(request.Symbol, request.Quantity, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.Limit:
                    order = new LimitOrder(request.Symbol, request.Quantity, request.LimitPrice, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder(request.Symbol, request.Quantity, request.StopPrice, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder(request.Symbol, request.Quantity, request.StopPrice, request.LimitPrice, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(request.Symbol, request.Quantity, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(request.Symbol, request.Quantity, request.Time, request.Tag, request.OrderProperties);
                    break;

                case OrderType.OptionExercise:
                    order = new OptionExerciseOrder(request.Symbol, request.Quantity, request.Time, request.Tag, request.OrderProperties);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            order.Status = OrderStatus.New;
            order.Id = request.OrderId;
            if (request.Tag != null)
            {
                order.Tag = request.Tag;
            }
            return order;
        }
    }
}
