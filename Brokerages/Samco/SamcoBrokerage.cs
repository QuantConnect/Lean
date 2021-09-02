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
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Samco.SamcoMessages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Samco Brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(SamcoBrokerageFactory))]
    public partial class SamcoBrokerage : Brokerage, IDataQueueHandler
    {
        private const int ConnectionTimeout = 30000;
        private readonly IDataAggregator _aggregator;
        private readonly IAlgorithm _algorithm;
        private readonly CancellationTokenSource _ctsFillMonitor = new CancellationTokenSource();
        private readonly AutoResetEvent _fillMonitorResetEvent = new AutoResetEvent(false);
        private readonly Task _fillMonitorTask;
        private readonly int _fillMonitorTimeout = Config.GetInt("samco.FillMonitorTimeout", 500);
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private readonly BrokerageConcurrentMessageHandler<WebSocketMessage> _messageHandler;
        private readonly ConcurrentDictionary<string, Order> _pendingOrders = new ConcurrentDictionary<string, Order>();
        private readonly SamcoBrokerageAPI _samcoAPI;
        private readonly string _samcoApiKey;
        private readonly string _samcoApiSecret;
        private readonly string _samcoYob;
        private readonly Timer _reconnectTimer;

        // MIS/CNC/NRML
        private readonly string _samcoProductType;

        private readonly ISecurityProvider _securityProvider;
        private readonly DataQueueHandlerSubscriptionManager _subscriptionManager;
        private readonly ConcurrentDictionary<string, Symbol> _subscriptionsById = new ConcurrentDictionary<string, Symbol>();
        private readonly SamcoSymbolMapper _symbolMapper;

        //EQUITY / COMMODITY
        private readonly string _tradingSegment;

        private readonly List<string> _subscribeInstrumentTokens = new List<string>();
        private readonly List<string> _unSubscribeInstrumentTokens = new List<string>();

        private DateTime _lastTradeTickTime;

        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected WebSocketClientWrapper WebSocket;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();

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
            _securityProvider = algorithm.Portfolio;
            _aggregator = aggregator;
            _samcoAPI = new SamcoBrokerageAPI();
            _symbolMapper = new SamcoSymbolMapper();
            _messageHandler = new BrokerageConcurrentMessageHandler<WebSocketMessage>(OnMessageImpl);
            _samcoApiKey = apiKey;
            _samcoApiSecret = apiSecret;
            _samcoYob = yob;

            var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();

            WebSocket = new WebSocketClientWrapper();
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

            // A single connection to stream.stocknote.com is only valid for 24 hours;
            // expect to be disconnected at the 24 hour mark
            _reconnectTimer = new Timer
            {
                // 23.5 hours
                Interval = 23.5 * 60 * 60 * 1000
            };
            _reconnectTimer.Elapsed += (s, e) =>
            {
                Log.Trace("Daily websocket restart: disconnect");
                Disconnect();

                Log.Trace("Daily websocket restart: connect");
                Connect();
            };
            Log.Trace("Start Samco Brokerage");
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
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            var brokerId = order.BrokerId[0].ToStringInvariant();
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                SamcoOrderResponse orderResponse = _samcoAPI.CancelOrder(brokerId);
                if (orderResponse.status == "Success")
                {
                    Order orderRemoved;
                    _pendingOrders.TryRemove(brokerId, out orderRemoved);

                    submitted = true;
                    return;
                }
                return;
            });
            return submitted;
        }

        /// <summary>
        /// Connects to Samco Websocket
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            Log.Trace("SamcoBrokerage.Connect(): Connecting...");

            _samcoAPI.Authorize(_samcoApiKey, _samcoApiSecret, _samcoYob);
            WebSocket.Initialize("wss://stream.stocknote.com", _samcoAPI.SamcoToken);

            var resetEvent = new ManualResetEvent(false);
            EventHandler triggerEvent = (o, args) => resetEvent.Set();
            WebSocket.Open += triggerEvent;
            WebSocket.Connect();
            if (!resetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException("Websockets connection timeout.");
            }
            _reconnectTimer.Start();
            WebSocket.Open -= triggerEvent;
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            if (WebSocket.IsOpen)
            {
                _reconnectTimer.Stop();
                WebSocket.Close();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _aggregator.Dispose();
            _samcoAPI.Dispose();
            _ctsFillMonitor.Dispose();
            _fillMonitorTask.Wait(TimeSpan.FromSeconds(5));
            _fillMonitorResetEvent.Dispose();
            _reconnectTimer.Dispose();
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdingsList = new List<Holding>();
            var samcoProductTypeUpper = _samcoProductType.ToUpperInvariant();
            var productTypeMIS = "MIS";
            var productTypeCNC = "CNC";
            var productTypeNRML = "NRML";
            // get MIS and NRML Positions
            if (string.IsNullOrEmpty(samcoProductTypeUpper) || samcoProductTypeUpper == productTypeMIS)
            {
                var positions = _samcoAPI.GetPositions("DAY");
                if (positions.Status != "Failure")
                {
                    foreach (var position in positions.PositionDetails)
                    {
                        //We only need Intraday positions here, Not carryforward postions
                        if (position.ProductCode.ToUpperInvariant() == productTypeMIS && position.PositionType.ToUpperInvariant() == "DAY")
                        {
                            Holding holding = new Holding
                            {
                                AveragePrice = Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture),
                                Symbol = _symbolMapper.GetLeanSymbol(position.TradingSymbol, _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol)),
                                MarketPrice = Convert.ToDecimal(position.LastTradedPrice, CultureInfo.InvariantCulture),
                                Quantity = position.NetQuantity,
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
            }
            // get CNC Positions
            if (string.IsNullOrEmpty(samcoProductTypeUpper) || samcoProductTypeUpper == productTypeCNC)
            {
                var holdingResponse = _samcoAPI.GetHoldings();
                if (holdingResponse.status != "Failure" && holdingResponse.holdingDetails != null)
                {
                    foreach (var item in holdingResponse.holdingDetails)
                    {
                        Holding holding = new Holding
                        {
                            AveragePrice = item.averagePrice,
                            Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol, _symbolMapper.GetBrokerageSecurityType(item.tradingSymbol)),
                            MarketPrice = item.lastTradedPrice,
                            Quantity = item.holdingsQuantity,
                            UnrealizedPnL = (item.averagePrice - item.lastTradedPrice) * item.holdingsQuantity,
                            CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                            MarketValue = item.lastTradedPrice * item.holdingsQuantity
                        };
                        holdingsList.Add(holding);
                    }
                }
            }
            // get NRML Positions
            if (string.IsNullOrEmpty(samcoProductTypeUpper) || samcoProductTypeUpper == productTypeNRML)
            {
                var positions = _samcoAPI.GetPositions("NET");
                if (positions.Status != "Failure")
                {
                    foreach (var position in positions.PositionDetails)
                    {
                        //We only need carry forward NRML positions here, Not intraday postions.
                        if (position.ProductCode.ToUpperInvariant() == productTypeNRML && position.PositionType.ToUpperInvariant() == "NET")
                        {
                            Holding holding = new Holding
                            {
                                AveragePrice = Convert.ToDecimal(position.AveragePrice, CultureInfo.InvariantCulture),
                                Symbol = _symbolMapper.GetLeanSymbol(position.TradingSymbol, _symbolMapper.GetBrokerageSecurityType(position.TradingSymbol)),
                                MarketPrice = Convert.ToDecimal(position.LastTradedPrice, CultureInfo.InvariantCulture),
                                Quantity = position.NetQuantity,
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
            // Samco API only allows us to support history requests for TickType.Trade
            if (request.TickType != TickType.Trade)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidTickType",
                    $"{request.TickType} TickType not supported, no history returned"));
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

            if (request.Resolution != Resolution.Minute)
            {
                throw new ArgumentException($"SamcoBrokerage.ConvertResolution: Unsupported resolution type: {request.Resolution}");
            }

            var leanSymbol = request.Symbol;
            var securityExchange = _securityProvider.GetSecurity(leanSymbol).Exchange;
            var exchange = _symbolMapper.GetDefaultExchange(leanSymbol);
            var isIndex = leanSymbol.SecurityType == SecurityType.Index;

            var history = _samcoAPI.GetIntradayCandles(request.Symbol, exchange, request.StartTimeLocal, request.EndTimeLocal, request.Resolution, isIndex);

            foreach (var baseData in history)
            {
                if (!securityExchange.DateTimeIsOpen(baseData.Time) && !request.IncludeExtendedMarketHours)
                {
                    continue;
                }
                yield return baseData;
            }
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
                foreach (var item in allOrders.orderBookDetails.Where(z => z.orderStatus.ToUpperInvariant() == "OPEN"))
                {
                    Order order;
                    if (item.orderType.ToUpperInvariant() == "MKT")
                    {
                        order = new MarketOrder { Price = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else if (item.orderType.ToUpperInvariant() == "L")
                    {
                        order = new LimitOrder { LimitPrice = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else if (item.orderType.ToUpperInvariant() == "SL-M")
                    {
                        order = new StopMarketOrder { StopPrice = Convert.ToDecimal(item.orderPrice, CultureInfo.InvariantCulture) };
                    }
                    else
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, allOrders.status,
                            "SamcoBrorage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.orderType));
                        continue;
                    }

                    var brokerageSecurityType = _symbolMapper.GetBrokerageSecurityType(item.tradingSymbol);
                    var itemTotalQty = Convert.ToInt32(item.totalQuantity, CultureInfo.InvariantCulture);
                    var originalQty = Convert.ToInt32(item.quantity, CultureInfo.InvariantCulture);
                    order.Quantity = item.transactionType.ToLowerInvariant() == "sell" ? -itemTotalQty : originalQty;
                    order.BrokerId = new List<string> { item.orderNumber };
                    order.Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol, brokerageSecurityType);
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

        /// <summary>
        /// Gets Quote using Samco API
        /// </summary>
        /// <returns>Quote Response</returns>
        public QuoteResponse GetQuote(Symbol symbol)
        {
            var exchange = _symbolMapper.GetDefaultExchange(symbol);
            return _samcoAPI.GetQuote(symbol.ID.Symbol, exchange);
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                var orderFee = OrderFee.Zero;
                var orderProperties = order.Properties as IndiaOrderProperties;
                var samcoProductType = _samcoProductType;
                if (orderProperties == null || orderProperties.Exchange == null)
                {
                    var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: Please specify a valid order properties with an exchange value";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));
                    return;
                }
                if (orderProperties.ProductType != null)
                {
                    samcoProductType = orderProperties.ProductType;
                }
                else if (string.IsNullOrEmpty(samcoProductType))
                {
                    throw new ArgumentException("Please set ProductType in config or provide a value in DefaultOrderProperties");
                }

                SamcoOrderResponse orderResponse = _samcoAPI.PlaceOrder(order, order.Symbol.Value, orderProperties.Exchange.ToString().ToUpperInvariant(), samcoProductType);

                if (orderResponse.validationErrors != null)
                {
                    var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.validationErrors.ToString()}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                    submitted = true;
                    return;
                }

                if (orderResponse.status == "Success")
                {
                    if (string.IsNullOrEmpty(orderResponse.orderNumber))
                    {
                        var errorMessage = $"Error parsing response from place order: {orderResponse.statusMessage}";
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse.statusMessage, errorMessage));

                        submitted = true;
                        return;
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

                    submitted = true;
                    return;
                }

                var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.statusMessage}";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

                submitted = true;
            });
            return submitted;
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
            //re add already subscribed symbols and send in one go
            foreach (var listingId in _subscribeInstrumentTokens)
            {
                try
                {
                    sub.request.data.symbols.Add(new Subscription.Symbol { symbol = listingId });
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    throw;
                }
            }
            foreach (var symbol in symbols)
            {
                try
                {
                    var scrip = _symbolMapper.SamcoSymbols.Where(x => x.Name.ToUpperInvariant() == symbol.ID.Symbol).First();
                    var listingId = scrip.SymbolCode;
                    if (!_subscribeInstrumentTokens.Contains(listingId))
                    {
                        sub.request.data.symbols.Add(new Subscription.Symbol { symbol = listingId });
                        _subscribeInstrumentTokens.Add(listingId);
                        _unSubscribeInstrumentTokens.Remove(listingId);
                        _subscriptionsById[listingId] = symbol;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    throw;
                }
            }
            var request = JsonConvert.SerializeObject(sub);
            // required to flush input json as per samco forum
            request = request + "\n";
            WebSocket.Send(request);
        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Order order)
        {
            var submitted = false;

            _messageHandler.WithLockedStream(() =>
            {
                var orderResponse = _samcoAPI.ModifyOrder(order);
                var orderFee = OrderFee.Zero;
                if (orderResponse.status == "Success")
                {
                    if (string.IsNullOrEmpty(orderResponse.orderNumber))
                    {
                        var errorMessage = $"Error parsing response from place order: {orderResponse.statusMessage}";
                        OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse.status, errorMessage));

                        submitted = true;
                        return;
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

                    submitted = true;
                    return;
                }

                var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse.statusMessage}";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Samco Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

                submitted = true;
                return;
            });
            return submitted;
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

        private void EmitFillOrder(SamcoOrderResponse orderResponse)
        {
            try
            {
                var brokerId = orderResponse.orderNumber;
                var orderDetails = orderResponse.orderDetails;
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

                var brokerageSecurityType = _symbolMapper.GetBrokerageSecurityType(orderDetails.tradingSymbol);
                var symbol = _symbolMapper.GetLeanSymbol(orderDetails.tradingSymbol, brokerageSecurityType);
                var fillPrice = decimal.Parse(orderDetails.filledPrice, NumberStyles.Float, CultureInfo.InvariantCulture);
                var fillQuantity = decimal.Parse(orderDetails.filledQuantity, NumberStyles.Float, CultureInfo.InvariantCulture);
                var updTime = DateTime.UtcNow;
                var security = _securityProvider.GetSecurity(order.Symbol);
                var orderFee = security.FeeModel.GetOrderFee(new OrderFeeParameters(security, order));
                var status = OrderStatus.Filled;

                if (order.Direction == OrderDirection.Sell)
                {
                    fillQuantity = -1 * fillQuantity;
                }

                if (fillQuantity != order.Quantity)
                {
                    decimal totalFillQuantity;
                    _fills.TryGetValue(order.Id, out totalFillQuantity);
                    totalFillQuantity += fillQuantity;
                    _fills[order.Id] = totalFillQuantity;

                    if (totalFillQuantity != order.Quantity)
                    {
                        status = OrderStatus.PartiallyFilled;
                        orderFee = OrderFee.Zero;
                    }
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
                    _pendingOrders.TryRemove(brokerId, out outOrder);
                }

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void EmitOpenInterestTick(Symbol symbol, long openInterest)
        {
            try
            {
                var tick = new Tick
                {
                    TickType = TickType.OpenInterest,
                    Value = openInterest,
                    Exchange = symbol.ID.Market,
                    Symbol = symbol
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

        private void EmitQuoteTick(Symbol symbol, decimal avgPrice, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            try
            {
                var tick = new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    Value = avgPrice,
                    Symbol = symbol,
                    Time = DateTime.UtcNow,
                    Exchange = symbol.ID.Market,
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

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal amount)
        {
            try
            {
                lock (TickLocker)
                {
                    var tick = new Tick
                    {
                        Value = price,
                        Time = time,
                        //Time = DateTime.UtcNow,
                        Symbol = symbol,
                        Exchange = symbol.ID.Market,
                        TickType = TickType.Trade,
                        Quantity = amount,
                        DataType = MarketDataType.Tick,
                        Suspicious = false,
                        EndTime = time
                    };
                    _aggregator.Update(tick);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void FillMonitorAction()
        {
            Log.Trace("SamcoBrokerage.FillMonitorAction(): task started");

            try
            {
                WaitTillConnected();
                foreach (var order in GetOpenOrders())
                {
                    _pendingOrders.TryAdd(order.BrokerId.First(), order);
                }

                while (!_ctsFillMonitor.IsCancellationRequested)
                {
                    try
                    {
                        WaitTillConnected();
                        _fillMonitorResetEvent.WaitOne(TimeSpan.FromMilliseconds(_fillMonitorTimeout), _ctsFillMonitor.Token);

                        foreach (var kvp in _pendingOrders)
                        {
                            var orderId = kvp.Key;
                            var order = kvp.Value;

                            var response = _samcoAPI.GetOrderDetails(orderId);

                            if (response.status != null)
                            {
                                if (response.status.ToUpperInvariant() == "FAILURE")
                                {
                                    OnMessage(new BrokerageMessageEvent(
                                        BrokerageMessageType.Warning,
                                        -1,
                                        $"SamcoBrokerage.FillMonitorAction(): request failed: [{response.status}] {response.statusMessage}, Content: {response.ToString()}, ErrorMessage: {response.validationErrors}"));

                                    continue;
                                }
                            }

                            //Process cancelled orders here.
                            if (response.orderStatus == "CANCELLED")
                            {
                                OnOrderClose(response.orderDetails);
                            }

                            if (response.orderStatus == "EXECUTED")
                            {
                                // Process rest of the orders here.
                                EmitFillOrder(response);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, exception.Message));
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, exception.Message));
            }

            Log.Trace("SamcoBrokerage.FillMonitorAction(): task ended");
        }

        private IEnumerable<Symbol> GetSubscribed()
        {
            return _subscriptionManager.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"SamcoBrokerage.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            _messageHandler.HandleNewMessage(e);
        }

        private void OnMessageImpl(WebSocketMessage webSocketMessage)
        {
            var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;
            try
            {
                var token = JToken.Parse(e.Message);
                if (token is JObject)
                {
                    var raw = token.ToObject<QuoteUpdate>();
                    if (raw.response.streaming_type.ToLowerInvariant() == "quote")
                    {
                        var upd = raw.response.data;
                        var sym = _subscriptionsById[raw.response.data.sym];

                        EmitQuoteTick(sym, upd.avgPr, upd.bPr, upd.bSz, upd.aPr, upd.aSz);

                        if (_lastTradeTickTime != upd.lTrdT)
                        {
                            EmitTradeTick(sym, upd.lTrdT, upd.ltp, upd.ltq);
                            _lastTradeTickTime = upd.lTrdT;
                        }
                        if (upd.oI != "")
                        {
                            EmitOpenInterestTick(sym, Convert.ToInt64(upd.oI, CultureInfo.InvariantCulture));
                        }
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
                        var scrip = _symbolMapper.SamcoSymbols.Where(x => x.Name.ToUpperInvariant() == symbol.ID.Symbol).First();
                        var listingId = scrip.SymbolCode;
                        if (!_unSubscribeInstrumentTokens.Contains(listingId))
                        {
                            sub.request.data.symbols.Add(new Subscription.Symbol { symbol = listingId });
                            _unSubscribeInstrumentTokens.Add(listingId);
                            _subscribeInstrumentTokens.Remove(listingId);
                            Symbol unSubscribeSymbol;
                            _subscriptionsById.TryRemove(listingId, out unSubscribeSymbol);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                        throw;
                    }
                }
                var request = JsonConvert.SerializeObject(sub);
                // required to flush input json as per samco forum
                request = request + "\n";
                WebSocket.Send(request);
                return true;
            }
            return false;
        }

        private void WaitTillConnected()
        {
            while (!IsConnected)
            {
                Thread.Sleep(500);
            }
        }
    }
}
