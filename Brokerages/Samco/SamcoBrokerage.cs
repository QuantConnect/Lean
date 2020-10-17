using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using QuantConnect.Brokerages.Samco.Messages;
using NodaTime;
using QuantConnect.Orders.Fees;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.Samco
{
    /// <summary>
    /// Samco Brokerage implementation
    /// </summary>
    public partial class SamcoBrokerage : BaseWebsocketsBrokerage, IDataQueueUniverseProvider, IHistoryProvider
    {
        private readonly IAlgorithm _algorithm;
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private SamcoSubscriptionManager _subscriptionManager;
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();

        /// <summary>
        /// The rest client instance
        /// </summary>
        protected IRestClient RestClient;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        private readonly SamcoSymbolMapper _symbolMapper = new SamcoSymbolMapper();
        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();

        /// <summary>
        /// The list of queued ticks
        /// </summary>
        public List<Tick> Ticks = new List<Tick>();
        private SamcoBrokerageAPI _samcoAPI;
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _yob;
        private readonly string _wssUrl;
      

        public string sessionToken;
        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="restUrl">rest api url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="yob">year of birth</param>
        public SamcoBrokerage(string wssUrl, string restUrl, string apiKey, string apiSecret, string yob, IAlgorithm algorithm, IPriceProvider priceProvider)
            : this(wssUrl, new SamcoWebSocketClientWrapper(), new RestClient(restUrl), apiKey, apiSecret, yob, algorithm)
        {
            Log.Trace("Start Samco Brokerage");
        }

       
        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="yob">year of birth</param>
        public SamcoBrokerage(string wssUrl, SamcoWebSocketClientWrapper websocket, IRestClient restClient, string apiKey, string apiSecret, string yob, IAlgorithm algorithm)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, "Samco")
        {
            RestClient = restClient;
            _algorithm = algorithm;
            _samcoAPI = new SamcoBrokerageAPI(restClient);
            _samcoAPI.Authorize(apiKey, apiSecret, yob);
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _yob = yob;
            _wssUrl = wssUrl;
            sessionToken = _samcoAPI.token;
            websocket = new SamcoWebSocketClientWrapper();
            websocket.Initialize(_wssUrl);
            websocket.SetAuthTokenHeader(sessionToken);
            //WebSocket = websocket;
            _subscriptionManager = new SamcoSubscriptionManager(this, _wssUrl, _symbolMapper, sessionToken);
            Log.Trace("Start Samco Brokerage");
        }

        /// <summary>
        /// Gets Quote using Samco API
        /// </summary>
        /// <returns> Quote Response</returns>
        public QuoteResponse GetQuote(Symbol symbol)
        {
            return _samcoAPI.GetQuote(symbol);
        }

        /// <summary>
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (_subscriptionManager.IsSubscribed(symbol) ||
                    symbol.Value.Contains("UNIVERSE") ||
                    !_symbolMapper.IsKnownBrokerageSymbol(symbol.Value) ||
                    symbol.SecurityType != _symbolMapper.GetLeanSecurityType(symbol.Value))
                {
                    continue;
                }

                _subscriptionManager.Subscribe(symbol);

                Log.Trace($"SamcoBrokerage.Subscribe(): Sent subscribe for {symbol.Value}.");
            }
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                _subscriptionManager.Unsubscribe(symbol);

                Log.Trace($"SamcoBrokerage.Unsubscribe(): Sent unsubscribe for {symbol.Value}.");
            }
        }

        private void OnOrderClose(string[] entries)
        {
            var brokerId = entries[0];
            if (entries[5].IndexOf("canceled", StringComparison.OrdinalIgnoreCase) >= 0)
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

        private void EmitFillOrder(string[] entries)
        {
            try
            {
                var brokerId = entries[4];
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

                var symbol = _symbolMapper.GetLeanSymbol(entries[2]);
                var fillPrice = decimal.Parse(entries[6], NumberStyles.Float, CultureInfo.InvariantCulture);
                var fillQuantity = decimal.Parse(entries[5], NumberStyles.Float, CultureInfo.InvariantCulture);
                var direction = fillQuantity < 0 ? OrderDirection.Sell : OrderDirection.Buy;
                var updTime = Time.UnixTimeStampToDateTime(double.Parse(entries[3], NumberStyles.Float, CultureInfo.InvariantCulture));
                var orderFee = new OrderFee(new CashAmount(
                        Math.Abs(decimal.Parse(entries[9], NumberStyles.Float, CultureInfo.InvariantCulture)),
                        entries[10]
                    ));

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
                    orderFee, $"Samco Order Event {direction}"
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
            Log.Trace("SamcoBrokerage.Messaging.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        public void UnlockStream()
        {
            Log.Trace("SamcoBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;
        /// <summary>
        /// Connects to samco wss
        /// </summary>
        public override void Connect()
        {
            base.Connect();
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
            Messages.OrderResponse orderResponse = null;
            //if (order.Type == OrderType.Bracket)
            //{
            //    orderResponse = _samcoAPI.PlaceBracketOrder(order, _algorithm);
            //}
            //else
            //{
                orderResponse = _samcoAPI.PlaceOrder(order, _algorithm);
            //}
            Log.Debug("SamcoOrderResponse:");
            Log.Debug(orderResponse.ToString());

            var orderFee = OrderFee.Zero;

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
            var orderFee = OrderFee.Zero;
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
            Messages.OrderResponse orderResponse = null;
            //if (order.Type == OrderType.Bracket)
            //{
            //    orderResponse = _samcoAPI.CancelBracketOrder(order.Id.ToStringInvariant());
            //}
            //else
            //{
                orderResponse = _samcoAPI.CancelOrder(order.Id.ToStringInvariant());
            //}
            if (orderResponse.status == "Success")
            {
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
            //(int orderId, OrderType type, Symbol symbol, decimal quantity, DateTime time,
            // string tag, IOrderProperties properties, decimal limitPrice, decimal stopPrice)

            List<Order> list = new List<Order>();

            //Only loop if there are any actual orders inside response
            if (allOrders.status != "Failure" && allOrders.orderBookDetails.Count > 0)
            {

                //TODO:Debug and find out the actual statuses coming from API

                foreach (var item in allOrders.orderBookDetails.Where(z => z.orderStatus == "filled"))
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
                    order.Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol);
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

        private OrderStatus ConvertOrderStatus(Messages.OrderDetails orderDetails)
        {
            var filledQty = Convert.ToInt32(orderDetails.filledQuantity, CultureInfo.InvariantCulture);
            var pendingQty = Convert.ToInt32(orderDetails.pendingQuantity, CultureInfo.InvariantCulture);
            var orderDetail = _samcoAPI.GetOrderDetails(orderDetails.orderNumber);
            if (orderDetails.orderStatus != "complete" && filledQty == 0)
            {
                return Orders.OrderStatus.Submitted;
            }
            else if (filledQty > 0 && pendingQty > 0)
            {
                return Orders.OrderStatus.PartiallyFilled;
            }
            else if (pendingQty == 0)
            {
                return Orders.OrderStatus.Filled;
            }
            else if (orderDetail.orderStatus.ToUpperInvariant() == "CANCELLED")
            {
                return Orders.OrderStatus.Canceled;
            }

            return Orders.OrderStatus.None;
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var holdingsList = new List<Holding>();
            var HoldingResponse = _samcoAPI.GetHoldings();
            if (HoldingResponse.holdingDetails == null)
            {
                return holdingsList;
            }
            foreach (var item in HoldingResponse.holdingDetails)
            {
                //(avgprice - lasttradedprice) * holdingsqty
                Holding holding = new Holding
                {
                    AveragePrice = item.averagePrice,
                    Symbol = _symbolMapper.GetLeanSymbol(item.tradingSymbol),
                    MarketPrice = item.lastTradedPrice,
                    Quantity = item.holdingsQuantity,
                    Type = SecurityType.Equity,
                    UnrealizedPnL = (item.averagePrice - item.lastTradedPrice) * item.holdingsQuantity,
                    CurrencySymbol = Currencies.GetCurrencySymbol("INR"),
                    //TODO:Can be item.holdings value too, need to debug
                    MarketValue = item.lastTradedPrice * item.holdingsQuantity
                    ////Other field to populate 

                };
                holdingsList.Add(holding);
            }
            return holdingsList;
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var amt = new decimal();
            var list = new List<CashAmount>();
            var response = _samcoAPI.GetUserLimits();
            if (response != null)
            {
                amt = Convert.ToDecimal(response.EquityLimit.NetAvailableMargin, CultureInfo.InvariantCulture);
                list.Add(new CashAmount(amt, Currencies.USD));
            }
            return list;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            if (request.Symbol.SecurityType != SecurityType.Equity)
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
            if (request.EndTimeUtc.Ticks % request.Resolution.ToTimeSpan().Ticks > 0)
            {
                // give a warning and return
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidEndTime",
                    "The history request's end date is not a full multiple of a resolution. " +
                    "Samco API only allows to support trade bar history requests. The start and end dates " +
                    "of a such request are expected to match exactly with the beginning of the first bar and ending of the last"));
                yield break;
            }

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

        #endregion

        #region IDataQueueHandler
        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (TickLocker)
            {
                var copy = Ticks.ToArray();
                Ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Subscribe(symbols);
        }


        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            Unsubscribe(symbols);
        }

        #endregion

        #region IHistoryProvider implementation

        /// <summary>
        /// Event fired when an invalid configuration has been detected
        /// </summary>
        public event EventHandler<InvalidConfigurationDetectedEventArgs> InvalidConfigurationDetected;

        /// <summary>
        /// Event fired when the numerical precision in the factor file has been limited
        /// </summary>
        public event EventHandler<NumericalPrecisionLimitedEventArgs> NumericalPrecisionLimited;

        /// <summary>
        /// Event fired when there was an error downloading a remote file
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs> DownloadFailed;

        /// <summary>
        /// Event fired when there was an error reading the data
        /// </summary>
        public event EventHandler<ReaderErrorDetectedEventArgs> ReaderErrorDetected;

        /// <summary>
        /// Event fired when the start date has been limited
        /// </summary>
        public event EventHandler<StartDateLimitedEventArgs> StartDateLimited;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount { get; private set; }
        public DateTime LastHeartbeatUtcTime { get; private set; }



        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public void Initialize(HistoryProviderInitializeParameters parameters)
        {
            Log.Trace("Init Samco Intraday History Provider");
        }

        /// <summary>
        /// Event invocator for the <see cref="InvalidConfigurationDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="InvalidConfigurationDetected"/> event</param>
        protected virtual void OnInvalidConfigurationDetected(InvalidConfigurationDetectedEventArgs e)
        {
            InvalidConfigurationDetected?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="NumericalPrecisionLimited"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="NumericalPrecisionLimited"/> event</param>
        protected virtual void OnNumericalPrecisionLimited(NumericalPrecisionLimitedEventArgs e)
        {
            NumericalPrecisionLimited?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="DownloadFailed"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="DownloadFailed"/> event</param>
        protected virtual void OnDownloadFailed(DownloadFailedEventArgs e)
        {
            DownloadFailed?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="ReaderErrorDetected"/> event
        /// </summary>
        /// <param name="e">Event arguments for the <see cref="ReaderErrorDetected"/> event</param>
        protected virtual void OnReaderErrorDetected(ReaderErrorDetectedEventArgs e)
        {
            ReaderErrorDetected?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the Message event
        /// </summary>
        /// <param name="e">The error</param>
        public new void OnMessage(BrokerageMessageEvent e)
        {
            base.OnMessage(e);
        }


        public IEnumerable<Slice> GetHistory(IEnumerable<Data.HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {

            foreach (var request in requests)
            {
                if (request.Symbol.ID.SecurityType != SecurityType.Equity)
                {
                    throw new ArgumentException("Invalid security type: " + request.Symbol.ID.SecurityType);
                }

                if (request.StartTimeUtc >= request.EndTimeUtc)
                {
                    throw new ArgumentException("Invalid date range specified");
                }

                var start = request.StartTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);
                var end = request.EndTimeUtc.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);

                var history = Enumerable.Empty<Slice>();


                switch (request.Resolution)
                {
                    case Resolution.Tick:
                        history = GetHistoryTick(request.Symbol, start, end);
                        break;

                    case Resolution.Second:
                        history = GetHistorySecond(request.Symbol, start, end);
                        break;

                    case Resolution.Minute:
                        history = GetHistoryMinute(request.Symbol, start, end);
                        break;

                    case Resolution.Hour:
                        history = GetHistoryHour(request.Symbol, start, end);
                        break;

                    case Resolution.Daily:
                        history = GetHistoryDaily(request.Symbol, start, end);
                        break;
                }

                foreach (var slice in history)
                {
                    yield return slice;
                }
            }
        }

        private IEnumerable<Slice> GetHistoryDaily(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Samco only supports minute resolution");
        }

        private IEnumerable<Slice> GetHistoryHour(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Samco only supports minute resolution");
        }

        private IEnumerable<Slice> GetHistoryMinute(Symbol symbol, DateTime start, DateTime end)
        {
            Log.Debug("SamcoBrokerage.GetHistoryMinute();");
            string scripSymbol = _symbolMapper.GetBrokerageSymbol(symbol);
            var startDateTime = start.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var endDateTime = end.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
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


            //if (!candles.intradayCandleData.Any())
            //{
            //    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "NoHistoricalData",
            //        $"Exchange returned no data for {symbol} on history request " +
            //        $"from {start:s} to {end:s}"));
            //    //yield break;
            //}

            //var history = List

            //foreach (var candle in candles.intradayCandleData)
            //{
            //      new TradeBar()
            //    {
            //        Time = candle.dateTime,
            //        Symbol = symbol,
            //        Low = candle.low,
            //        High = candle.high,
            //        Open = candle.open,
            //        Close = candle.close,
            //        Volume = candle.volume,
            //        Value = candle.close,
            //        DataType = MarketDataType.TradeBar,
            //        Period = Time.OneMinute,
            //        EndTime = candle.dateTime.AddMinutes(1)
            //    };
            //}


            if (candles.intradayCandleData == null)
                return Enumerable.Empty<Slice>();

            DataPointCount += candles.intradayCandleData.Count;

            return candles.intradayCandleData
                .Select(bar => new TradeBar(bar.dateTime, symbol, bar.open, bar.high, bar.low, bar.close, bar.volume, Time.OneMinute))
                .Select(tradeBar => new Slice(tradeBar.EndTime, new[] { tradeBar }));
        }

        private IEnumerable<Slice> GetHistorySecond(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Samco only supports minute resolution");
        }

        private IEnumerable<Slice> GetHistoryTick(Symbol symbol, DateTime start, DateTime end)
        {
            throw new ArgumentException("Samco only supports minute resolution");
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
        }

        public override void OnMessage(object sender, WebSocketMessage e)
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
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Information, -1, $"Parsing new wss message. Data: {e.Message}"));
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }
        #endregion

        #region IDataQueueUniverseProvider

        /// <summary>
        /// Method returns a collection of Symbols that are available at the broker.
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, bool includeExpired, string securityCurrency = null, string securityExchange = null)
        {
            Func<Symbol, string> lookupFunc;

            switch (securityType)
            {
                case SecurityType.Option:
                    // for option, futures contract we search the underlying
                    lookupFunc = symbol => symbol.HasUnderlying ? symbol.Underlying.Value : string.Empty;
                    break;
                case SecurityType.Future:
                    lookupFunc = symbol => symbol.ID.Symbol;
                    break;
                default:
                    lookupFunc = symbol => symbol.Value;
                    break;
            }

            var result = SamcoSymbolMapper.KnownSymbols.Where(x => lookupFunc(x) == lookupName &&
                                            x.ID.SecurityType == securityType &&
                                            (securityExchange == null || x.ID.Market == securityExchange))
                                         .ToList();

            return result.Select(x => x);
        }

        /// <summary>
        /// Returns whether the time can be advanced or not.
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>true if the time can be advanced</returns>
        public bool CanAdvanceTime(SecurityType securityType)
        {
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Represents a subscription channel
    /// </summary>
    public class Channel
    {

        /// <summary>
        /// Represents channel identifier for specific subscription
        /// </summary>
        public string ChannelId { get; set; }
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The ticker symbol of the channel
        /// </summary>
        public string Symbol { get; set; }
    }
}
