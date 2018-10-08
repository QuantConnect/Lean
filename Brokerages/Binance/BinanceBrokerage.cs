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
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(BinanceBrokerageFactory))]
    public partial class BinanceBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        /// <summary>
        /// Key Header
        /// </summary>
        public const string KeyHeader = "X-MBX-APIKEY";

        private readonly RateGate _restRateLimiter = new RateGate(10, TimeSpan.FromSeconds(1));
        private readonly string _wssUrl;
        private readonly IAlgorithm _algorithm;
        private readonly ISecurityProvider _securityProvider;
        private readonly BinanceSymbolMapper _symbolMapper = new BinanceSymbolMapper();

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="restUrl">rest api url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        public BinanceBrokerage(string wssUrl, string restUrl, string apiKey, string apiSecret, IAlgorithm algorithm, IPriceProvider priceProvider)
            : base(new RestClient(restUrl), apiKey, apiSecret, Market.Binance, "Binance")
        {
            _algorithm = algorithm;
            _securityProvider = algorithm?.Portfolio;

            _wssUrl = wssUrl;
            WebSocket = new WebSocketWrapper();

            WebSocket.Message += OnMessage;
            WebSocket.Error += OnError;
        }

        #region IBrokerage
        /// <summary>
        /// Checks if the websocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            StartSession();
            WebSocket.Initialize($"{_wssUrl}/stream?streams={SessionId}");

            base.Connect();
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();

            WebSocket?.Close();
            StopSession();
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            return new List<Holding>();
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var list = new List<CashAmount>();
            var queryString = $"timestamp={GetNonce()}";
            var endpoint = $"/api/v3/account?{queryString}&signature={AuthenticationToken(queryString)}";
            var request = new RestRequest(endpoint, Method.GET);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var account = JsonConvert.DeserializeObject<Messages.AccountInformation>(response.Content);
            var balances = account.Balances?.Where(balance => balance.Amount > 0);
            if (balances == null || !balances.Any())
                return list;

            foreach (var balance in balances)
            {
                list.Add(new CashAmount(balance.Amount, balance.Asset.ToUpper()));
            }

            return list;
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();
            var queryString = $"timestamp={GetNonce()}";
            var endpoint = $"/api/v3/openOrders?{queryString}&signature={AuthenticationToken(queryString)}";
            var request = new RestRequest(endpoint, Method.GET);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.GetCashBalance: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var orders = JsonConvert.DeserializeObject<Messages.OpenOrder[]>(response.Content);
            foreach (var item in orders)
            {
                Order order;
                switch (item.Type.ToUpper())
                {
                    case "MARKET":
                        order = new MarketOrder { Price = item.Price };
                        break;
                    case "LIMIT":
                    case "LIMIT_MAKER":
                        order = new LimitOrder { LimitPrice = item.Price };
                        break;
                    case "STOP_LOSS":
                    case "TAKE_PROFIT":
                        order = new StopMarketOrder { StopPrice = item.StopPrice, Price = item.Price };
                        break;
                    case "STOP_LOSS_LIMIT":
                    case "TAKE_PROFIT_LIMIT":
                        order = new StopLimitOrder { StopPrice = item.StopPrice, LimitPrice = item.Price };
                        break;
                    default:
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, (int)response.StatusCode,
                            "BinanceBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                        continue;
                }

                order.Quantity = item.Quantity;
                order.BrokerId = new List<string> { item.Id };
                order.Symbol = _symbolMapper.GetLeanSymbol(item.Symbol);
                order.Time = Time.UnixMillisecondTimeStampToDateTime(item.Time);
                order.Status = ConvertOrderStatus(item.Status);
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

            return list;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public override bool PlaceOrder(Order order)
        {
            LockStream();

            // supported time in force values {GTC, IOC, FOK}
            // use GTC as LEAN doesn't support others yet
            IDictionary<string, object> body = new Dictionary<string, object>()
            {
                { "symbol", _symbolMapper.GetBrokerageSymbol(order.Symbol) },
                { "quantity", Math.Abs(order.Quantity).ToString(CultureInfo.InvariantCulture) },
                { "side", ConvertOrderDirection(order.Direction) }
            };

            decimal ticker = 0, stopPrice = 0m;
            switch (order.Type)
            {
                case OrderType.Limit:
                    body["type"] = (order.Properties as BinanceOrderProperties)?.PostOnly == true
                        ? "LIMIT_MAKER"
                        : "LIMIT";
                    body["price"] = (order as LimitOrder).LimitPrice.ToString(CultureInfo.InvariantCulture);
                    // timeInForce is not required for LIMIT_MAKER
                    if (Equals(body["type"], "LIMIT"))
                        body["timeInForce"] = "GTC";
                    break;
                case OrderType.Market:
                    body["type"] = "MARKET";
                    break;
                case OrderType.StopLimit:
                    ticker = GetTickerPrice(order);
                    stopPrice = (order as StopLimitOrder).StopPrice;
                    if (order.Direction == OrderDirection.Sell)
                        body["type"] = stopPrice <= ticker? "STOP_LOSS_LIMIT" : "TAKE_PROFIT_LIMIT";
                    else
                        body["type"] = stopPrice <= ticker ? "TAKE_PROFIT_LIMIT" : "STOP_LOSS_LIMIT";
                    body["timeInForce"] = "GTC";
                    body["stopPrice"] = stopPrice.ToString(CultureInfo.InvariantCulture);
                    body["price"] = (order as StopLimitOrder).LimitPrice.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new NotSupportedException($"BinanceBrokerage.ConvertOrderType: Unsupported order type: {order.Type}");
            }

            var endpoint = $"/api/v3/order";
            body["timestamp"] = GetNonce();
            body["signature"] = AuthenticationToken(body.ToQueryString());
            var request = new RestRequest(endpoint, Method.POST);
            request.AddHeader(KeyHeader, ApiKey);
            request.AddParameter(
                "application/x-www-form-urlencoded",
                Encoding.UTF8.GetBytes(body.ToQueryString()),
                ParameterType.RequestBody
            );

            var response = ExecuteRestRequest(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var raw = JsonConvert.DeserializeObject<Messages.NewOrder>(response.Content);

                if (string.IsNullOrEmpty(raw?.Id))
                {
                    var errorMessage = $"Error parsing response from place order: {response.Content}";
                    OnOrderEvent(new OrderEvent(
                        order,
                        DateTime.UtcNow,
                        OrderFee.Zero,
                        "Binance Order Event") { Status = OrderStatus.Invalid, Message = errorMessage });
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, (int)response.StatusCode, errorMessage));

                    UnlockStream();
                    return true;
                }

                var brokerId = raw.Id;
                if (CachedOrderIDs.ContainsKey(order.Id))
                {
                    order.BrokerId.Clear();
                    order.BrokerId.Add(brokerId);
                }
                else
                {
                    order.BrokerId.Add(brokerId);
                    CachedOrderIDs.TryAdd(order.Id, order);
                }

                // Generate submitted event
                OnOrderEvent(new OrderEvent(
                    order,
                    Time.UnixMillisecondTimeStampToDateTime(raw.TransactionTime),
                    OrderFee.Zero,
                    "Binance Order Event") { Status = OrderStatus.Submitted }
                );
                Log.Trace($"Order submitted successfully - OrderId: {order.Id}");

                UnlockStream();
                return true;
            }

            var message = $"Order failed, Order Id: {order.Id} timestamp: {order.Time} quantity: {order.Quantity} content: {response.Content}";
            OnOrderEvent(new OrderEvent(
                order,
                DateTime.UtcNow,
                OrderFee.Zero,
                "Binance Order Event") { Status = OrderStatus.Invalid });
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
            throw new NotSupportedException("BinanceBrokerage.UpdateOrder: Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was submitted for cancellation, false otherwise</returns>
        public override bool CancelOrder(Order order)
        {
            LockStream();

            var success = new List<bool>();
            IDictionary<string, object> body = new Dictionary<string, object>()
            {
                { "symbol", _symbolMapper.GetBrokerageSymbol(order.Symbol) }
            };
            foreach (var id in order.BrokerId)
            {
                if (body.ContainsKey("signature"))
                {
                    body.Remove("signature");
                }
                body["orderId"] = id;
                body["timestamp"] = GetNonce();
                body["signature"] = AuthenticationToken(body.ToQueryString());

                var request = new RestRequest("/api/v3/order", Method.DELETE);
                request.AddHeader(KeyHeader, ApiKey);
                request.AddParameter(
                    "application/x-www-form-urlencoded",
                    Encoding.UTF8.GetBytes(body.ToQueryString()),
                    ParameterType.RequestBody
                );

                var response = ExecuteRestRequest(request);
                success.Add(response.StatusCode == HttpStatusCode.OK);
            }

            var canceled = false;
            if (success.All(a => a))
            {
                OnOrderEvent(new OrderEvent(order,
                    DateTime.UtcNow,
                    OrderFee.Zero,
                    "Binance Order Event") { Status = OrderStatus.Canceled });

                canceled = true;
            }
            UnlockStream();
            return canceled;
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(Data.HistoryRequest request)
        {
            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution is not supported, no history returned"));
                yield break;
            }

            string resolution = ConvertResolution(request.Resolution);
            long resolutionInMS = (long)request.Resolution.ToTimeSpan().TotalMilliseconds;
            string symbol = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            long startMTS = (long)Time.DateTimeToUnixTimeStamp(request.StartTimeUtc) * 1000;
            long endMTS = (long)Time.DateTimeToUnixTimeStamp(request.EndTimeUtc) * 1000;
            string endpoint = $"/api/v1/klines?symbol={symbol}&interval={resolution}&limit=1000";

            while ((endMTS - startMTS) > resolutionInMS)
            {
                var timeframe = $"&startTime={startMTS}&endTime={endMTS}";

                var restRequest = new RestRequest(endpoint + timeframe, Method.GET);
                var response = ExecuteRestRequest(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"BinanceBrokerage.GetHistory: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
                }

                var klines = JsonConvert.DeserializeObject<object[][]>(response.Content)
                    .Select(entries => new Messages.Kline(entries))
                    .ToList();

                startMTS = klines.Last().OpenTime + resolutionInMS;
                var period = request.Resolution.ToTimeSpan();

                foreach (var kline in klines)
                {
                    yield return new TradeBar()
                    {
                        Time = Time.UnixMillisecondTimeStampToDateTime(kline.OpenTime),
                        Symbol = request.Symbol,
                        Low = kline.Low,
                        High = kline.High,
                        Open = kline.Open,
                        Close = kline.Close,
                        Volume = kline.Volume,
                        Value = kline.Close,
                        DataType = MarketDataType.TradeBar,
                        Period = period,
                        EndTime = Time.UnixMillisecondTimeStampToDateTime(kline.OpenTime + (long)period.TotalMilliseconds)
                    };
                }
            }
        }

        /// <summary>
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            bool refresh = false;
            foreach (var symbol in symbols)
            {
                if (symbol.Value.Contains("UNIVERSE") ||
                    string.IsNullOrEmpty(_symbolMapper.GetBrokerageSymbol(symbol)) ||
                    symbol.SecurityType != _symbolMapper.GetLeanSecurityType(symbol.Value))
                {
                    continue;
                }

                if (!ChannelList.ContainsKey(symbol.Value))
                {
                    ChannelList.Add(symbol.Value, new Channel()
                    {
                        Name = _symbolMapper.GetBrokerageSymbol(symbol).ToLower(),
                        Symbol = _symbolMapper.GetBrokerageSymbol(symbol)
                    });
                    refresh = true;
                }
            }

            if (refresh)
                ConnectStream();
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

            OnMessageImpl(sender, e);
        }

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected override IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (ChannelList)
            {
                foreach (var ticker in ChannelList.Select(x => x.Value.Symbol).Distinct())
                {
                    list.Add(_symbolMapper.GetLeanSymbol(ticker));
                }
            }
            return list;
        }

        private void ConnectStream()
        {
            if (ChannelList.Count == 0)
                return;

            //close current connection
            WebSocket.Close();
            Wait(() => !WebSocket.IsOpen);

            var streams = ChannelList.Select((pair) => string.Format("{0}@depth/{0}@trade", pair.Value.Name));
            WebSocket.Initialize($"{_wssUrl}/stream?streams={SessionId}/{string.Join("/", streams)}");

            // connect to new endpoing
            Reconnect();

            Log.Trace("BinanceBrokerage.Subscribe: Sent subscribe.");
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _restRateLimiter.Dispose();
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        private void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                foreach (var symbol in symbols)
                {
                    if (ChannelList.ContainsKey(symbol.Value))
                    {
                        ChannelList.Remove(symbol.Value);
                    }
                }

                ConnectStream();
            }
        }
    }
}
