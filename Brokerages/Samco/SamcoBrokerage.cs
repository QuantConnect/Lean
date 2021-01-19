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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Samco.SamcoMessages;
using QuantConnect.Orders.Fees;
using System.Threading;
using QuantConnect.Configuration;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Samco Brokerage implementation
    /// </summary>
    public partial class SamcoBrokerage : Brokerage, IDataQueueHandler, IDataQueueUniverseProvider
    {
        private readonly IAlgorithm _algorithm;
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private readonly DataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private const int ConnectionTimeout = 30000;
        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected SamcoWebSocketClientWrapper WebSocket;
        private readonly SamcoSymbolMapper _symbolMapper;
        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();
        private readonly ConcurrentDictionary<string, Symbol> _subscriptionsById = new ConcurrentDictionary<string, Symbol>();
        private readonly SamcoBrokerageAPI _samcoAPI;
        private readonly IDataAggregator _aggregator;

        /// <summary>
        /// Timestamp of most recent heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime = DateTime.UtcNow;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        private ConcurrentDictionary<string, QuoteUpdate> quotes = new ConcurrentDictionary<string, QuoteUpdate>();
        private readonly CancellationTokenSource _ctsFillMonitor = new CancellationTokenSource();
        private readonly Task _fillMonitorTask;
        private readonly AutoResetEvent _fillMonitorResetEvent = new AutoResetEvent(false);
        private readonly int _fillMonitorTimeout = Config.GetInt("samco.FillMonitorTimeout", 500);
        private readonly ConcurrentDictionary<string, Order> _pendingOrders = new ConcurrentDictionary<string, Order>();

        //EQUITY / COMMODITY
        private readonly string _tradingSegment;
        // MIS/CNC/NRML
        private readonly string _samcoProductType;

        private readonly List<string> subscribeInstrumentTokens = new List<string>();
        private readonly List<string> unSubscribeInstrumentTokens = new List<string>();


        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="tradingSegment">Trading Segment</param>
        /// <param name="productType">Product Type</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="yob">year of birth</param>
        public SamcoBrokerage(string tradingSegment, string productType, string apiKey, string apiSecret, string yob, IAlgorithm algorithm, IDataAggregator aggregator)
            : base("Samco")
        {
            _tradingSegment = tradingSegment;
            _samcoProductType = productType;
            _algorithm = algorithm;
            _aggregator = aggregator;
            _samcoAPI = new SamcoBrokerageAPI();
            _samcoAPI.Authorize(apiKey, apiSecret, yob);
            _symbolMapper = new SamcoSymbolMapper();
            var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();

            WebSocket = new SamcoWebSocketClientWrapper();
            WebSocket.Initialize("");
            WebSocket.SetAuthTokenHeader(_samcoAPI.token);
            WebSocket.Message += OnMessage;
            WebSocket.Open += (sender, args) =>
            {
                Log.Trace($"SamcoBrokerage(): WebSocket.Open. Subscribing");
                Subscribe(GetSubscribed());
            };
            WebSocket.Error += OnError;

            subscriptionManager.SubscribeImpl += (s, t) =>
            {
                Subscribe(s);
                return true;
            };
            subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            _subscriptionManager = subscriptionManager;
            _fillMonitorTask = Task.Factory.StartNew(FillMonitorAction, _ctsFillMonitor.Token);
            Log.Trace("Start Samco Brokerage");
        }

        private decimal CalculateBrokerageOrderFee(decimal orderValue, OrderDirection orderDirection)
        {
            bool isSell = orderDirection == OrderDirection.Sell ? true : false;
            orderValue = Math.Abs(orderValue);
            var multiplied = orderValue * 0.0003M;
            var brokerage = (multiplied > 20) ? 20 : Math.Round(multiplied, 2);

            var turnover = Math.Round(orderValue, 2);

            decimal sttTotal = 0;
            if (isSell)
            {
                sttTotal = Math.Round(orderValue * 0.00025M, 2);
            }

            var exchangeTxncharge = Math.Round(turnover * 0.0000325M, 2);
            var cc = 0;

            var stax = Math.Round(0.18M * (brokerage + exchangeTxncharge), 2);

            var sebiCharges = Math.Round((turnover * 0.000001M), 2);
            decimal stampDutyCharges = 0;
            if (!isSell)
            {
                stampDutyCharges = Math.Round(orderValue * 0.00003M, 2);
            }

            return Math.Round(brokerage + sttTotal + exchangeTxncharge + stampDutyCharges + cc + stax + sebiCharges, 2);
        }

        private void FillMonitorAction()
        {
            Log.Trace("SamcoBrokerage.FillMonitorAction(): task started");

            try
            {
                foreach (var order in GetOpenOrders())
                {
                    _pendingOrders.TryAdd(order.BrokerId.First(), order);
                }

                while (!_ctsFillMonitor.IsCancellationRequested)
                {
                    _fillMonitorResetEvent.WaitOne(TimeSpan.FromMilliseconds(_fillMonitorTimeout), _ctsFillMonitor.Token);

                    foreach (var kvp in _pendingOrders)
                    {
                        var orderId = kvp.Key;
                        var order = kvp.Value;

                        var response = _samcoAPI.GetOrderDetails(orderId);
                        if (response.status != "Success")
                        {
                            OnMessage(new BrokerageMessageEvent(
                                BrokerageMessageType.Warning,
                                -1,
                                $"SamcoBrokerage.FillMonitorAction(): request failed: [{response.status}] {response.statusMessage}, Content: {response.ToString()}, ErrorMessage: {response.validationErrors}"));

                            continue;
                        }

                        //Process cancelled orders here.
                        if (response.orderStatus == "CANCELLED")
                        {
                            OnOrderClose(response.orderDetails);
                        }
                       
                        // Process rest of the orders here.
                        EmitFillOrder(response.orderDetails);
                        
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, exception.Message));
            }

            Log.Trace("SamcoBrokerage.FillMonitorAction(): task ended");

        }

        /// <summary>
        /// Gets Quote using Samco API
        /// </summary>
        /// <returns> Quote Response</returns>
        public QuoteResponse GetQuote(Symbol symbol)
        {
            return _samcoAPI.GetQuote(symbol.ID.Symbol, symbol.ID.Market);
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
            var sub = new Subscription();
            foreach (var symbol in symbols)
            {

                try
                {
                    var quote = GetQuote(symbol);
                    _subscriptionsById[quote.listingId] = symbol;
                    sub.request.data.symbols.Add(new Subscription.Symbol { symbol = quote.listingId });
                    if (!subscribeInstrumentTokens.Contains(quote.listingId))
                    {
                        subscribeInstrumentTokens.Add(quote.listingId);

                        unSubscribeInstrumentTokens.Remove(quote.listingId);
                        _subscriptionsById[quote.listingId] = symbol;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    throw;
                }
            }
            var request = JsonConvert.SerializeObject(sub);
            WebSocket.Send(request);
            WebSocket.Send("\n");

        }

        private IEnumerable<Symbol> GetSubscribed()
        {
            return _subscriptionManager.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        private bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                var sub = new Subscription();
                sub.request.request_type = "unsubcribe";

                foreach (var symbol in symbols)
                {
                    try
                    {
                        var quote = GetQuote(symbol);
                        sub.request.data.symbols.Add(new Subscription.Symbol { symbol = quote.listingId });
                        if (!unSubscribeInstrumentTokens.Contains(quote.listingId))
                        {
                            unSubscribeInstrumentTokens.Add(quote.listingId);
                            subscribeInstrumentTokens.Remove(quote.listingId);
                            Symbol unSubscribeSymbol;
                            _subscriptionsById.TryRemove(quote.listingId, out unSubscribeSymbol);
                        }

                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                        throw;
                    }

                }
                var request = JsonConvert.SerializeObject(sub);
                WebSocket.Send(request);
                WebSocket.Send("\n");
                return true;
            }
            return false;
        }

        private void OnOrderClose(OrderDetails orderDetails)
        {
            var brokerId = orderDetails.orderNumber;
            if (orderDetails.orderStatus == "CANCELLED")
            {
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
                Order outOrder;
                if (CachedOrderIDs.TryRemove(order.Id, out outOrder))
                {
                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Samco Order Event")
                    { Status = OrderStatus.Canceled });
                }
            }
        }

        private void EmitFillOrder(OrderDetails orderDetails)
        {
            try
            {
                var brokerId = orderDetails.orderNumber;
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

                var symbol = _symbolMapper.GetLeanSymbol(orderDetails.tradingSymbol, _symbolMapper.GetBrokerageSecurityType(orderDetails.tradingSymbol, orderDetails.exchange), orderDetails.exchange);
                var fillPrice = decimal.Parse(orderDetails.filledPrice, NumberStyles.Float, CultureInfo.InvariantCulture);
                var fillQuantity = decimal.Parse(orderDetails.filledQuantity, NumberStyles.Float, CultureInfo.InvariantCulture);
                var updTime = DateTime.UtcNow;
                var orderFee = new OrderFee(new CashAmount(CalculateBrokerageOrderFee(fillPrice * fillQuantity, order.Direction), Currencies.INR));
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
                    order.Direction, fillPrice, fillQuantity,
                    orderFee, $"Samco Order Event {order.Direction}"
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
            Log.Trace("SamcoBrokerage.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        public void UnlockStream()
        {
            Log.Trace("SamcoBrokerage.UnlockStream(): Stream Unlocked.");
            Log.Trace("SamcoBrokerage.UnlockStream(): Processing Backlog...");
            while (_messageBuffer.Any())
            {
                WebSocketMessage e;
                _messageBuffer.TryDequeue(out e);
                OnMessageImpl(e);
            }
            Log.Trace("SamcoBrokerage.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        public override string AccountBaseCurrency => Currencies.INR;

        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Connects to Samco Websocket
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            Log.Trace("SamcoBrokerage.Connect(): Connecting...");

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
            SamcoOrderResponse orderResponse = _samcoAPI.PlaceOrder(order, order.Symbol.Value, _samcoProductType, _algorithm);

            var orderFee = new OrderFee(new CashAmount(CalculateBrokerageOrderFee(order.Quantity * order.Price, order.Direction), Currencies.INR));

            if (orderResponse.validationErrors != null)
            {
                var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.validationErrors.ToString()}";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                UnlockStream();
                return true;
            }

            if (orderResponse.status == "Success")
            {

                if (string.IsNullOrEmpty(orderResponse.orderNumber))
                {
                    var errorMessage = $"Error parsing response from place order: {orderResponse.statusMessage}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse.statusMessage, errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = orderResponse.orderNumber;
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
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Submitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                _pendingOrders.TryAdd(brokerId, order);
                _fillMonitorResetEvent.Set();

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.statusMessage}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return true;

        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            LockStream();
            var orderResponse = _samcoAPI.ModifyOrder(order, _algorithm);
            var orderFee = new OrderFee(new CashAmount(CalculateBrokerageOrderFee(order.Quantity * order.Price, order.Direction), Currencies.INR));
            if (orderResponse.status == "Success")
            {
                if (string.IsNullOrEmpty(orderResponse.orderNumber))
                {
                    var errorMessage = $"Error parsing response from place order: {orderResponse.statusMessage}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse.status, errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = orderResponse.orderNumber;
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
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.UpdateSubmitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.statusMessage}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return true;
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            LockStream();
            SamcoOrderResponse orderResponse = _samcoAPI.CancelOrder(order.Id.ToStringInvariant());
            if (orderResponse.status == "Success")
            {
                Order orderRemoved;
                _pendingOrders.TryRemove(order.Id.ToStringInvariant(), out orderRemoved);
                UnlockStream();
                return true;
            }
            UnlockStream();
            return false;
        }


        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var allOrders = _samcoAPI.GetOrderBook();

            List<Order> list = new List<Order>();

            //Only loop if there are any actual orders inside response
            if (allOrders.status != "Failure" && allOrders.orderBookDetails.Count > 0)
            {
                foreach (var item in allOrders.orderBookDetails.Where(z => z.orderStatus.ToUpperInvariant() == "PENDING"))
                {

                    Order order;
                    if (item.orderType.ToLowerInvariant() == "MKT")
                    {
                        order = new MarketOrder { Price = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else if (item.orderType.ToLowerInvariant() == "L")
                    {
                        order = new LimitOrder { LimitPrice = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else if (item.orderType.ToLowerInvariant() == "SL-M")
                    {
                        order = new StopMarketOrder { StopPrice = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, allOrders.status,
                            "SamcoBrorage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.orderType));
                        continue;
                    }

                    var itemTotalQty = Convert.ToInt32(item.totalQuantity, CultureInfo.InvariantCulture);
                    var originalQty = Convert.ToInt32(item.quantity, CultureInfo.InvariantCulture);
                    order.Quantity = item.transactionType.ToLowerInvariant() == "sell" ? -itemTotalQty : originalQty;
                    order.BrokerId = new List<string> { item.orderNumber };
                    order.Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol, _symbolMapper.GetBrokerageSecurityType(item.tradingSymbol, item.exchange), item.exchange);
                    order.Time = Convert.ToDateTime(item.orderTime, CultureInfo.InvariantCulture);
                    order.Status = ConvertOrderStatus(item);
                    order.Price = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture);
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

        private OrderStatus ConvertOrderStatus(OrderDetails orderDetails)
        {
            var filledQty = Convert.ToInt32(orderDetails.filledQuantity, CultureInfo.InvariantCulture);
            var pendingQty = Convert.ToInt32(orderDetails.pendingQuantity, CultureInfo.InvariantCulture);
            var orderDetail = _samcoAPI.GetOrderDetails(orderDetails.orderNumber);
            if (orderDetails.orderStatus != "complete" && filledQty == 0)
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
            else if (orderDetail.orderStatus.ToUpperInvariant() == "CANCELLED")
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
            if (_samcoProductType.ToUpperInvariant() == "MIS")
            {
                var positions = _samcoAPI.GetPositions("DAY");
                if (positions.Status == "Failure")
                {
                    return holdingsList;
                }
                foreach (var position in positions.PositionDetails)
                {
                    //We only need Intraday positions here, Not carryforward postions
                    if (position.ProductCode.ToUpperInvariant() == "MIS" && position.PositionType.ToUpperInvariant() == "DAY")
                    {
                        Holding holding = new Holding
                        {
                            AveragePrice = Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture),
                            Symbol = _symbolMapper.GetLeanSymbol(position.TradingSymbol, _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol, position.Exchange), position.Exchange),
                            MarketPrice = Convert.ToDecimal(position.LastTradedPrice, CultureInfo.InvariantCulture),
                            Quantity = position.NetQuantity,
                            Type = _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol, position.Exchange),
                            UnrealizedPnL = (Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture) - Convert.ToDecimal(position.LastTradedPrice,
                            CultureInfo.InvariantCulture)) * position.NetQuantity,
                            CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                            MarketValue = Convert.ToDecimal(position.LastTradedPrice,
                            CultureInfo.InvariantCulture) * position.NetQuantity

                        };
                        holdingsList.Add(holding);
                    }
                }
            }
            else if (_samcoProductType.ToUpperInvariant() == "CNC")
            {
                var holdingResponse = _samcoAPI.GetHoldings();
                if (holdingResponse.status == "Failure")
                {
                    return holdingsList;
                }
                if (holdingResponse.holdingDetails == null)
                {
                    return holdingsList;
                }
                foreach (var item in holdingResponse.holdingDetails)
                {
                    Holding holding = new Holding
                    {
                        AveragePrice = item.averagePrice,
                        Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol, _symbolMapper.GetBrokerageSecurityType(item.tradingSymbol, item.exchange), item.exchange),
                        MarketPrice = item.lastTradedPrice,
                        Quantity = item.holdingsQuantity,
                        Type = _symbolMapper.GetBrokerageSecurityType(item.tradingSymbol, item.exchange),
                        UnrealizedPnL = (item.averagePrice - item.lastTradedPrice) * item.holdingsQuantity,
                        CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                        MarketValue = item.lastTradedPrice * item.holdingsQuantity
                    };
                    holdingsList.Add(holding);
                }
            }
            else
            {
                var positions = _samcoAPI.GetPositions("NET");
                if (positions.Status == "Failure")
                {
                    return holdingsList;
                }
                foreach (var position in positions.PositionDetails)
                {
                    //We only need carry forward NRML positions here, Not intraday postions.
                    if (position.ProductCode.ToUpperInvariant() == "NRML" && position.PositionType.ToUpperInvariant() == "NET")
                    {
                        Holding holding = new Holding
                        {
                            AveragePrice = Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture),
                            Symbol = _symbolMapper.GetLeanSymbol(position.TradingSymbol, _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol, position.Exchange), position.Exchange),
                            MarketPrice = Convert.ToDecimal(position.LastTradedPrice, CultureInfo.InvariantCulture),
                            Quantity = position.NetQuantity,
                            Type = _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol, position.Exchange),
                            UnrealizedPnL = (Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture) - Convert.ToDecimal(position.LastTradedPrice,
                            CultureInfo.InvariantCulture)) * position.NetQuantity,
                            CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                            MarketValue = Convert.ToDecimal(position.LastTradedPrice,
                            CultureInfo.InvariantCulture) * position.NetQuantity

                        };
                        holdingsList.Add(holding);
                    }
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
            decimal amt;
            var list = new List<CashAmount>();
            var response = _samcoAPI.GetUserLimits();
            if (response != null)
            {
                if (_tradingSegment == "EQUITY")
                {
                    amt = Convert.ToDecimal(response.EquityLimit.NetAvailableMargin, CultureInfo.InvariantCulture);
                }
                else if (_tradingSegment == "COMMODITY")
                {
                    amt = Convert.ToDecimal(response.CommodityLimit.NetAvailableMargin, CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new ArgumentException("Invalid Samco trading segment: " + _tradingSegment + ". Valid values are: EQUITY / COMMODITY");
                }
                list.Add(new CashAmount(amt, AccountBaseCurrency));
            }
            return list;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
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

            // if the end time cannot be rounded to resolution without a remainder
            //TODO Fix this 
            //if (request.EndTimeUtc.Ticks % request.Resolution.ToTimeSpan().Ticks > 0)
            //{
            //    // give a warning and return
            //    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidEndTime",
            //        "The history request's end date is not a full multiple of a resolution. " +
            //        "Samco API only allows to support trade bar history requests. The start and end dates " +
            //        "of a such request are expected to match exactly with the beginning of the first bar and ending of the last"));
            //    yield break;
            //}

            if (request.Resolution != Resolution.Minute)
            {
                throw new ArgumentException($"SamcoBrokerage.ConvertResolution: Unsupported resolution type: {request.Resolution}");
            }

            string symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var period = request.Resolution.ToTimeSpan();
            DateTime latestTime = request.StartTimeUtc;
            do
            {
                latestTime = latestTime.AddDays(29);
                var start = request.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var end = latestTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string endpoint = $"/intraday/candleData?symbolName={symbol}&fromDate={start}&toDate={end}";

                var restRequest = new RestRequest(endpoint, Method.GET);
                var response = _samcoAPI.ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(
                        $"SamcoBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, " +
                        $"Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                // we need to drop the last bar provided by the exchange as its open time is a history request's end time
                var candles = JsonConvert.DeserializeObject<CandleResponse>(response.Content);


                if (!candles.intradayCandleData.Any())
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
                        $"Exchange returned no data for {symbol} on history request " +
                        $"from {request.StartTimeUtc:s} to {request.EndTimeUtc:s}"));
                    yield break;
                }

                foreach (var candle in candles.intradayCandleData)
                {
                    yield return new TradeBar()
                    {
                        Time = candle.dateTime,
                        Symbol = request.Symbol,
                        Low = candle.low,
                        High = candle.high,
                        Open = candle.open,
                        Close = candle.close,
                        Volume = candle.volume,
                        Value = candle.close,
                        DataType = MarketDataType.TradeBar,
                        Period = period,
                        EndTime = candle.dateTime.AddMinutes(1)
                    };
                }
            } while (latestTime < request.EndTimeUtc);
        }

        // <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"SamcoBrokerage.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        public void OnMessage(object sender, WebSocketMessage e)
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
        private void OnMessageImpl(WebSocketMessage e)
        {
            try
            {
                var token = JToken.Parse(e.Message);
                if (token is JObject)
                {
                    var raw = token.ToObject<QuoteUpdate>();
                    if (raw.response.streaming_type.ToLowerInvariant() == "quote")
                    {
                        QuoteUpdate existing;
                        if (!quotes.TryGetValue(raw.response.data.sym, out existing))
                        {
                            existing = raw;
                            quotes[raw.response.data.sym] = raw;
                        }

                        var upd = raw.response.data;
                        var sym = _subscriptionsById[raw.response.data.sym];

                        EmitQuoteTick(sym, upd.bPr, upd.bSz, upd.aPr, upd.aSz);

                        if (existing.response.data.vol == raw.response.data.vol)
                        {
                            return;
                        }

                        EmitTradeTick(sym, upd.lTrdT, upd.ltp, upd.ltq);
                    }
                    else
                    {
                        Log.Trace($"SamcoSubscriptionManager.OnMessage(): Unexpected message format: {e.Message}");
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal amount)
        {
            try
            {
                lock (TickLocker)
                {
                    _aggregator.Update(new Tick
                    {
                        Value = price,
                        Time = time.ConvertToUtc(TimeZones.Kolkata),
                        //Time = DateTime.UtcNow,
                        Symbol = symbol,
                        Exchange = symbol.ID.Market,
                        TickType = TickType.Trade,
                        Quantity = amount
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            try
            {
                var tick = new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    //Value = (askPrice + bidPrice) / 2m,
                    Symbol = symbol,
                    Time = DateTime.UtcNow,
                    //Exchange = symbol.ID.Market,
                    TickType = TickType.Quote,
                    AskSize = askSize,
                    BidSize = bidSize
                };

                lock (TickLocker)
                {
                    _aggregator.Update(tick);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private Holding ConvertHolding(HoldingsResponse.HoldingDetail detail)
        {
            var holding = new Holding
            {
                Symbol = _symbolMapper.GetLeanSymbol(detail.tradingSymbol, _symbolMapper.GetLeanSecurityType(detail.tradingSymbol,detail.exchange), detail.exchange),
                AveragePrice = detail.averagePrice,
                Quantity = detail.holdingsQuantity,
                UnrealizedPnL = (detail.lastTradedPrice - detail.averagePrice) * detail.holdingsQuantity,
                CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                Type = SecurityType.Equity
            };

            try
            {
                holding.MarketPrice = detail.lastTradedPrice;
            }
            catch (Exception)
            {
                Log.Error($"SamcoBrokerage.ConvertHolding(): failed to set {holding.Symbol} market price");
                throw;
            }
            return holding;
        }
    }
}