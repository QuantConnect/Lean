using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Samco.Messages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Samco
{
    public class SamcoSubscriptionManager
    {
        private readonly string _wssUrl;
        private readonly object _locker = new object();
        private readonly SamcoBrokerage _brokerage;
        private readonly SamcoSymbolMapper _symbolMapper;
        private string _wssAuthToken;
        private const int MaximumSubscriptionsPerSocket = 30;
        private ConcurrentDictionary<string, Symbol> _subscriptionsById = new ConcurrentDictionary<string, Symbol>();
        private RateGate _connectionRateLimiter = new RateGate(30, TimeSpan.FromMinutes(1));
        private ConcurrentDictionary<Symbol, List<SamcoWebSocketWrapper>> _subscriptionsBySymbol = new ConcurrentDictionary<Symbol, List<SamcoWebSocketWrapper>>();
        private ConcurrentDictionary<SamcoWebSocketWrapper, List<SamcoChannel>> _channelsByWebSocket = new ConcurrentDictionary<SamcoWebSocketWrapper, List<SamcoChannel>>();
        private ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new ConcurrentDictionary<Symbol, DefaultOrderBook>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SamcoSubscriptionManager"/> class.
        /// </summary>
        public SamcoSubscriptionManager(SamcoBrokerage brokerage, string wssUrl, SamcoSymbolMapper symbolMapper, string wssAuthToken)
        {
            _brokerage = brokerage;
            _wssUrl = wssUrl;
            _symbolMapper = symbolMapper;
            _wssAuthToken = wssAuthToken;
        }

        /// <summary>
        /// Returns true if there is an active subscription for the requested symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool IsSubscribed(Symbol symbol)
        {
            return _subscriptionsBySymbol.ContainsKey(symbol);
        }

        private SamcoWebSocketWrapper GetFreeWebSocket(SamcoChannel channel)
        {
            int count;

            lock (_locker)
            {
                foreach (var kvp in _channelsByWebSocket)
                {
                    if (kvp.Value.Count < MaximumSubscriptionsPerSocket)
                    {
                        kvp.Value.Add(channel);
                        count = _channelsByWebSocket.Sum(x => x.Value.Count);
                        Log.Trace($"SamcoSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
                        return kvp.Key;
                    }
                }
            }

            if (!_connectionRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                _connectionRateLimiter.WaitToProceed();
            }

            var webSocket = new SamcoWebSocketWrapper(
                new DefaultConnectionHandler
                {
                    MaximumIdleTimeSpan = TimeSpan.FromSeconds(15)
                });

            lock (_locker)
            {
                _channelsByWebSocket.TryAdd(webSocket, new List<SamcoChannel> { channel });

                count = _channelsByWebSocket.Sum(x => x.Value.Count);
                Log.Trace($"SamcoSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
            }

            webSocket.Initialize(_wssUrl);
            webSocket.SetAuthTokenHeader(_wssAuthToken);
            webSocket.Message += OnMessage;
            webSocket.Error += OnError;
            webSocket.Closed += OnClosed;
            webSocket.Connect();

            webSocket.ConnectionHandler.ConnectionLost += OnConnectionLost;
            webSocket.ConnectionHandler.ConnectionRestored += OnConnectionRestored;
            webSocket.ConnectionHandler.ReconnectRequested += OnReconnectRequested;
            webSocket.ConnectionHandler.Initialize(webSocket.ConnectionId);

            Log.Trace("SamcoSubscriptionManager.GetFreeWebSocket(): New websocket added: " +
                      $"Hashcode: {webSocket.GetHashCode()}, " +
                      $"WebSocket connections: {_channelsByWebSocket.Count}");

            return webSocket;
        }

        private void OnClosed(object sender, WebSocketCloseData e)
        {
            Log.Error("SamcoSubscriptionManager.OnClosed(): WebSocket connection closed.");
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            Log.Error("SamcoSubscriptionManager.OnConnectionLost(): WebSocket connection lost.");
        }

        private void OnConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("SamcoSubscriptionManager.OnConnectionRestored(): WebSocket connection restored.");
            foreach (var channels in _channelsByWebSocket.Values)
            {
                foreach (var channel in channels)
                {
                    SubscribeChannel(channel.Name, channel.ChannelId, channel.Symbol);
                }
            }
        }

        private void OnReconnectRequested(object sender, EventArgs e)
        {
            var connectionHandler = (DefaultConnectionHandler)sender;

            Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): WebSocket reconnection requested [Id: {connectionHandler.ConnectionId}]");

            SamcoWebSocketWrapper webSocket = null;

            lock (_locker)
            {
                foreach (var connection in _channelsByWebSocket.Keys)
                {
                    if (connection.ConnectionId == connectionHandler.ConnectionId)
                    {
                        webSocket = connection;
                    }
                }
            }

            if (webSocket == null)
            {
                Log.Error($"SamcoSubscriptionManager.OnReconnectRequested(): WebSocket ConnectionId not found: {connectionHandler.ConnectionId}");
                return;
            }

            Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            if (!webSocket.IsOpen)
            {
                Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): Websocket connecting. [Id: {connectionHandler.ConnectionId}]");
                webSocket.Connect();
            }

            if (!webSocket.IsOpen)
            {
                Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): Websocket not open: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");
                return;
            }

            Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): Reconnected: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            List<SamcoChannel> channels;
            lock (_locker)
            {
                if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    return;
            }

            Log.Trace($"SamcoSubscriptionManager.OnReconnectRequested(): Resubscribing channels. [Id: {connectionHandler.ConnectionId}]");

            foreach (var channel in channels)
            {
                Log.Trace("Resubcribing quotes for: "+channel.Symbol);
                var ticker = _symbolMapper.GetBrokerageSymbol(channel.Symbol);
                var sub = new Subscription();
                sub.request.data.symbols.Add(new Subscription.Symbol { symbol = channel.ChannelId });
                var request = JsonConvert.SerializeObject(sub);
                Log.Trace(request);
                webSocket.Send(request);
                webSocket.Send("\n");
            }
        }




        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"SamcoSubscriptionManager.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        ConcurrentDictionary<string, QuoteUpdate> quotes = new ConcurrentDictionary<string, QuoteUpdate>();

        private void OnMessage(object sender, WebSocketMessage e)
        {
            try
            {
                var token = JToken.Parse(e.Message);
                if (token is JObject)
                {
                    var raw = token.ToObject<QuoteUpdate>();
                    if (raw.response.streaming_type.ToLowerInvariant() == "quote")
                    {
                        if (!quotes.TryGetValue(raw.response.data.sym, out var existing))
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
                _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        /// <summary>
        /// Adds a subscription for the requested symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        public void Subscribe(Symbol symbol)
        {
            try
            {
                var quote = _brokerage.GetQuote(symbol);
                _subscriptionsById[quote.listingId] = symbol;
                Log.Trace("Subscribe symbol: " + quote.listingId.ToStringInvariant());

                var quotesSubscription = SubscribeChannel("quotes", quote.listingId, symbol);
                _subscriptionsBySymbol.TryAdd(symbol, new List<SamcoWebSocketWrapper> { quotesSubscription });
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        private SamcoWebSocketWrapper SubscribeChannel(string channelName, string listingId, string symbol)
        {
            var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
            var sub = new Subscription();
            sub.request.data.symbols.Add(new Subscription.Symbol { symbol = listingId });
            var request = JsonConvert.SerializeObject(sub);
            var channel = new SamcoChannel { Name = channelName, Symbol = ticker, ChannelId = listingId };
            var webSocket = GetFreeWebSocket(channel);
            //TODO: bug handle if websocket state is still connecting
            System.Threading.Thread.Sleep(1000);
            Log.Trace("Websocket Request: "+request.ToStringInvariant());
            if (webSocket.IsOpen)
            {
                webSocket.Send(request);
                webSocket.Send("\n");

                OnSubscribe(webSocket, channel);
            }
            return webSocket;
        }


        /// <summary>
        /// Removes the subscription for the requested symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        public void Unsubscribe(Symbol symbol)
        {
            List<SamcoWebSocketWrapper> subscriptions;
            if (_subscriptionsBySymbol.TryGetValue(symbol, out subscriptions))
            {
                foreach (var webSocket in subscriptions)
                {
                    try
                    {
                        lock (_locker)
                        {
                            List<SamcoChannel> channels;
                            if (_channelsByWebSocket.TryGetValue(webSocket, out channels))
                            {
                                foreach (var channel in channels)
                                {
                                    UnsubscribeChannel(webSocket, channel);
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception);
                    }
                }

                _subscriptionsBySymbol.TryRemove(symbol, out subscriptions);
            }

        }

        private void UnsubscribeChannel(SamcoWebSocketWrapper webSocket, SamcoChannel channel)
        {
            var sub = new Subscription();
            sub.request.request_type = "unsubcribe";
            sub.request.data.symbols.Add(new Subscription.Symbol { symbol = channel.ChannelId });
            var request = JsonConvert.SerializeObject(sub);
            webSocket.Send(request);
            webSocket.Send("\n");
            OnUnsubscribe(webSocket, channel);
        }

        private void OnSubscribe(SamcoWebSocketWrapper webSocket, SamcoChannel data)
        {
            try
            {
                lock (_locker)
                {
                    List<SamcoChannel> channels;
                    if (_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    {
                        var channel = channels.First(x => x.Name == data.Name && x.Symbol == data.Symbol);

                        channel.ChannelId = data.ChannelId;

                        webSocket.ConnectionHandler.EnableMonitoring(true);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUnsubscribe(SamcoWebSocketWrapper webSocket, SamcoChannel data)
        {
            try
            {
                lock (_locker)
                {
                    List<SamcoChannel> channels;
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out channels)) return;

                    channels.Remove(channels.First(x => x.ChannelId == data.ChannelId));

                    if (channels.Count != 0) return;

                    _channelsByWebSocket.TryRemove(webSocket, out channels);
                }

                webSocket.Close();
                webSocket.ConnectionHandler.DisposeSafely();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal amount)
        {
            try
            {
                lock (_brokerage.TickLocker)
                {
                    _brokerage.Ticks.Add(new Tick
                    {
                        Value = price,
                        Time = time,
                        Symbol = symbol,
                        TickType = TickType.Trade,
                        Quantity = Math.Abs(amount)
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
            lock (_brokerage.TickLocker)
            {
                _brokerage.Ticks.Add(new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    Value = (askPrice + bidPrice) / 2m,
                    Time = DateTime.UtcNow,
                    Symbol = symbol,
                    TickType = TickType.Quote,
                    AskSize = Math.Abs(askSize),
                    BidSize = Math.Abs(bidSize)
                });
            }
        }
    }
}