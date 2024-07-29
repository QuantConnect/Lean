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
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public abstract class Order
    {
        private volatile int _incrementalId;
        private decimal _quantity;
        private decimal _price;

        /// <summary>
        /// Order ID.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; internal set; }

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        [JsonProperty(PropertyName = "contingentId")]
        public int ContingentId { get; internal set; }

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        [JsonProperty(PropertyName = "brokerId")]
        public List<string> BrokerId { get; internal set; }

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public Symbol Symbol { get; internal set; }

        /// <summary>
        /// Price of the Order.
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal Price
        {
            get { return _price; }
            internal set { _price = value.Normalize(); }
        }

        /// <summary>
        /// Currency for the order price
        /// </summary>
        [JsonProperty(PropertyName = "priceCurrency")]
        public string PriceCurrency { get; internal set; }

        /// <summary>
        /// Gets the utc time the order was created.
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public DateTime Time { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was created. Alias for <see cref="Time"/>
        /// </summary>
        [JsonProperty(PropertyName = "createdTime")]
        public DateTime CreatedTime => Time;

        /// <summary>
        /// Gets the utc time the last fill was received, or null if no fills have been received
        /// </summary>
        [JsonProperty(PropertyName = "lastFillTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastFillTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was last updated, or null if the order has not been updated.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdateTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastUpdateTime { get; internal set; }

        /// <summary>
        /// Gets the utc time this order was canceled, or null if the order was not canceled.
        /// </summary>
        [JsonProperty(PropertyName = "canceledTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CanceledTime { get; internal set; }

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public virtual decimal Quantity
        {
            get { return _quantity; }
            internal set { _quantity = value.Normalize(); }
        }

        /// <summary>
        /// Order Type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public abstract OrderType Type { get; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Order Time In Force
        /// </summary>
        [JsonIgnore]
        public TimeInForce TimeInForce => Properties.TimeInForce;

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        [JsonProperty(PropertyName = "tag" ,DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tag { get; internal set; }

        /// <summary>
        /// Additional properties of the order
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IOrderProperties Properties { get; private set; }

        /// <summary>
        /// The symbol's security type
        /// </summary>
        [JsonProperty(PropertyName = "securityType")]
        public SecurityType SecurityType => Symbol.ID.SecurityType;

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
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
        [JsonIgnore]
        public decimal AbsoluteQuantity => Math.Abs(Quantity);

        /// <summary>
        /// Deprecated
        /// </summary>
        [JsonProperty(PropertyName = "value"), Obsolete("Please use Order.GetValue(security) or security.Holdings.HoldingsValue")]
        public decimal Value => Quantity * Price;

        /// <summary>
        /// Gets the price data at the time the order was submitted
        /// </summary>
        [JsonProperty(PropertyName = "orderSubmissionData")]
        public OrderSubmissionData OrderSubmissionData { get; internal set; }

        /// <summary>
        /// Returns true if the order is a marketable order.
        /// </summary>
        [JsonProperty(PropertyName = "isMarketable")]
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

                return Type == OrderType.Market || Type == OrderType.ComboMarket;
            }
        }

        /// <summary>
        /// Manager for the orders in the group if this is a combo order
        /// </summary>
        [JsonProperty(PropertyName = "groupOrderManager", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public GroupOrderManager GroupOrderManager { get; set; }

        /// <summary>
        /// The adjustment mode used on the order fill price
        /// </summary>
        [JsonProperty(PropertyName = "priceAdjustmentMode")]
        public DataNormalizationMode PriceAdjustmentMode { get; set; }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        protected Order()
        {
            Time = new DateTime();
            PriceCurrency = string.Empty;
            Symbol = Symbol.Empty;
            Status = OrderStatus.None;
            Tag = string.Empty;
            BrokerId = new List<string>();
            Properties = new OrderProperties();
            GroupOrderManager = null;
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="groupOrderManager">Manager for the orders in the group if this is a combo order</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        protected Order(Symbol symbol, decimal quantity, DateTime time, GroupOrderManager groupOrderManager, string tag = "",
            IOrderProperties properties = null)
        {
            Time = time;
            PriceCurrency = string.Empty;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            BrokerId = new List<string>();
            Properties = properties ?? new OrderProperties();
            GroupOrderManager = groupOrderManager;
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
            : this(symbol, quantity, time, null, tag, properties)
        {
        }

        /// <summary>
        /// Creates an enumerable containing each position resulting from executing this order.
        /// </summary>
        /// <remarks>
        /// This is provided in anticipation of a new combo order type that will need to override this method,
        /// returning a position for each 'leg' of the order.
        /// </remarks>
        /// <returns>An enumerable of positions matching the results of executing this order</returns>
        public virtual IEnumerable<IPosition> CreatePositions(SecurityManager securities)
        {
            var security = securities[Symbol];
            yield return new Position(security, Quantity);
        }

        /// <summary>
        /// Gets the value of this order at the given market price in units of the account currency
        /// NOTE: Some order types derive value from other parameters, such as limit prices
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        /// <returns>The value of this order given the current market price</returns>
        /// <remarks>TODO: we should remove this. Only used in tests</remarks>
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
        /// Gets the default tag for this order
        /// </summary>
        /// <returns>The default tag</returns>
        public virtual string GetDefaultTag()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets a new unique incremental id for this order
        /// </summary>
        /// <returns>Returns a new id for this order</returns>
        internal int GetNewId()
        {
            return Interlocked.Increment(ref _incrementalId);
        }

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
            return Messages.Order.ToString(this);
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
            // The group order manager has to be set before the quantity,
            // since combo orders might need it to calculate the quantity in the Quantity setter.
            order.GroupOrderManager = GroupOrderManager;
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
            order.PriceAdjustmentMode = PriceAdjustmentMode;
        }

        /// <summary>
        /// Creates an <see cref="Order"/> to match the specified <paramref name="request"/>
        /// </summary>
        /// <param name="request">The <see cref="SubmitOrderRequest"/> to create an order for</param>
        /// <returns>The <see cref="Order"/> that matches the request</returns>
        public static Order CreateOrder(SubmitOrderRequest request)
        {
            return CreateOrder(request.OrderId, request.OrderType, request.Symbol, request.Quantity, request.Time,
                 request.Tag, request.OrderProperties, request.LimitPrice, request.StopPrice, request.TriggerPrice, request.TrailingAmount,
                 request.TrailingAsPercentage, request.LimitOffset, request.GroupOrderManager);
        }

        private static Order CreateOrder(int orderId, OrderType type, Symbol symbol, decimal quantity, DateTime time,
            string tag, IOrderProperties properties, decimal limitPrice, decimal stopPrice, decimal triggerPrice, decimal trailingAmount,
            bool trailingAsPercentage, decimal limitOffset, GroupOrderManager groupOrderManager)
        {
            Order order;
            switch (type)
            {
                case OrderType.Market:
                    order = new MarketOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.Limit:
                    order = new LimitOrder(symbol, quantity, limitPrice, time, tag, properties);
                    break;

                case OrderType.StopMarket:
                    order = new StopMarketOrder(symbol, quantity, stopPrice, time, tag, properties);
                    break;

                case OrderType.StopLimit:
                    order = new StopLimitOrder(symbol, quantity, stopPrice, limitPrice, time, tag, properties);
                    break;

                case OrderType.TrailingStop:
                    order = new TrailingStopOrder(symbol, quantity, stopPrice, trailingAmount, trailingAsPercentage, time, tag, properties);
                    break;

                case OrderType.TrailingStopLimit:
                    order = new TrailingStopLimitOrder(symbol, quantity, stopPrice, limitPrice, trailingAmount, trailingAsPercentage,
                        limitOffset, time, tag, properties);
                    break;

                case OrderType.LimitIfTouched:
                    order = new LimitIfTouchedOrder(symbol, quantity, triggerPrice, limitPrice, time, tag, properties);
                    break;

                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.OptionExercise:
                    order = new OptionExerciseOrder(symbol, quantity, time, tag, properties);
                    break;

                case OrderType.ComboLimit:
                    order = new ComboLimitOrder(symbol, quantity, limitPrice, time, groupOrderManager, tag, properties);
                    break;

                case OrderType.ComboLegLimit:
                    order = new ComboLegLimitOrder(symbol, quantity, limitPrice, time, groupOrderManager, tag, properties);
                    break;

                case OrderType.ComboMarket:
                    order = new ComboMarketOrder(symbol, quantity, time, groupOrderManager, tag, properties);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            order.Status = OrderStatus.New;
            order.Id = orderId;
            if (groupOrderManager != null)
            {
                lock (groupOrderManager.OrderIds)
                {
                    groupOrderManager.OrderIds.Add(orderId);
                }
            }
            return order;
        }
    }
}
