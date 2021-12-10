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
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Bitfinex.Messages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexBrokerage
    {
        private readonly RateGate _connectionRateLimiter = new(5, TimeSpan.FromMinutes(1));
        private readonly ConcurrentDictionary<IWebSocket, BitfinexWebSocketChannels> _channelsByWebSocket = new();
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new();
        private readonly ManualResetEvent _onSubscribeEvent = new(false);
        private readonly ManualResetEvent _onUnsubscribeEvent = new(false);
        private readonly object _locker = new();

        private const int MaximumSymbolsPerConnection = 12;

        /// <summary>
        /// Subscribes to the requested symbol (using an individual streaming channel)
        /// </summary>
        /// <param name="webSocket">The websocket instance</param>
        /// <param name="symbol">The symbol to subscribe</param>
        private bool Subscribe(IWebSocket webSocket, Symbol symbol)
        {
            lock (_locker)
            {
                if (!_channelsByWebSocket.ContainsKey(webSocket))
                {
                    _channelsByWebSocket.TryAdd(webSocket, new BitfinexWebSocketChannels());
                }
            }

            var success = SubscribeChannel(webSocket, "trades", symbol);
            success &= SubscribeChannel(webSocket, "book", symbol);

            return success;
        }

        /// <summary>
        /// Ends current subscription
        /// </summary>
        /// <param name="webSocket">The websocket instance</param>
        /// <param name="symbol">The symbol to unsubscribe</param>
        private bool Unsubscribe(IWebSocket webSocket, Symbol symbol)
        {
            BitfinexWebSocketChannels channels;

            lock (_locker)
            {
                if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                {
                    return true;
                }
            }

            var success = UnsubscribeChannel(webSocket, channels, new Channel("trades", symbol));
            success &= UnsubscribeChannel(webSocket, channels, new Channel("book", symbol));

            return success;
        }

        private bool SubscribeChannel(IWebSocket webSocket, string channelName, Symbol symbol)
        {
            _onSubscribeEvent.Reset();

            webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = channelName,
                pair = _symbolMapper.GetBrokerageSymbol(symbol)
            }));

            if (!_onSubscribeEvent.WaitOne(TimeSpan.FromSeconds(30)))
            {
                Log.Error($"BitfinexBrokerage.Unsubscribe(): Could not subscribe to {symbol.Value}/{channelName}.");
                return false;
            }

            return true;
        }

        private bool UnsubscribeChannel(IWebSocket webSocket, BitfinexWebSocketChannels channels, Channel channel)
        {
            if (channels.Contains(channel))
            {
                var channelId = channels.GetChannelId(channel);

                _onUnsubscribeEvent.Reset();

                webSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "unsubscribe",
                    chanId = channelId.ToStringInvariant()
                }));

                if (!_onUnsubscribeEvent.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    Log.Error($"BitfinexBrokerage.Unsubscribe(): Could not unsubscribe from {channel.Symbol.Value}/{channel.Name}.");
                    return false;
                }
            }

            return true;
        }

        private void OnDataMessage(WebSocketMessage webSocketMessage)
        {
            var webSocket = (BitfinexWebSocketWrapper)webSocketMessage.WebSocket;
            var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;

            try
            {
                var token = JToken.Parse(e.Message);

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
                                OnUpdate(webSocket, channel, token[2].ToObject<string[]>());
                                break;

                            // ignored -- trades already handled in "te" message
                            // https://github.com/bitfinexcom/bitfinex-api-node#te-vs-tu-messages
                            case "tu":
                                break;

                            default:
                                Log.Error($"BitfinexBrokerage.OnDataMessage(): Unexpected message type: {type}");
                                return;
                        }
                    }

                    // public channels
                    else if (channel != 0 && token[1].Type == JTokenType.Array)
                    {
                        var tokens = (JArray)token[1];

                        if (tokens.Count > 0)
                        {
                            if (tokens[0].Type == JTokenType.Array)
                            {
                                OnSnapshot(
                                    webSocket,
                                    channel,
                                    tokens.ToObject<string[][]>()
                                );
                            }
                            else
                            {
                                // pass channel id as separate arg
                                OnUpdate(
                                    webSocket,
                                    channel,
                                    tokens.ToObject<string[]>()
                                );
                            }
                        }
                    }
                }
                else if (token is JObject)
                {
                    var raw = token.ToObject<BaseMessage>();
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
                                //_subscribeErrorCode = error.Code;
                                _onSubscribeEvent.Set();
                            }
                            Log.Error($"BitfinexBrokerage.OnDataMessage(): {e.Message}");
                            return;

                        default:
                            Log.Error($"BitfinexBrokerage.OnDataMessage(): Unexpected message format: {e.Message}");
                            break;
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnSubscribe(BitfinexWebSocketWrapper webSocket, ChannelSubscription data)
        {
            try
            {
                lock (_locker)
                {
                    var symbol = _symbolMapper.GetLeanSymbol(data.Symbol, SecurityType.Crypto, Market.Bitfinex);
                    var channel = new Channel(data.Channel, symbol);

                    if (!_channelsByWebSocket.TryGetValue(webSocket, out var channels))
                    {
                        _onSubscribeEvent.Set();
                        return;
                    }

                    // we need to update the channel on re subscription
                    var channelsToRemove = channels
                        .Where(x => x.Value.Equals(channel))
                        .Select(x => x.Key)
                        .ToList();
                    foreach (var channelId in channelsToRemove)
                    {
                        channels.TryRemove(channelId, out _);
                    }
                    channels.AddOrUpdate(data.ChannelId, channel);

                    Log.Trace($"BitfinexBrokerage.OnSubscribe(): Channel subscribed: Id:{data.ChannelId} {channel.Symbol}/{channel.Name}");

                    _onSubscribeEvent.Set();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnUnsubscribe(BitfinexWebSocketWrapper webSocket, ChannelUnsubscribing data)
        {
            try
            {
                lock (_locker)
                {
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out var channels))
                    {
                        _onUnsubscribeEvent.Set();
                        return;
                    }

                    if (!channels.TryRemove(data.ChannelId, out _))
                    {
                        _onUnsubscribeEvent.Set();
                        return;
                    }

                    _onUnsubscribeEvent.Set();

                    if (channels.Count != 0)
                    {
                        return;
                    }

                    _channelsByWebSocket.TryRemove(webSocket, out channels);
                }

                webSocket.Close();
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnSnapshot(BitfinexWebSocketWrapper webSocket, int channelId, string[][] entries)
        {
            try
            {
                Channel channel;

                lock (_locker)
                {
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out var channels))
                    {
                        return;
                    }

                    if (!channels.TryGetValue(channelId, out channel))
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
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

                if (!_orderBooks.TryGetValue(symbol, out var orderBook))
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
                    {
                        orderBook.UpdateBidRow(price, amount);
                    }
                    else
                    {
                        orderBook.UpdateAskRow(price, Math.Abs(amount));
                    }
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

        private void OnUpdate(BitfinexWebSocketWrapper webSocket, int channelId, string[] entries)
        {
            try
            {
                Channel channel;

                lock (_locker)
                {
                    if (!_channelsByWebSocket.TryGetValue(webSocket, out var channels))
                    {
                        return;
                    }

                    if (!channels.TryGetValue(channelId, out channel))
                    {
                        OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, -1, $"Message received from unknown channel Id {channelId}"));
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
                var time = Time.UnixMillisecondTimeStampToDateTime(decimal.Parse(entries[1], NumberStyles.Float, CultureInfo.InvariantCulture));
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
                EmitTick(new Tick
                {
                    Value = price,
                    Time = time,
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Quantity = Math.Abs(amount)
                });
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            EmitTick(new Tick
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

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }
    }
}
