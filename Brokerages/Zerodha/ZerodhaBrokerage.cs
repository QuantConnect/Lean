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
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;
using QuantConnect.Orders.Fees;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Brokerages.Zerodha.Messages;
using Newtonsoft.Json.Linq;
using Tick = QuantConnect.Data.Market.Tick;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Zerodha Brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(ZerodhaBrokerageFactory))]
    public partial class ZerodhaBrokerage : Brokerage, IDataQueueHandler,IDataQueueUniverseProvider, IHistoryProvider
    {
        #region Declarations
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
        public ConcurrentDictionary<int, Orders.Order> CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();
        /// <summary>
        /// A list of currently subscribed channels
        /// </summary>
        protected Dictionary<string, Channel> ChannelList = new Dictionary<string, Channel>();
        private string _market { get; set; }
        /// <summary>
        /// The api secret
        /// </summary>
        protected string ApiSecret;
        /// <summary>
        /// The api key
        /// </summary>
        protected string ApiKey;
        /// <summary>
        /// Timestamp of most recent heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime = DateTime.UtcNow;
        private const int _heartbeatTimeout = 90;
        private Thread _connectionMonitorThread;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockerConnectionMonitor = new object();
        private volatile bool _connectionLost;
        private const int _connectionTimeout = 30000;
        private readonly IAlgorithm _algorithm;
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<int, decimal> _fills = new ConcurrentDictionary<int, decimal>();
        private ZerodhaSubscriptionManager _subscriptionManager;
        private readonly ConcurrentQueue<MessageData> _messageBuffer = new ConcurrentQueue<MessageData>();

        private readonly IDataAggregator _aggregator;
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;
        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        public readonly object TickLocker = new object();

        private readonly ZerodhaSymbolMapper _symbolMapper = new ZerodhaSymbolMapper();

        private Kite _kite;
        private readonly IOrderProvider _orderProvider;
        private readonly ISecurityProvider _securityProvider;
        private readonly string _apiKey;
        private readonly string _accessToken;
        private readonly string _wssUrl = "wss://ws.kite.trade/";
        public string sessionToken;
        #endregion


        
        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        public ZerodhaBrokerage(string apiKey, string apiSecret, string accessToken, IAlgorithm algorithm, IDataAggregator aggregator)
            : base("Zerodha")
        {
            _algorithm = algorithm;
            _aggregator = aggregator;
            _kite = new Kite(apiKey);
            _apiKey = apiKey;
            //var user = _kite.GenerateSession(requestToken,apiSecret);
            _accessToken = accessToken;
            var websocket = new ZerodhaWebSocketClientWrapper();
            _wssUrl += string.Format(CultureInfo.InvariantCulture,"?api_key={0}&access_token={1}", _apiKey, _accessToken);
            websocket.Initialize(_wssUrl);
            WebSocket = websocket;
            WebSocket.Message += OnMessage;
            WebSocket.Error += OnError;
            _subscriptionManager = new ZerodhaSubscriptionManager(this, _wssUrl, _symbolMapper, sessionToken);
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            Log.Trace("Start Zerodha Brokerage");
        }

        /// <summary>
        /// Gets Quote using Zerodha API
        /// </summary>
        /// <returns> Quote</returns>
        public Quote GetQuote(Symbol symbol)
        {
            var instrumentIds = new string[] { symbol.ID.Symbol };
            var quotes = _kite.GetQuote(instrumentIds);
            return quotes[symbol.ID.Symbol];
        }

        /// <summary>
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
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
                Log.Trace($"ZerodhaBrokerage.Subscribe(): Sent subscribe for {symbol.Value}.");
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
                Log.Trace($"ZerodhaBrokerage.Unsubscribe(): Sent unsubscribe for {symbol.Value}.");
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
                Orders.Order outOrder;
                if (CachedOrderIDs.TryRemove(order.Id, out outOrder))
                {
                    OnOrderEvent(new OrderEvent(order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Zerodha Order Event")
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
                    orderFee, $"Zerodha Order Event {direction}"
                );

                // if the order is closed, we no longer need it in the active order list
                if (status == OrderStatus.Filled)
                {
                    Orders.Order outOrder;
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
            WebSocket.Connect();
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
        public override bool PlaceOrder(Orders.Order order)
        {
            LockStream();
            Dictionary<string,dynamic> orderResponse;

            //if (order.Type == OrderType.Bracket)
            //{
            //    orderResponse = _kite.PlaceOrder(order.Symbol.ID.Market, order.Symbol.ID.Symbol,"",order.Quantity.ConvertInvariant<int>());
            //}
            //else
            //{
                orderResponse = _kite.PlaceOrder(order.Symbol.ID.Market, order.Symbol.ID.Symbol, "", order.Quantity.ConvertInvariant<int>());
            //}
            Log.Debug("ZerodhaOrderResponse:");
            Log.Debug(orderResponse.ToString());

            var orderFee = OrderFee.Zero;
            if (orderResponse["status"] == "error")
            {
                var errorMessage = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse["message"]}";
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, errorMessage));

                UnlockStream();
                return true;
            }

            if (orderResponse["status"] == "success")
            {

                if (string.IsNullOrEmpty(orderResponse["order_id"]))
                {
                    var errorMessage = $"Error parsing response from place order: {orderResponse["status_message"]}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse["status_message"], errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = orderResponse["order_id"];
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
            return true;

        }

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            LockStream();
            var orderResponse = _kite.ModifyOrder(order.Id.ToStringInvariant());
            var orderFee = OrderFee.Zero;
            if (orderResponse["status"] == "success")
            {
                if (string.IsNullOrEmpty(orderResponse["order_id"]))
                {
                    var errorMessage = $"Error parsing response from place order: {orderResponse["status_message"]}";
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, orderResponse["status"], errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = orderResponse["order_id"];
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
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.UpdateSubmitted });
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {orderResponse["status_message"]}";
            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, orderFee, "Zerodha Order Event") { Status = OrderStatus.Invalid });
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, message));

            UnlockStream();
            return true;
        }

        public void EmitTick(Tick tick)
        {
            _aggregator.Update(tick);
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Orders.Order order)
        {
            LockStream();
            Dictionary<string, dynamic> orderResponse = null;
            //if (order.Type == OrderType.Bracket)
            //{
            //    orderResponse = _kite.CancelOrder(order.Id.ToStringInvariant());
            //}
            //else
            //{
                orderResponse = _kite.CancelOrder(order.Id.ToStringInvariant());
            //}
            if (orderResponse["status"] == "Success")
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
        public override List<Orders.Order> GetOpenOrders()
        {
            var allOrders = _kite.GetOrders();
            List<Orders.Order> list = new List<Orders.Order>();
            //Only loop if there are any actual orders inside response
            if (allOrders.Count > 0)
            {

                foreach (var item in allOrders.Where(z => z.Status == "filled"))
                {

                    Orders.Order order;
                    if (item.OrderType.ToLowerInvariant() == "MKT")
                    {
                        order = new MarketOrder { Price = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else if (item.OrderType.ToLowerInvariant() == "L")
                    {
                        order = new LimitOrder { LimitPrice = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else if (item.OrderType.ToLowerInvariant() == "SL-M")
                    {
                        order = new StopMarketOrder { StopPrice = Convert.ToDecimal(item.Price, CultureInfo.InvariantCulture) };
                    }
                    else
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error,"UnKnownOrderType",
                            "ZerodhaBrorage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.OrderType));
                        continue;
                    }

                    var itemTotalQty = item.Quantity;
                    var originalQty = item.Quantity;
                    order.Quantity = item.TransactionType.ToLowerInvariant() == "sell" ? -itemTotalQty : originalQty;
                    order.BrokerId = new List<string> { item.OrderId };
                    order.Symbol = _symbolMapper.GetLeanSymbol(item.InstrumentToken.ToStringInvariant());
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
            if (orderDetails.Status != "complete" && filledQty == 0)
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
            else if (orderDetails.Status.ToUpperInvariant() == "CANCELLED")
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
            var HoldingResponse = _kite.GetHoldings();
            if (HoldingResponse == null)
            {
                return holdingsList;
            }
            foreach (var item in HoldingResponse)
            {
                //(avgprice - lasttradedprice) * holdingsqty
                Holding holding = new Holding
                {
                    AveragePrice = item.AveragePrice,
                    Symbol = _symbolMapper.GetLeanSymbol(item.TradingSymbol),
                    MarketPrice = item.LastPrice,
                    Quantity = item.Quantity,
                    Type = SecurityType.Equity,
                    UnrealizedPnL = item.PNL, //(item.averagePrice - item.lastTradedPrice) * item.holdingsQuantity,
                    CurrencySymbol = Currencies.GetCurrencySymbol(AccountBaseCurrency),
                    //TODO:Can be item.holdings value too, need to debug
                    MarketValue = item.ClosePrice * item.Quantity

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
            var list = new List<CashAmount>();
            var response = _kite.GetMargins();
            decimal amt = Convert.ToDecimal(response.Equity.Available, CultureInfo.InvariantCulture);
            list.Add(new CashAmount(amt, AccountBaseCurrency));
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
                    "Zerodha API only allows to support trade bar history requests. The start and end dates " +
                    "of a such request are expected to match exactly with the beginning of the first bar and ending of the last"));
                yield break;
            }

            if (request.Resolution != Resolution.Minute || request.Resolution != Resolution.Hour || request.Resolution != Resolution.Daily)
            {
                throw new ArgumentException($"ZerodhaBrokerage.ConvertResolution: Unsupported resolution type: {request.Resolution}");
            }

            DateTime latestTime = request.StartTimeUtc;
            var requests = new List<Data.HistoryRequest>();
            requests.Add(request);
            do
            {
                var candles = GetHistory(requests, TimeZones.Kolkata);
                yield break;

            } while (latestTime < request.EndTimeUtc);
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

        /// <summary>
        /// The ticker symbol security type of the channel
        /// </summary>
        public SecurityType SecurityType { get;  set; }
    }
}
