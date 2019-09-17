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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Handles Bitfinex data subscriptions with multiple websocket connections
    /// </summary>
    public class BitfinexSubscriptionManager
    {
        /// <summary>
        /// Maximum number of subscribed channels per websocket connection
        /// </summary>
        /// <remarks>
        /// Source: https://medium.com/bitfinex/bitfinex-api-update-june-2019-661e806e6567
        /// </remarks>
        private const int MaximumSubscriptionsPerSocket = 30;

        private readonly string _wssUrl;
        private readonly object _locker = new object();
        private readonly BitfinexBrokerage _brokerage;
        private readonly BitfinexSymbolMapper _symbolMapper;
        private readonly RateGate _connectionRateLimiter = new RateGate(10, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<Symbol, List<BitfinexWebSocketWrapper>> _subscriptionsBySymbol = new ConcurrentDictionary<Symbol, List<BitfinexWebSocketWrapper>>();
        private readonly ConcurrentDictionary<BitfinexWebSocketWrapper, List<BitfinexChannel>> _channelsByWebSocket = new ConcurrentDictionary<BitfinexWebSocketWrapper, List<BitfinexChannel>>();
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new ConcurrentDictionary<Symbol, DefaultOrderBook>();

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
        /// Returns true if there is an active subscription for the requested symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public bool IsSubscribed(Symbol symbol)
        {
            return _subscriptionsBySymbol.ContainsKey(symbol);
        }

        /// <summary>
        /// Adds a subscription for the requested symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        public void Subscribe(Symbol symbol)
        {
            try
            {
                var bookSubscription = SubscribeChannel("book", symbol);
                var tradesSubscription = SubscribeChannel("trades", symbol);

                _subscriptionsBySymbol.TryAdd(symbol, new List<BitfinexWebSocketWrapper> { bookSubscription, tradesSubscription });
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
        /// <param name="symbol">The symbol</param>
        public void Unsubscribe(Symbol symbol)
        {
            List<BitfinexWebSocketWrapper> subscriptions;
            if (_subscriptionsBySymbol.TryGetValue(symbol, out subscriptions))
            {
                foreach (var webSocket in subscriptions)
                {
                    try
                    {
                        lock (_locker)
                        {
                            List<BitfinexChannel> channels;
                            if (_channelsByWebSocket.TryGetValue(webSocket, out channels))
                            {
                                foreach (var channel in channels)
                                {
                                    UnsubscribeChannel(webSocket, channel.ChannelId);
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

        private BitfinexWebSocketWrapper SubscribeChannel(string channelName, Symbol symbol)
        {
            var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
            var channel = new BitfinexChannel { Name = channelName, Symbol = ticker, ChannelId = string.Empty };
            var webSocket = GetFreeWebSocket(channel);

            webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = channelName,
                pair = ticker
            }));

            return webSocket;
        }

        private void UnsubscribeChannel(IWebSocket webSocket, string channelId)
        {
            webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "unsubscribe",
                channelId
            }));
        }

        private BitfinexWebSocketWrapper GetFreeWebSocket(BitfinexChannel channel)
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
                _channelsByWebSocket.TryAdd(webSocket, new List<BitfinexChannel> { channel });

                count = _channelsByWebSocket.Sum(x => x.Value.Count);
                Log.Trace($"BitfinexSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
            }

            webSocket.Initialize(_wssUrl);
            webSocket.Message += OnMessage;
            webSocket.Error += OnError;
            webSocket.Connect();

            webSocket.ConnectionHandler.ConnectionLost += OnConnectionLost;
            webSocket.ConnectionHandler.ConnectionRestored += OnConnectionRestored;
            webSocket.ConnectionHandler.ReconnectRequested += OnReconnectRequested;
            webSocket.ConnectionHandler.Initialize(webSocket.ConnectionId);

            Log.Trace("BitfinexSubscriptionManager.GetFreeWebSocket(): New websocket added: " +
                      $"Hashcode: {webSocket.GetHashCode()}, " +
                      $"WebSocket connections: {_channelsByWebSocket.Count}");

            return webSocket;
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            Log.Error("BitfinexSubscriptionManager.OnConnectionLost(): WebSocket connection lost.");
        }

        private void OnConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("BitfinexSubscriptionManager.OnConnectionRestored(): WebSocket connection restored.");
        }

        private void OnReconnectRequested(object sender, EventArgs e)
        {
            var connectionHandler = (DefaultConnectionHandler)sender;

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): WebSocket reconnection requested [Id: {connectionHandler.ConnectionId}]");

            BitfinexWebSocketWrapper webSocket = null;

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
                Log.Error($"BitfinexSubscriptionManager.OnReconnectRequested(): WebSocket ConnectionId not found: {connectionHandler.ConnectionId}");
                return;
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            if (!webSocket.IsOpen)
            {
                Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Websocket connecting. [Id: {connectionHandler.ConnectionId}]");
                webSocket.Connect();
            }

            if (!webSocket.IsOpen)
            {
                Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Websocket not open: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");
                return;
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Reconnected: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            List<BitfinexChannel> channels;
            lock (_locker)
            {
                if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                return;
            }

            Log.Trace($"BitfinexSubscriptionManager.OnReconnectRequested(): Resubscribing channels. [Id: {connectionHandler.ConnectionId}]");

            foreach (var channel in channels)
            {
                webSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = channel.Name,
                    pair = channel.Symbol
                }));
            }
        }

        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"BitfinexSubscriptionManager.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }

        private void OnMessage(object sender, WebSocketMessage e)
        {
            var webSocket = (BitfinexWebSocketWrapper) sender;

            try
            {
                var token = JToken.Parse(e.Message);

                if (token is JArray)
                {
                    var channel = token[0].ToObject<int>();
                    // heartbeat
                    if (token[1].Type == JTokenType.String && token[1].Value<string>() == "hb")
                    {
                        webSocket.ConnectionHandler.KeepAlive(DateTime.UtcNow);
                        return;
                    }

                    // public channels
                    if (channel != 0)
                    {
                        webSocket.ConnectionHandler.KeepAlive(DateTime.UtcNow);

                        if (token.Count() == 2)
                        {
                            OnSnapshot(
                                webSocket,
                                token[0].ToObject<string>(),
                                token[1].ToObject<string[][]>()
                            );
                        }
                        else
                        {
                            // pass channel id as separate arg
                            OnUpdate(
                                webSocket,
                                token[0].ToObject<string>(),
                                token.ToObject<string[]>().Skip(1).ToArray()
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
                            OnSubscribe(webSocket, token.ToObject<Messages.ChannelSubscription>());
                            return;
                        case "unsubscribed":
                            OnUnsubscribe(webSocket, token.ToObject<Messages.ChannelUnsubscribing>());
                            return;
                        case "auth":
                        case "info":
                        case "ping":
                            return;
                        case "error":
                            var error = token.ToObject<Messages.ErrorMessage>();
                            Log.Trace($"BitfinexSubscriptionManager.OnMessage(): {error.Level}: {error.Message}");
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
                    List<BitfinexChannel> channels;
                    if (_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    {
                        var channel = channels.First(x => x.Name == data.Channel && x.Symbol == data.Symbol);

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

        private void OnUnsubscribe(BitfinexWebSocketWrapper webSocket, Messages.ChannelUnsubscribing data)
        {
            try
            {
                lock (_locker)
                {
                    List<BitfinexChannel> channels;
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

        private void OnSnapshot(BitfinexWebSocketWrapper webSocket, string channelId, string[][] entries)
        {
            try
            {
                BitfinexChannel channel;

                lock (_locker)
                {
                    List<BitfinexChannel> channels;
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    {
                        _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
                        return;
                    }

                    channel = channels.FirstOrDefault(x => x.ChannelId == channelId);
                    if (channel == null)
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
                    case "trades":
                        ProcessTradesSnapshot(channel, entries);
                        return;
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void ProcessOrderBookSnapshot(BitfinexChannel channel, string[][] entries)
        {
            try
            {
                var symbol = _symbolMapper.GetLeanSymbol(channel.Symbol);

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

        private void ProcessTradesSnapshot(BitfinexChannel channel, string[][] entries)
        {
            try
            {
                var symbol = _symbolMapper.GetLeanSymbol(channel.Symbol);
                foreach (var entry in entries)
                {
                    // pass time, price, amount
                    EmitTradeTick(symbol, entry.Skip(1).ToArray());
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUpdate(BitfinexWebSocketWrapper webSocket, string channelId, string[] entries)
        {
            try
            {
                BitfinexChannel channel;

                lock (_locker)
                {
                    List<BitfinexChannel> channels;
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    {
                        _brokerage.OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
                        return;
                    }

                    channel = channels.FirstOrDefault(x => x.ChannelId == channelId);
                    if (channel == null)
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

        private void ProcessOrderBookUpdate(BitfinexChannel channel, string[] entries)
        {
            try
            {
                var symbol = _symbolMapper.GetLeanSymbol(channel.Symbol);
                var orderBook = _orderBooks[symbol];

                var price = decimal.Parse(entries[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                var count = Parse.Int(entries[1]);
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
                Log.Error(e);
                throw;
            }
        }

        private void ProcessTradeUpdate(BitfinexChannel channel, string[] entries)
        {
            try
            {
                string eventType = entries[0];
                if (eventType == "tu")
                {
                    var symbol = _symbolMapper.GetLeanSymbol(channel.Symbol);
                    // pass time, price, amount
                    EmitTradeTick(symbol, new[] { entries[3], entries[4], entries[5] });
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void EmitTradeTick(Symbol symbol, string[] entries)
        {
            try
            {
                var time = Time.UnixTimeStampToDateTime(double.Parse(entries[0], NumberStyles.Float, CultureInfo.InvariantCulture));
                var price = decimal.Parse(entries[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                var amount = decimal.Parse(entries[2], NumberStyles.Float, CultureInfo.InvariantCulture);

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

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }
    }
}
