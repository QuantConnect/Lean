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

using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect.Orders.Fees;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Brokerages.Zerodha.Messages;
using Tick = QuantConnect.Data.Market.Tick;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Util;
using Order = QuantConnect.Orders.Order;
using OrderType = QuantConnect.Orders.OrderType;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Zerodha Brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(ZerodhaBrokerageFactory))]
    public partial class ZerodhaBrokerage : Brokerage, IDataQueueHandler
    {
        #region Declarations
        private const int ConnectionTimeout = 30000;

        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected ZerodhaWebSocketClientWrapper WebSocket;

        /// <summary>
        /// standard json parsing settings
        /// </summary>
        protected JsonSerializerSettings JsonSettings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };

        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();

        /// <summary>
        /// A list of currently subscribed channels
        /// </summary>
        protected Dictionary<string, Channel> ChannelList = new Dictionary<string, Channel>();

        /// <summary>
        /// The api secret
        /// </summary>
        protected string ApiSecret;

        /// <summary>
        /// The api key
        /// </summary>
        protected string ApiKey;

        private readonly ISecurityProvider _securityProvider;

        /// <summary>
        /// Timestamp of most recent heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime = DateTime.UtcNow;

        private readonly IAlgorithm _algorithm;
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();

        private readonly DataQueueHandlerSubscriptionManager SubscriptionManager;

        private ConcurrentDictionary<string, Symbol> _subscriptionsById = new ConcurrentDictionary<string, Symbol>();

        private readonly ConcurrentQueue<MessageData> _messageBuffer = new ConcurrentQueue<MessageData>();

        private readonly IDataAggregator _aggregator;

        private readonly ZerodhaSymbolMapper _symbolMapper;


        private readonly List<string> subscribeInstrumentTokens = new List<string>();
        private readonly List<string> unSubscribeInstrumentTokens = new List<string>();

        private Kite _kite;
        private readonly string _apiKey;
        private readonly string _accessToken;
        private readonly string _wssUrl = "wss://ws.kite.trade/";

        private readonly string _tradingSegment;
        private readonly string _zerodhaProductType;

        private DateTime _lastTradeTickTime;
        #endregion



        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="aggregator">data aggregator </param>
        /// <param name="tradingSegment">trading segment</param>
        /// <param name="zerodhaProductType">zerodha product type - MIS, CNC or NRML </param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="securityProvider">Security provider for fetching holdings</param>
        public ZerodhaBrokerage(string tradingSegment, string zerodhaProductType, string apiKey, string apiSecret, IAlgorithm algorithm, ISecurityProvider securityProvider, IDataAggregator aggregator)
            : base("Zerodha")
        {
            _tradingSegment = tradingSegment;
            _zerodhaProductType = zerodhaProductType;
            _algorithm = algorithm;
            _aggregator = aggregator;
            _kite = new Kite(apiKey, apiSecret);
            _apiKey = apiKey;
            _accessToken = apiSecret;
            _securityProvider = securityProvider;
            WebSocket = new ZerodhaWebSocketClientWrapper();
            _wssUrl += string.Format(CultureInfo.InvariantCulture, "?api_key={0}&access_token={1}", _apiKey, _accessToken);
            WebSocket.Initialize(_wssUrl);
            WebSocket.Message += OnMessage;
            WebSocket.Open += (sender, args) =>
            {
                Log.Trace($"ZerodhaBrokerage(): WebSocket.Open. Subscribing");
                Subscribe(GetSubscribed());
            };
            WebSocket.Error += OnError;
            _symbolMapper = new ZerodhaSymbolMapper(_kite);

            var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            subscriptionManager.SubscribeImpl += (s, t) =>
            {
                Subscribe(s);
                return true;
            };
            subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);
            SubscriptionManager = subscriptionManager;

            Log.Trace("Start Zerodha Brokerage");
        }

        /// <summary>
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
        {
            if (symbols.Count() <= 0)
            {
                return;
            }
            foreach (var symbol in symbols)
            {
                var market = GetSymbolMarket(symbol);
                var instrumentToken = _symbolMapper.GetZerodhaInstrumentToken(symbol.ID.Symbol, market);
                if (instrumentToken == 0)
                {
                    Log.Error("ZerodhaBrokerage.Subscribe(): Invalid Zerodha Instrument token");
                    continue;
                }
                if (!subscribeInstrumentTokens.Contains(instrumentToken.ToStringInvariant()))
                {
                    subscribeInstrumentTokens.Add(instrumentToken.ToStringInvariant());
                    unSubscribeInstrumentTokens.Remove(instrumentToken.ToStringInvariant());
                    _subscriptionsById[instrumentToken.ToStringInvariant()] = symbol;
                }
            }
            //Websocket Data subscription modes. Full mode gives depth of asks and bids along with basic data.
            var request = "{\"a\":\"subscribe\",\"v\":[" + String.Join(",", subscribeInstrumentTokens.ToArray()) + "]}";
            var requestFullMode = "{\"a\":\"mode\",\"v\":[\"full\",[" + String.Join(",", subscribeInstrumentTokens.ToArray()) + "]]}";
            WebSocket.Send(request);
            WebSocket.Send(requestFullMode);
        }

        /// <summary>
        /// Get symbol exchange
        /// </summary>
        /// <param name="symbol">symbols to get market</param>
        /// <returns>string</returns>
        private string GetSymbolMarket(Symbol symbol)
        {
            if(symbol.SecurityType == SecurityType.Equity && symbol.ID.Market.ToLowerInvariant() == "nfo")
            {
                return "nse";
            }
            else if (symbol.SecurityType == SecurityType.Option && symbol.ID.Market.ToLowerInvariant() == "nfo" && symbol.HasUnderlying)
            {
                if(symbol.Underlying.SecurityType == SecurityType.Equity && symbol.Underlying.ID.Market == "nfo")
                {
                    return "nse";
                }
            }
            return symbol.ID.Market;
        }

        /// <summary>
        /// Get list of subscribed symbol
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Symbol> GetSubscribed()
        {
            return SubscriptionManager.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                foreach (var symbol in symbols)
                {
                    var instrumentToken = _symbolMapper.GetZerodhaInstrumentToken(symbol.ID.Symbol, symbol.ID.Market);
                    if (instrumentToken == 0)
                    {
                        Log.Error("ZerodhaBrokerage.Unsubscribe(): Invalid Zerodha Instrument token");
                        continue;
                    }
                    if (!unSubscribeInstrumentTokens.Contains(instrumentToken.ToStringInvariant()))
                    {
                        unSubscribeInstrumentTokens.Add(instrumentToken.ToStringInvariant());
                        subscribeInstrumentTokens.Remove(instrumentToken.ToStringInvariant());
                        Symbol unSubscribeSymbol;
                        _subscriptionsById.TryRemove(instrumentToken.ToStringInvariant(), out unSubscribeSymbol);
                    }
                }
                var request = "{\"a\":\"unsubscribe\",\"v\":[" + String.Join(",", unSubscribeInstrumentTokens.ToArray()) + "]}";
                WebSocket.Send(request);
                return true;
            }
            return false;

        }


        /// <summary>
        /// Gets Quote using Zerodha API
        /// </summary>
        /// <returns> Quote</returns>
        public Quote GetQuote(Symbol symbol)
        {
            var instrument = _symbolMapper.GetZerodhaInstrumentToken(symbol.ID.Symbol, symbol.ID.Market);
            var instrumentIds = new string[] { instrument.ToStringInvariant() };
            var quotes = _kite.GetQuote(instrumentIds);
            return quotes[instrument.ToStringInvariant()];
        }

        /// <summary>
        /// Zerodha brokerage order events
        /// </summary>
        /// <param name="orderUpdate"></param>
        private void OnOrderUpdate(Messages.Order orderUpdate)
        {
            try
            {
                var brokerId = orderUpdate.OrderId;
                Log.Trace("OnZerodhaOrderEvent(): Broker ID:" + brokerId);
                var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
                if (order == null)
                {
                    order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
                    if (order == null)
                    {
                        // not our order, nothing else to do here
                        return;
                    }
                }

                if (orderUpdate.Status == "CANCELLED")
                {
                    Order outOrder;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                    decimal ignored;
                    _fills.TryRemove(order.Id, out ignored);
                }

                if (orderUpdate.Status == "REJECTED")
                {
                    Order outOrder;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                    decimal ignored;
                    _fills.TryRemove(order.Id, out ignored);
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Zerodha Order Rejected Event: " + orderUpdate.StatusMessage) { Status = OrderStatus.Canceled });
                }

                if (orderUpdate.FilledQuantity > 0)
                {
                    var symbol = _symbolMapper.ConvertZerodhaSymbolToLeanSymbol(orderUpdate.InstrumentToken);
                    var fillPrice = orderUpdate.AveragePrice;
                    var fillQuantity = orderUpdate.FilledQuantity;
                    var direction = orderUpdate.TransactionType == "SELL" ? OrderDirection.Sell : OrderDirection.Buy;
                    var updTime = orderUpdate.OrderTimestamp.GetValueOrDefault();

                    var security = _securityProvider.GetSecurity(order.Symbol);
                    var orderFee = security.FeeModel.GetOrderFee(
                        new OrderFeeParameters(security, order));

                    
                    if (direction == OrderDirection.Sell)
                    {
                        fillQuantity = -1 * fillQuantity;
                    }
                    var status = OrderStatus.Filled;
                    if (fillQuantity != order.Quantity)
                    {
                        decimal totalFillQuantity;
                        _fills.TryGetValue(order.Id, out totalFillQuantity);
                        totalFillQuantity += fillQuantity;
                        _fills[order.Id] = totalFillQuantity;
                        status = totalFillQuantity == order.Quantity
                            ? OrderStatus.Filled
                            : OrderStatus.PartiallyFilled;
                    }

                    var orderEvent = new OrderEvent
                    (
                        order.Id, symbol, updTime, status,
                        direction, fillPrice, fillQuantity,
                        orderFee, $"Zerodha Order Event {direction}"
                    );

                    // if the order is closed, we no longer need it in the active order list
                    if (status == OrderStatus.Filled)
                    {
                        Order outOrder;
                        CachedOrderIDs.TryRemove(order.Id, out outOrder);
                        decimal ignored;
                        _fills.TryRemove(order.Id, out ignored);
                    }

                    OnOrderEvent(orderEvent);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }
        

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        public void LockStream()
        {
            Log.Trace("ZerodhaBrokerage.Messaging.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        public void UnlockStream()
        {
            Log.Trace("ZerodhaBrokerage.Messaging.UnlockStream(): Processing Backlog...");
            while (_messageBuffer.Any())
            {
                MessageData e;
                _messageBuffer.TryDequeue(out e);
                OnMessageImpl(e);
            }
            Log.Trace("ZerodhaBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        #region IBrokerage

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public override string AccountBaseCurrency => Currencies.INR;

        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Connects to Zerodha wss
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
            {
                return;
            }

            Log.Trace("ZerodhaBrokerage.Connect(): Connecting...");

            var resetEvent = new ManualResetEvent(false);
            EventHandler triggerEvent = (o, args) => resetEvent.Set();
            WebSocket.Open += triggerEvent;
            WebSocket.Connect();
            if (!resetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException("Websockets connection timeout.");
            }
            WebSocket.Open -= triggerEvent;
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            //base.Disconnect();
            if (WebSocket.IsOpen)
            {
                WebSocket.Close();
            }
        }


        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            LockStream();

            uint orderQuantity = Convert.ToUInt32(Math.Abs(order.Quantity));
            JObject orderResponse;

            decimal? triggerPrice = GetOrderTriggerPrice(order);
            decimal? orderPrice = GetOrderPrice(order);

            var kiteOrderType = ConvertOrderType(order.Type);
            var security = _securityProvider.GetSecurity(order.Symbol);
            var orderFee = security.FeeModel.GetOrderFee(
                        new OrderFeeParameters(security, order));
            var orderProperties = order.Properties as ZerodhaOrderProperties;
            if (orderProperties == null)
            {
                var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Invalid product type Please set either MIS,CNC or NRML";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                UnlockStream();
                return false;
            }


            try
            {
                orderResponse = _kite.PlaceOrder(order.Symbol.ID.Market.ToUpperInvariant(), order.Symbol.ID.Symbol, order.Direction.ToString().ToUpperInvariant(),
                    orderQuantity, orderPrice, orderProperties.ProductType, kiteOrderType, null, null, triggerPrice);
            }
            catch (Exception ex)
            {

                var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {ex.Message}";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                UnlockStream();
                return false;
            }


            if ((string)orderResponse["status"] == "success")
            {
                if (string.IsNullOrEmpty((string)orderResponse["data"]["order_id"]))
                {
                    var errorMessage = $"Error parsing response from place order: {(string)orderResponse["status_message"]}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (string)orderResponse["status_message"], errorMessage));

                    UnlockStream();
                    return false;
                }

                var brokerId = (string)orderResponse["data"]["order_id"];
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Clear();
                    CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                // Generate submitted event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Submitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse["status_message"]}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return false;

        }

        /// <summary>
        /// Return a relevant price for order depending on order type
        /// Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private static decimal? GetOrderPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.Limit:
                    return ((LimitOrder)order).LimitPrice;

                case OrderType.StopLimit:
                    return ((StopLimitOrder)order).LimitPrice;

                case OrderType.Market:
                case OrderType.StopMarket:
                    return null;
            }

            throw new NotSupportedException($"ZerodhaBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        /// <summary>
        /// Return a relevant price for order depending on order type
        /// Price must be positive
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private static decimal? GetOrderTriggerPrice(Order order)
        {
            switch (order.Type)
            {
                case OrderType.StopLimit:
                    return ((StopLimitOrder)order).StopPrice;

                case OrderType.StopMarket:
                    return ((StopMarketOrder)order).StopPrice;

                case OrderType.Limit:
                case OrderType.Market:
                    return null;
            }

            throw new NotSupportedException($"ZerodhaBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
        }

        private static string ConvertOrderType(OrderType orderType)
        {

            //            public const string ORDER_TYPE_MARKET = "MARKET";
            //public const string ORDER_TYPE_LIMIT = "LIMIT";
            //public const string ORDER_TYPE_SLM = "SL-M";
            //public const string ORDER_TYPE_SL = "SL";

            switch (orderType)
            {
                case OrderType.Limit:
                    return "LIMIT";
                case OrderType.Market:
                    return "MARKET";
                case OrderType.StopMarket:
                    return "SL-M";
                case OrderType.StopLimit:
                    return "SL";
                default:
                    throw new NotSupportedException($"ZerodhaBrokerage.ConvertOrderType: Unsupported order type: {orderType}");
            }
        }


        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            LockStream();

            if (!order.Status.IsOpen())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "error", "Order is already being processed"));
                UnlockStream();
                return false;
            }


            var orderProperties = order.Properties as ZerodhaOrderProperties;
            if (orderProperties == null)
            {
                var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Invalid product type Please set either MIS,CNC or NRML";
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                UnlockStream();
                return false;
            }


            uint orderQuantity = Convert.ToUInt32(Math.Abs(order.Quantity));
            JObject orderResponse;
            decimal? triggerPrice = GetOrderTriggerPrice(order);
            decimal? orderPrice = GetOrderPrice(order);
            var kiteOrderType = ConvertOrderType(order.Type);

            var orderFee = OrderFee.Zero;
            try
            {
                orderResponse = _kite.ModifyOrder(order.BrokerId[0].ToStringInvariant(),
                null,
                order.Symbol.ID.Market.ToUpperInvariant(),
                order.Symbol.ID.Symbol,
                order.Direction.ToString().ToUpperInvariant(),
                orderQuantity,
                orderPrice,
                orderProperties.ProductType,
                kiteOrderType,
                null,
                null,
                triggerPrice
                );
            }
            catch (Exception ex)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Update Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {ex.Message}"));

                UnlockStream();
                return false;
            }


            if ((string)orderResponse["status"] == "success")
            {
                if (string.IsNullOrEmpty((string)orderResponse["data"]["order_id"]))
                {
                    var errorMessage = $"Error parsing response from modify order: {orderResponse["status_message"]}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Update Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (string)orderResponse["status"], errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = (string)orderResponse["data"]["order_id"];
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    CachedOrderIDs[order.Id].BrokerId.Clear();
                    CachedOrderIDs[order.Id].BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                // Generate submitted event
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Update Order Event") { Status = OrderStatus.UpdateSubmitted });
                Log.Trace($"Order modified successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse["status_message"]}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Update Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return false;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            LockStream();

            JObject orderResponse = new JObject();
            if (order.Status.IsOpen())
            {
                try
                {
                    orderResponse = _kite.CancelOrder(order.BrokerId[0].ToStringInvariant());
                }
                catch (Exception)
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (string)orderResponse["status"], $"Error cancelling order: {orderResponse["status_message"]}"));
                    UnlockStream();
                    return false;
                }
            }
            else
            {
                //Verify this
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, 500, $"Error cancelling open order"));
                UnlockStream();
                return false;
            }

            if ((string)orderResponse["status"] == "success")
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Zerodha Order Cancelled Event") { Status = OrderStatus.Canceled });
                UnlockStream();
                return true;
            }
            var errorMessage = $"Error cancelling order: {orderResponse["status_message"]}";
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (string)orderResponse["status"], errorMessage));

            UnlockStream();
            return false;


        }


        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var allOrders = _kite.GetOrders();
            List<Order> list = new List<Order>();

            //Only loop if there are any actual orders inside response
            if (allOrders.Count > 0)
            {
                foreach (var item in allOrders.Where(z => z.Status == "OPEN" || z.Status == "TRIGGER PENDING"))
                {
                    Order order;
                    if (item.OrderType.ToUpperInvariant() == "MARKET")
                    {
                        order = new MarketOrder { Price = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else if (item.OrderType.ToUpperInvariant() == "LIMIT")
                    {
                        order = new LimitOrder { LimitPrice = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else if (item.OrderType.ToUpperInvariant() == "SL-M")
                    {
                        order = new StopMarketOrder { StopPrice = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else if (item.OrderType.ToUpperInvariant() == "SL")
                    {
                        order = new StopLimitOrder { StopPrice = Convert.ToDecimal(item.TriggerPrice, CultureInfo.InvariantCulture), LimitPrice = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "UnKnownOrderType",
                            "ZerodhaBrorage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.OrderType));
                        continue;
                    }

                    var itemTotalQty = item.Quantity;
                    var originalQty = item.Quantity;
                    order.Quantity = item.TransactionType.ToLowerInvariant() == "sell" ? -itemTotalQty : originalQty;
                    order.BrokerId = new List<string> { item.OrderId };
                    order.Symbol = _symbolMapper.ConvertZerodhaSymbolToLeanSymbol(item.InstrumentToken);
                    order.Time = (DateTime)item.OrderTimestamp;
                    order.Status = ConvertOrderStatus(item);
                    order.Price = item.Price;
                    list.Add(order);

                }
                foreach (var item in list)
                {
                    if (item.Status.IsOpen())
                    {
                        var cached = CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                        if (cached.Any())
                        {
                            CachedOrderIDs[cached.First().Key] = item;
                        }
                    }
                }
            }

            return list;
        }

        private OrderStatus ConvertOrderStatus(Messages.Order orderDetails)
        {
            var filledQty = Convert.ToInt32(orderDetails.FilledQuantity, CultureInfo.InvariantCulture);
            var pendingQty = Convert.ToInt32(orderDetails.PendingQuantity, CultureInfo.InvariantCulture);
            var orderDetail = _kite.GetOrderHistory(orderDetails.OrderId);
            if (orderDetails.Status.ToLowerInvariant() != "complete" && filledQty == 0)
            {
                return OrderStatus.Submitted;
            }
            else if (filledQty > 0 && pendingQty > 0)
            {
                return OrderStatus.PartiallyFilled;
            }
            else if (pendingQty == 0)
            {
                return OrderStatus.Filled;
            }
            else if (orderDetails.Status.ToUpperInvariant() == "CANCELLED")
            {
                return OrderStatus.Canceled;
            }

            return OrderStatus.None;
        }



        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdingsList = new List<Holding>();
            if (_zerodhaProductType.ToUpperInvariant() == KiteProductType.MIS.ToString().ToUpperInvariant() || _zerodhaProductType.ToUpperInvariant() == KiteProductType.NRML.ToString().ToUpperInvariant())
            {
                var HoldingResponse = _kite.GetPositions();
                if (HoldingResponse.Day.Count == 0)
                {
                    return holdingsList;
                }
                foreach (var item in HoldingResponse.Day)
                {

                    Holding holding = new Holding
                    {
                        AveragePrice = item.AveragePrice,
                        Symbol = _symbolMapper.GetLeanSymbol(item.TradingSymbol),
                        MarketPrice = item.LastPrice,
                        Quantity = item.Quantity,
                        UnrealizedPnL = item.Unrealised,
                        CurrencySymbol = Currencies.GetCurrencySymbol(AccountBaseCurrency),
                        MarketValue = item.ClosePrice * item.Quantity

                    };
                    holdingsList.Add(holding);
                }

            }
            else if (_zerodhaProductType.ToUpperInvariant() == KiteProductType.CNC.ToString().ToUpperInvariant())
            {
                var HoldingResponse = _kite.GetHoldings();
                if (HoldingResponse == null)
                {
                    return holdingsList;
                }

                foreach (var item in HoldingResponse)
                {
                    Holding holding = new Holding
                    {
                        AveragePrice = item.AveragePrice,
                        Symbol = _symbolMapper.GetLeanSymbol(item.TradingSymbol),
                        MarketPrice = item.LastPrice,
                        Quantity = item.Quantity,
                        UnrealizedPnL = item.PNL,
                        CurrencySymbol = Currencies.GetCurrencySymbol(AccountBaseCurrency),
                        MarketValue = item.ClosePrice * item.Quantity
                    };
                    holdingsList.Add(holding);
                }
            }

            return holdingsList;
        }
        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            decimal amt = 0m;
            var list = new List<CashAmount>();
            var response = _kite.GetMargins();
            if (_tradingSegment == "EQUITY")
            {
                amt = Convert.ToDecimal(response.Equity.Available.Cash, CultureInfo.InvariantCulture);
            }
            else
            {
                amt = Convert.ToDecimal(response.Commodity.Available.Cash, CultureInfo.InvariantCulture);
            }
            list.Add(new CashAmount(amt, AccountBaseCurrency));
            return list;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            
            if (request.DataType != typeof(TradeBar))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidBarType",
                    $"{request.DataType} type not supported, no history returned"));
                yield break;
            }
            
            if (request.Symbol.SecurityType != SecurityType.Equity && request.Symbol.SecurityType != SecurityType.Future && request.Symbol.SecurityType != SecurityType.Option)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSecurityType",
                    $"{request.Symbol.SecurityType} security type not supported, no history returned"));
                yield break;
            }

            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution not supported, no history returned"));
                yield break;
            }

            if (request.StartTimeUtc >= request.EndTimeUtc)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidDateRange",
                    "The history request start date must precede the end date, no history returned"));
                yield break;
            }


            DateTime latestTime = request.StartTimeUtc;
            var requests = new List<HistoryRequest>();
            requests.Add(request);

            foreach (var historyRequest in requests)
            {
                if (historyRequest.Symbol.ID.SecurityType != SecurityType.Equity && historyRequest.Symbol.ID.SecurityType != SecurityType.Future && historyRequest.Symbol.ID.SecurityType != SecurityType.Option)
                {
                    throw new ArgumentException("Zerodha does not support this security type: " + historyRequest.Symbol.ID.SecurityType);
                }

                if (historyRequest.StartTimeUtc >= historyRequest.EndTimeUtc)
                {
                    throw new ArgumentException("Invalid date range specified");
                }

                var start = historyRequest.StartTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);
                var end = historyRequest.EndTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);

                var history = Enumerable.Empty<BaseData>();

                if (Config.GetBool("zerodha-history-subscription"))
                {
                    switch (historyRequest.Resolution)
                    {
                        case Resolution.Minute:
                            history = GetHistoryForPeriod(historyRequest.Symbol, start, end, historyRequest.Resolution, "minute");
                            break;

                        case Resolution.Hour:
                            history = GetHistoryForPeriod(historyRequest.Symbol, start, end, historyRequest.Resolution, "60minute");
                            break;

                        case Resolution.Daily:
                            history = GetHistoryForPeriod(historyRequest.Symbol, start, end, historyRequest.Resolution, "day");
                            break;
                    }
                }

                foreach (var baseData in history)
                {
                    yield return baseData;
                }
            }
        }

        private IEnumerable<BaseData> GetHistoryForPeriod(Symbol symbol, DateTime start, DateTime end, Resolution resolution, string zerodhaResolution)
        {
            Log.Debug("ZerodhaBrokerage.GetHistoryForPeriod();");
            var scripSymbol = _symbolMapper.GetZerodhaInstrumentToken(symbol.Value, symbol.ID.Market);
            var candles = _kite.GetHistoricalData(scripSymbol.ToStringInvariant(), start, end, zerodhaResolution);

            if (!candles.Any())
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                    $"Exchange returned no data for {symbol} on history request " +
                    $"from {start:s} to {end:s}"));
            }

            var period = resolution.ToTimeSpan();

            foreach (var candle in candles)
            {
                yield return new TradeBar(candle.TimeStamp.ConvertFromUtc(TimeZones.Kolkata),symbol,candle.Open,candle.High,candle.Low,candle.Close,candle.Volume,resolution.ToTimeSpan());
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _aggregator.DisposeSafely();
            if (WebSocket.IsOpen)
            {
                WebSocket.Close();
            }
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"ZerodhaSubscriptionManager.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        private void OnMessage(object sender, MessageData e)
        {
            LastHeartbeatUtcTime = DateTime.UtcNow;

            // Verify if we're allowed to handle the streaming packet yet; while we're placing an order we delay the
            // stream processing a touch.
            try
            {
                if (_streamLocked)
                {
                    _messageBuffer.Enqueue(e);
                    return;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            OnMessageImpl(e);
        }

        /// <summary>
        /// Implementation of the OnMessage event
        /// </summary>
        /// <param name="e"></param>
        private void OnMessageImpl(MessageData e)
        {
            try
            {
                if (e.MessageType == WebSocketMessageType.Binary)
                {
                    if (e.Count > 1)
                    {
                        int offset = 0;
                        ushort count = ReadShort(e.Data, ref offset); //number of packets

                        for (ushort i = 0; i < count; i++)
                        {
                            ushort length = ReadShort(e.Data, ref offset); // length of the packet
                            Messages.Tick tick = new Messages.Tick();
                            if (length == 8) // ltp
                                tick = ReadLTP(e.Data, ref offset);
                            else if (length == 28) // index quote
                                tick = ReadIndexQuote(e.Data, ref offset);
                            else if (length == 32) // index quote
                                tick = ReadIndexFull(e.Data, ref offset);
                            else if (length == 44) // quote
                                tick = ReadQuote(e.Data, ref offset);
                            else if (length == 184) // full with marketdepth and timestamp
                                tick = ReadFull(e.Data, ref offset);
                            // If the number of bytes got from stream is less that that is required
                            // data is invalid. This will skip that wrong tick
                            if (tick.InstrumentToken != 0 && offset <= e.Count && tick.Mode == Constants.MODE_FULL)
                            {
                                var symbol = _symbolMapper.ConvertZerodhaSymbolToLeanSymbol(tick.InstrumentToken);

                                var bestBidQuote = tick.Bids[0];
                                var bestAskQuote = tick.Offers[0];

                                var time = tick.Timestamp ?? DateTime.UtcNow.ConvertFromUtc(TimeZones.Kolkata);

                                EmitQuoteTick(symbol, time, bestBidQuote.Price, bestBidQuote.Quantity, bestAskQuote.Price, bestAskQuote.Quantity);

                                if (_lastTradeTickTime != time)
                                {
                                    EmitTradeTick(symbol, time, tick.LastPrice, tick.LastQuantity);
                                    _lastTradeTickTime = time;
                                }
                            }
                        }
                    }
                }
                else if (e.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(e.Data.Take(e.Count).ToArray());

                    JObject messageDict = Utils.JsonDeserialize(message);
                    if ((string)messageDict["type"] == "order")
                    {
                        OnOrderUpdate(new Messages.Order(messageDict["data"]));
                    }
                    else if ((string)messageDict["type"] == "error")
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Zerodha WSS Error. Data: {e.Data} Exception: {messageDict["data"]}"));

                    }
                }
                else if (e.MessageType == WebSocketMessageType.Close)
                {
                    WebSocket.Close();
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Data} Exception: {exception}"));
                throw;
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal amount)
        {
            var tick = new Tick(time, symbol, string.Empty, symbol.ID.Market, Math.Abs(amount), price);
            _aggregator.Update(tick);
        }

        private void EmitQuoteTick(Symbol symbol, DateTime time, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            if (bidPrice > 0 && askPrice > 0)
            {
                var tick = new Tick(time, symbol, string.Empty, string.Empty, bidSize, bidPrice, askSize, askPrice);
                _aggregator.Update(tick);
            }
        }

        /// <summary>
        /// Reads 2 byte short int from byte stream
        /// </summary>
        private ushort ReadShort(byte[] b, ref int offset)
        {
            ushort data = (ushort)(b[offset + 1] + (b[offset] << 8));
            offset += 2;
            return data;
        }

        /// <summary>
        /// Reads 4 byte int32 from byte stream
        /// </summary>
        private uint ReadInt(byte[] b, ref int offset)
        {
            uint data = BitConverter.ToUInt32(new byte[] { b[offset + 3], b[offset + 2], b[offset + 1], b[offset + 0] }, 0);
            offset += 4;
            return data;
        }

        /// <summary>
        /// Reads an ltp mode tick from raw binary data
        /// </summary>
        private Messages.Tick ReadLTP(byte[] b, ref int offset)
        {
            Messages.Tick tick = new Messages.Tick();
            tick.Mode = Constants.MODE_LTP;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            return tick;
        }

        /// <summary>
        /// Reads a index's quote mode tick from raw binary data
        /// </summary>
        private Messages.Tick ReadIndexQuote(byte[] b, ref int offset)
        {
            Messages.Tick tick = new Messages.Tick();
            tick.Mode = Constants.MODE_QUOTE;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;
            tick.Change = ReadInt(b, ref offset) / divisor;
            return tick;
        }

        private Messages.Tick ReadIndexFull(byte[] b, ref int offset)
        {
            Messages.Tick tick = new Messages.Tick();
            tick.Mode = Constants.MODE_FULL;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;
            tick.Change = ReadInt(b, ref offset) / divisor;
            uint time = ReadInt(b, ref offset);
            tick.Timestamp = Time.UnixTimeStampToDateTime(time);
            return tick;
        }

        /// <summary>
        /// Reads a quote mode tick from raw binary data
        /// </summary>
        private Messages.Tick ReadQuote(byte[] b, ref int offset)
        {
            Messages.Tick tick = new Messages.Tick
            {
                Mode = Constants.MODE_QUOTE,
                InstrumentToken = ReadInt(b, ref offset)
            };

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.LastQuantity = ReadInt(b, ref offset);
            tick.AveragePrice = ReadInt(b, ref offset) / divisor;
            tick.Volume = ReadInt(b, ref offset);
            tick.BuyQuantity = ReadInt(b, ref offset);
            tick.SellQuantity = ReadInt(b, ref offset);
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;

            return tick;
        }

        /// <summary>
        /// Reads a full mode tick from raw binary data
        /// </summary>
        private Messages.Tick ReadFull(byte[] b, ref int offset)
        {
            Messages.Tick tick = new Messages.Tick();
            tick.Mode = Constants.MODE_FULL;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.LastQuantity = ReadInt(b, ref offset);
            tick.AveragePrice = ReadInt(b, ref offset) / divisor;
            tick.Volume = ReadInt(b, ref offset);
            tick.BuyQuantity = ReadInt(b, ref offset);
            tick.SellQuantity = ReadInt(b, ref offset);
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;

            // KiteConnect 3 fields
            tick.LastTradeTime = Time.UnixTimeStampToDateTime(ReadInt(b, ref offset));
            tick.OI = ReadInt(b, ref offset);
            tick.OIDayHigh = ReadInt(b, ref offset);
            tick.OIDayLow = ReadInt(b, ref offset);
            tick.Timestamp = Time.UnixTimeStampToDateTime(ReadInt(b, ref offset));


            tick.Bids = new DepthItem[5];
            for (int i = 0; i < 5; i++)
            {
                tick.Bids[i].Quantity = ReadInt(b, ref offset);
                tick.Bids[i].Price = ReadInt(b, ref offset) / divisor;
                tick.Bids[i].Orders = ReadShort(b, ref offset);
                offset += 2;
            }

            tick.Offers = new DepthItem[5];
            for (int i = 0; i < 5; i++)
            {
                tick.Offers[i].Quantity = ReadInt(b, ref offset);
                tick.Offers[i].Price = ReadInt(b, ref offset) / divisor;
                tick.Offers[i].Orders = ReadShort(b, ref offset);
                offset += 2;
            }
            return tick;
        }
        #endregion

    }

}
