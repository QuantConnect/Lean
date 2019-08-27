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
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance brokerage implementation
    /// </summary>
    [BrokerageFactory(typeof(BinanceBrokerageFactory))]
    public partial class BinanceBrokerage : BaseWebsocketsBrokerage, IDataQueueHandler
    {
        private readonly RateGate _restRateLimiter = new RateGate(10, TimeSpan.FromSeconds(1));
        private readonly string _wssUrl;
        private readonly IAlgorithm _algorithm;
        private readonly BinanceSymbolMapper _symbolMapper = new BinanceSymbolMapper();
        private readonly IWebSocket TickerWebSocket;
        private readonly TimeSpan _subscribeDelay = TimeSpan.FromMilliseconds(250);
        private HashSet<Symbol> SubscribedSymbols = new HashSet<Symbol>();
        private RealTimeSynchronizedTimer _keepAliveTimer;
        private RealTimeSynchronizedTimer _reconnectTimer;
        private readonly object _lockerSubscriptions = new object();
        private DateTime _lastSubscribeRequestUtcTime = DateTime.MinValue;
        private bool _subscriptionsPending;
        private readonly BinanceRestApiClient _apiClient;

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

            _wssUrl = wssUrl;
            _apiClient = new BinanceRestApiClient(
                _symbolMapper,
                algorithm?.Portfolio,
                restUrl,
                apiKey,
                apiSecret);

            WebSocket = new WebSocketWrapper();

            WebSocket.Message += OnMessage;
            WebSocket.Error += OnError;
            WebSocket.Open += (s, e) =>
            {
                _keepAliveTimer = new RealTimeSynchronizedTimer(TimeSpan.FromMinutes(30), (d) => _apiClient.SessionKeepAlive());
                _keepAliveTimer.Start();
            };
            WebSocket.Closed += (s, e) => { _keepAliveTimer.Stop(); };
            WebSocket.Message += (s, e) => OnSocketMessage(s, e, OnUserMessageImpl);

            var tickerConnectionHandler = new DefaultConnectionHandler();
            tickerConnectionHandler.ReconnectRequested += (s, e) => { ProcessSubscriptionRequest(); };
            TickerWebSocket = new BinanceWebSocketWrapper(
                tickerConnectionHandler
            );

            TickerWebSocket.Message += (s, e) => OnSocketMessage(s, e, OnStreamMessageImpl);
            TickerWebSocket.Message += (s, e) => (s as BinanceWebSocketWrapper)?.ConnectionHandler.KeepAlive(DateTime.UtcNow);
            TickerWebSocket.Error += OnError;

            _reconnectTimer = new RealTimeSynchronizedTimer(TimeSpan.FromHours(12), (d) =>
            {
                Reconnect();
                ProcessSubscriptionRequest();
            });
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
            _apiClient.CreateListenKey();
            _reconnectTimer.Start();

            WebSocket.Initialize($"{_wssUrl}/stream?streams={_apiClient.SessionId}");

            base.Connect();
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();
            _reconnectTimer.Stop();

            WebSocket?.Close();
            _apiClient.StopSession();

            (TickerWebSocket as BinanceWebSocketWrapper)?.ConnectionHandler.DisposeSafely();
            if (TickerWebSocket.IsOpen)
            {
                TickerWebSocket.Close();
            }
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            return _apiClient.GetAccountHoldings();
        }

        /// <summary>
        /// Gets the total account cash balance for specified account type
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var account = _apiClient.GetCashBalance();
            var balances = account.Balances?.Where(balance => balance.Amount > 0);
            if (balances == null || !balances.Any())
                return new List<CashAmount>();

            return balances
                .Select(b => new CashAmount(b.Amount, b.Asset.LazyToUpper()))
                .ToList();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var orders = _apiClient.GetOpenOrders();
            List<Order> list = new List<Order>();
            foreach (var item in orders)
            {
                Order order;
                switch (item.Type.LazyToUpper())
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
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1,
                            "BinanceBrokerage.GetOpenOrders: Unsupported order type returned from brokerage: " + item.Type));
                        continue;
                }

                order.Quantity = item.Quantity;
                order.BrokerId = new List<string> { item.Id };
                order.Symbol = _symbolMapper.GetLeanSymbol(item.Symbol);
                order.Time = Time.UnixMillisecondTimeStampToDateTime(item.Time);
                order.Status = ConvertOrderStatus(item.Status);
                order.Price = item.Price;

                if (order.Status.IsOpen())
                {
                    var cached = CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(order.BrokerId.First()));
                    if (cached.Any())
                    {
                        CachedOrderIDs[cached.First().Key] = order;
                    }
                }

                list.Add(order);
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
            bool submitted = false;
            WithLockedStream(() =>
            {
                submitted = _apiClient.PlaceOrder(order);
            });
            return submitted;
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
            bool submitted = false;
            WithLockedStream(() =>
            {
                submitted = _apiClient.CancelOrder(order);
            });
            return submitted;
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

            var period = request.Resolution.ToTimeSpan();

            foreach (var kline in _apiClient.GetHistory(request))
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

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, WebSocketMessage e)
        {
            LastHeartbeatUtcTime = DateTime.UtcNow;
        }

        private void OnSocketMessage(object sender, WebSocketMessage e, Action<Messages.BaseMessage> handler)
        {
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

            OnMessageImpl(sender, e, handler);
        }

        /// <summary>
        /// Force reconnect
        /// </summary>
        protected override void Reconnect()
        {
            if (WebSocket.IsOpen)
            {
                WebSocket?.Close();
            }
            base.Reconnect();
        }

        private void ProcessSubscriptionRequest()
        {
            if (_subscriptionsPending) return;

            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
            _subscriptionsPending = true;

            Task.Run(async () =>
            {
                while (true)
                {
                    DateTime requestTime;
                    List<Symbol> symbolsToSubscribe;
                    lock (_lockerSubscriptions)
                    {
                        requestTime = _lastSubscribeRequestUtcTime.Add(_subscribeDelay);
                        symbolsToSubscribe = SubscribedSymbols.ToList();
                    }

                    if (DateTime.UtcNow > requestTime)
                    {
                        // restart streaming session
                        SubscribeSymbols(symbolsToSubscribe);

                        lock (_lockerSubscriptions)
                        {
                            _lastSubscribeRequestUtcTime = DateTime.UtcNow;
                            if (SubscribedSymbols.Count == symbolsToSubscribe.Count)
                            {
                                // no more subscriptions pending, task finished
                                _subscriptionsPending = false;
                                break;
                            }
                        }
                    }

                    await Task.Delay(200).ConfigureAwait(false);
                }
            });
        }

        private void SubscribeSymbols(List<Symbol> symbolsToSubscribe)
        {
            if (symbolsToSubscribe.Count == 0)
                return;

            //close current connection
            if (TickerWebSocket.IsOpen)
            {
                TickerWebSocket.Close();
            }
            Wait(() => !TickerWebSocket.IsOpen);

            var streams = symbolsToSubscribe.Select((s) => string.Format(CultureInfo.InvariantCulture, "{0}@depth/{0}@trade", s.Value.LazyToLower()));
            TickerWebSocket.Initialize($"{_wssUrl}/stream?streams={string.Join("/", streams)}");

            Log.Trace($"BaseWebsocketsBrokerage(): Reconnecting... IsConnected: {IsConnected}");

            TickerWebSocket.Error -= this.OnError;
            try
            {
                //try to clean up state
                if (TickerWebSocket.IsOpen)
                {
                    TickerWebSocket.Close();
                    Wait(() => !TickerWebSocket.IsOpen);
                }
                if (!TickerWebSocket.IsOpen)
                {
                    TickerWebSocket.Connect();
                    Wait(() => TickerWebSocket.IsOpen);
                }
            }
            finally
            {
                TickerWebSocket.Error += this.OnError;
                this.Subscribe(symbolsToSubscribe);
            }

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
        /// Subscribes to the requested symbols (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe</param>
        public override void Subscribe(IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                List<Symbol> symbolsToSubscribe = new List<Symbol>();
                foreach (var symbol in symbols)
                {
                    if (symbol.Value.Contains("UNIVERSE") ||
                        string.IsNullOrEmpty(_symbolMapper.GetBrokerageSymbol(symbol)) ||
                        symbol.SecurityType != _symbolMapper.GetLeanSecurityType(symbol.Value) ||
                        SubscribedSymbols.Contains(symbol))
                    {
                        continue;
                    }

                    symbolsToSubscribe.Add(symbol);
                }

                if (symbolsToSubscribe.Count == 0)
                    return;

                Log.Trace("BinanceBrokerage.Subscribe(): {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));

                SubscribedSymbols = symbolsToSubscribe
                    .Union(SubscribedSymbols.ToList())
                    .ToList()
                    .ToHashSet();

                ProcessSubscriptionRequest();
            }
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        private void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                if (WebSocket.IsOpen)
                {
                    var symbolsToUnsubscribe = (from symbol in symbols
                                                where SubscribedSymbols.Contains(symbol)
                                                select symbol).ToList();
                    if (symbolsToUnsubscribe.Count == 0)
                        return;

                    Log.Trace("BinanceBrokerage.Unsubscribe(): {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));

                    SubscribedSymbols = SubscribedSymbols
                        .ToList()
                        .Where(x => !symbolsToUnsubscribe.Contains(x))
                        .ToHashSet();

                    ProcessSubscriptionRequest();
                }
            }
        }
    }
}
