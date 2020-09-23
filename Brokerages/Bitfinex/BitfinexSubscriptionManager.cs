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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages.Bitfinex.Messages;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Handles Bitfinex data subscriptions with multiple websocket connections
    /// </summary>
    public class BitfinexSubscriptionManager : DataQueueHandlerSubscriptionManager
    {
        /// <summary>
        /// Maximum number of subscribed channels per websocket connection
        /// </summary>
        /// <remarks>
        /// Source: https://medium.com/bitfinex/bitfinex-api-update-june-2019-661e806e6567
        /// </remarks>
        private const int MaximumSubscriptionsPerSocket = 30;

        private const int ConnectionTimeout = 30000;

        private readonly string _wssUrl;
        private volatile int _subscribeErrorCode;
        private readonly object _locker = new object();
        private readonly BitfinexBrokerage _brokerage;
        private readonly BitfinexSymbolMapper _symbolMapper;
        private readonly RateGate _connectionRateLimiter = new RateGate(5, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<Symbol, List<BitfinexWebSocketWrapper>> _subscriptionsBySymbol = new ConcurrentDictionary<Symbol, List<BitfinexWebSocketWrapper>>();
        private readonly ConcurrentDictionary<BitfinexWebSocketWrapper, List<Channel>> _channelsByWebSocket = new ConcurrentDictionary<BitfinexWebSocketWrapper, List<Channel>>();
        private readonly ConcurrentDictionary<int, Channel> _channels = new ConcurrentDictionary<int, Channel>();
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new ConcurrentDictionary<Symbol, DefaultOrderBook>();
        private readonly IReadOnlyDictionary<TickType, string> _tickType2ChannelName = new Dictionary<TickType, string>() {
            { TickType.Trade, "trades"},
            { TickType.Quote, "book"}
        };
        private readonly ManualResetEvent _onSubscribeEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent _onUnsubscribeEvent = new ManualResetEvent(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexSubscriptionManager"/> class.
        /// </summary>
        public BitfinexSubscriptionManager(BitfinexBrokerage brokerage, string wssUrl, BitfinexSymbolMapper symbolMapper)
        {
            _brokerage = brokerage;
            _wssUrl = wssUrl;
            _symbolMapper = symbolMapper;
        }

        /// <summary>
        /// Subscribes to the requested subscription (using an individual streaming channel)
        /// </summary>
        /// <param name="symbols">symbol list</param>
        /// <param name="tickType">Type of tick data</param>
        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            try
            {
                var states = new List<bool>(symbols.Count());
                foreach (var symbol in symbols)
                {
                    _onSubscribeEvent.Reset();
                    _subscribeErrorCode = 0;
                    var subscription = SubscribeChannel(
                        ChannelNameFromTickType(tickType),
                        symbol);

                    _subscriptionsBySymbol.AddOrUpdate(
                        symbol,
                        new List<BitfinexWebSocketWrapper> { subscription },
                        (k, v) =>
                        {
                            if (!v.Contains(subscription))
                            {
                                v.Add(subscription);
                            }
                            return v;
                        });

                    Log.Trace($"BitfinexBrokerage.Subscribe(): Sent subscribe for {symbol.Value}.");

                    if (_onSubscribeEvent.WaitOne(TimeSpan.FromSeconds(10)) && _subscribeErrorCode == 0)
                    {
                        states.Add(true);
                    }
                    else
                    {
                        Log.Trace($"BitfinexBrokerage.Subscribe(): Could not subscribe to {symbol.Value}.");
                        states.Add(false);
                    }
                }

                return states.All(s => s);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        /// <summary>
        /// Removes the subscription for the requested symbol
        /// </summary>
        /// <param name="symbols">symbol list</param>
        /// <param name="tickType">Type of tick data</param>
        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            string channelName = ChannelNameFromTickType(tickType);
            var states = new List<bool>(symbols.Count());
            foreach (var symbol in symbols)
            {
                List<BitfinexWebSocketWrapper> subscriptions;
                if (_subscriptionsBySymbol.TryGetValue(symbol, out subscriptions))
                {
                    for (int i = subscriptions.Count - 1; i >= 0; i--)
                    {
                        var webSocket = subscriptions[i];
                        _onUnsubscribeEvent.Reset();
                        try
                        {
                            Channel channel = new Channel(channelName, symbol);
                            List<Channel> channels;
                            if (_channelsByWebSocket.TryGetValue(webSocket, out channels) && channels.Contains(channel))
                            {
                                UnsubscribeChannel(webSocket, channel);

                                if (_onUnsubscribeEvent.WaitOne(TimeSpan.FromSeconds(30)))
                                {
                                    states.Add(true);
                                }
                                else
                                {
                                    Log.Trace($"BitfinexBrokerage.Unsubscribe(): Could not unsubscribe from {symbol.Value}.");
                                    states.Add(false);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception);
                        }
                    }
                }
            }
            return states.All(s => s);
        }

        protected override string ChannelNameFromTickType(TickType tickType)
        {
            string channelName;
            if (_tickType2ChannelName.TryGetValue(tickType, out channelName))
            {
                return channelName;
            }
            else
            {
                throw new ArgumentOutOfRangeException("TickType", $"BitfinexSubscriptionManager.Subscribe(): Tick type {tickType} is not allowed for this brokerage.");
            }
        }

        private BitfinexWebSocketWrapper SubscribeChannel(string channelName, Symbol symbol)
        {
            var channel = new Channel(channelName, symbol);

            var webSocket = GetFreeWebSocket(channel);

            webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = channelName,
                pair = _symbolMapper.GetBrokerageSymbol(symbol)
            }));

            return webSocket;
        }

        private void UnsubscribeChannel(IWebSocket webSocket, Channel channel)
        {
            int channelId = _channels.First(c => c.Value.Equals(channel)).Key;
            webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "unsubscribe",
                chanId = channelId.ToStringInvariant()
            }));
        }

        private BitfinexWebSocketWrapper GetFreeWebSocket(Channel channel)
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
                        Log.Trace($"BitfinexSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");

                        return kvp.Key;
                    }
                }
            }

            if (!_connectionRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                _connectionRateLimiter.WaitToProceed();
            }

            var webSocket = new BitfinexWebSocketWrapper(
                new DefaultConnectionHandler
                {
                    MaximumIdleTimeSpan = TimeSpan.FromSeconds(15)
                });

            lock (_locker)
            {
                _channelsByWebSocket.TryAdd(webSocket, new List<Channel> { channel });

                count = _channelsByWebSocket.Sum(x => x.Value.Count);
                Log.Trace($"BitfinexSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
            }

            webSocket.Initialize(_wssUrl);
            webSocket.Message += OnMessage;

            Connect(webSocket);

            webSocket.ConnectionHandler.ReconnectRequested += OnReconnectRequested;
            webSocket.ConnectionHandler.Initialize(webSocket.ConnectionId);

            int connections;
            lock (_locker)
            {
                connections = _channelsByWebSocket.Count;
            }

            Log.Trace("BitfinexSubscriptionManager.GetFreeWebSocket(): New websocket added: " +
                      $"Hashcode: {webSocket.GetHashCode()}, " +
                      $"WebSocket connections: {connections}");

            return webSocket;
        }

        private void Connect(IWebSocket webSocket)
        {
            var connectedEvent = new ManualResetEvent(false);
            EventHandler onOpenAction = (s, e) =>
            {
                connectedEvent.Set();
            };

            webSocket.Open += onOpenAction;

            try
            {
                webSocket.Connect();

                if (!connectedEvent.WaitOne(ConnectionTimeout))
                {
                    throw new Exception("BitfinexSubscriptionManager.Connect(): WebSocket connection timeout.");
                }
            }
            finally
            {
                webSocket.Open -= onOpenAction;

                connectedEvent.DisposeSafely();
            }
        }

        private void OnReconnectRequested(object sender, EventArgs e)
        {
            var connectionHandler = (DefaultConnectionHandler)sender;

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): WebSocket reconnection requested [Id: {connectionHandler.ConnectionId}]");

            BitfinexWebSocketWrapper webSocket = null;

            lock (_locker)
            {
                webSocket = _channelsByWebSocket.Keys
                    .FirstOrDefault(connection => connection.ConnectionId == connectionHandler.ConnectionId);
            }

            if (webSocket == null)
            {
                Log.Error($"BitfinexSubscriptionManager.OnReconnectRequested(): WebSocket ConnectionId not found: {connectionHandler.ConnectionId}");
                return;
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): IsOpen:{webSocket.IsOpen} [Id: {connectionHandler.ConnectionId}]");

            if (!webSocket.IsOpen)
            {
                Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Websocket connecting. [Id: {connectionHandler.ConnectionId}]");
                webSocket.Connect();
            }

            if (!webSocket.IsOpen)
            {
                Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Websocket not open: IsOpen:{webSocket.IsOpen} [Id: {connectionHandler.ConnectionId}]");
                return;
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Reconnected: IsOpen:{webSocket.IsOpen} [Id: {connectionHandler.ConnectionId}]");

            List<Channel> channels;
            lock (_locker)
            {
                if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                {
                    return;
                }
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Resubscribing channels. [Id: {connectionHandler.ConnectionId}]");

            foreach (var channel in channels)
            {
                webSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = channel.Name,
                    pair = _symbolMapper.GetBrokerageSymbol(channel.Symbol)
                }));
            }
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            var webSocket = (BitfinexWebSocketWrapper)sender;

            try
            {
                var token = JToken.Parse(e.Message);

                webSocket.ConnectionHandler.KeepAlive(DateTime.UtcNow);

                if (token is JArray)
                {
                    var channel = token[0].ToObject<int>();

                    if (token[1].Type == JTokenType.String)
                    {
                        var type = token[1].Value<string>();

                        switch (type)
                        {
                            // heartbeat
                            case "hb":
                                return;

                            // trade execution
                            case "te":
                                OnUpdate(channel, token[2].ToObject<string[]>());
                                break;

                            // ignored -- trades already handled in "te" message
                            // https://github.com/bitfinexcom/bitfinex-api-node#te-vs-tu-messages
                            case "tu":
                                break;

                            default:
                                Log.Trace($"BitfinexSubscriptionManager.OnMessage(): Unexpected message type: {type}");
                                return;
                        }
                    }

                    // public channels
                    else if (channel != 0 && token[1].Type == JTokenType.Array)
                    {
                        if (token[1][0].Type == JTokenType.Array)
                        {
                            OnSnapshot(
                                channel,
                                token[1].ToObject<string[][]>()
                            );
                        }
                        else
                        {
                            // pass channel id as separate arg
                            OnUpdate(
                                channel,
                                token[1].ToObject<string[]>()
                            );
                        }
                    }
                }
                else if (token is JObject)
                {
                    var raw = token.ToObject<Messages.BaseMessage>();
                    switch (raw.Event.ToLowerInvariant())
                    {
                        case "subscribed":
                            OnSubscribe(webSocket, token.ToObject<ChannelSubscription>());
                            return;

                        case "unsubscribed":
                            OnUnsubscribe(webSocket, token.ToObject<ChannelUnsubscribing>());
                            return;

                        case "auth":
                        case "info":
                        case "ping":
                            return;

                        case "error":
                            var error = token.ToObject<ErrorMessage>();
                            // 10300 Subscription failed (generic) | 10301 : Already subscribed | 10302 : Unknown channel
                            // see https://docs.bitfinex.com/docs/ws-general
                            if (error.Code == 10300 || error.Code == 10301 || error.Code == 10302)
                            {
                                _subscribeErrorCode = error.Code;
                                _onSubscribeEvent.Set();
                            }
                            Log.Error($"BitfinexSubscriptionManager.OnMessage(): {e.Message}");
                            return;

                        default:
                            Log.Trace($"BitfinexSubscriptionManager.OnMessage(): Unexpected message format: {e.Message}");
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnSubscribe(BitfinexWebSocketWrapper webSocket, Messages.ChannelSubscription data)
        {
            try
            {
                lock (_locker)
                {
                    var channel = new Channel(data.Channel, _symbolMapper.GetLeanSymbol(data.Symbol));

                    _channels.AddOrUpdate(data.ChannelId, channel);
                    _onSubscribeEvent.Set();

                    webSocket.ConnectionHandler.EnableMonitoring(true);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUnsubscribe(BitfinexWebSocketWrapper webSocket, Messages.ChannelUnsubscribing data)
        {
            try
            {
                lock (_locker)
                {
                    Channel channel;
                    if (!_channels.TryRemove(data.ChannelId, out channel)) return;

                    _onUnsubscribeEvent.Set();

                    List<Channel> channels;
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out channels)) return;

                    channels.Remove(channel);

                    if (channels.Count(c => c.Symbol.Equals(channel.Symbol)) == 0)
                    {
                        List<BitfinexWebSocketWrapper> subscriptions;
                        if (_subscriptionsBySymbol.TryGetValue(channel.Symbol, out subscriptions))
                        {
                            subscriptions.Remove(webSocket);

                            if (subscriptions.Count == 0)
                            {
                                _subscriptionsBySymbol.TryRemove(channel.Symbol, out subscriptions);
                            }
                        }
                    }

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

        private void OnSnapshot(int channelId, string[][] entries)
        {
            try
            {
                Channel channel;

                lock (_locker)
                {
                    if (!_channels.TryGetValue(channelId, out channel))
                    {
                        _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
                        return;
                    }
                }

                switch (channel.Name.ToLowerInvariant())
                {
                    case "book":
                        ProcessOrderBookSnapshot(channel, entries);
                        return;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void ProcessOrderBookSnapshot(Channel channel, string[][] entries)
        {
            try
            {
                var symbol = channel.Symbol;

                DefaultOrderBook orderBook;
                if (!_orderBooks.TryGetValue(symbol, out orderBook))
                {
                    orderBook = new DefaultOrderBook(symbol);
                    _orderBooks[symbol] = orderBook;
                }
                else
                {
                    orderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
                    orderBook.Clear();
                }

                foreach (var entry in entries)
                {
                    var price = decimal.Parse(entry[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var amount = decimal.Parse(entry[2], NumberStyles.Float, CultureInfo.InvariantCulture);

                    if (amount > 0)
                        orderBook.UpdateBidRow(price, amount);
                    else
                        orderBook.UpdateAskRow(price, Math.Abs(amount));
                }

                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;

                EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUpdate(int channelId, string[] entries)
        {
            try
            {
                Channel channel;

                lock (_locker)
                {
                    if (!_channels.TryGetValue(channelId, out channel))
                    {
                        _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
                        return;
                    }
                }

                switch (channel.Name.ToLowerInvariant())
                {
                    case "book":
                        ProcessOrderBookUpdate(channel, entries);
                        return;

                    case "trades":
                        ProcessTradeUpdate(channel, entries);
                        return;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void ProcessOrderBookUpdate(Channel channel, string[] entries)
        {
            try
            {
                var symbol = channel.Symbol;
                var orderBook = _orderBooks[symbol];

                var price = decimal.Parse(entries[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                var count = Parse.Long(entries[1]);
                var amount = decimal.Parse(entries[2], NumberStyles.Float, CultureInfo.InvariantCulture);

                if (count == 0)
                {
                    orderBook.RemovePriceLevel(price);
                }
                else
                {
                    if (amount > 0)
                    {
                        orderBook.UpdateBidRow(price, amount);
                    }
                    else if (amount < 0)
                    {
                        orderBook.UpdateAskRow(price, Math.Abs(amount));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Entries: [{string.Join(",", entries)}]");
                throw;
            }
        }

        private void ProcessTradeUpdate(Channel channel, string[] entries)
        {
            try
            {
                var time = Time.UnixMillisecondTimeStampToDateTime(double.Parse(entries[1], NumberStyles.Float, CultureInfo.InvariantCulture));
                var amount = decimal.Parse(entries[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                var price = decimal.Parse(entries[3], NumberStyles.Float, CultureInfo.InvariantCulture);

                EmitTradeTick(channel.Symbol, time, price, amount);
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
                    _brokerage.EmitTick(new Tick
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
                _brokerage.EmitTick(new Tick
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

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }
    }
}
