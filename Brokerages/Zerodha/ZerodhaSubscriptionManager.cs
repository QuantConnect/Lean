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
using System.Linq;
using Newtonsoft.Json;
using System.Net.WebSockets;
using QuantConnect.Logging;
using QuantConnect.Util;
using System.Text;
using QuantConnect.Brokerages.Zerodha.Messages;
using QuantConnect.Data;

namespace QuantConnect.Brokerages.Zerodha
{
    public class ZerodhaSubscriptionManager : DataQueueHandlerSubscriptionManager
    {
        private readonly string _wssUrl;
        private readonly object _locker = new object();
        private readonly ZerodhaBrokerage _brokerage;
        private readonly ZerodhaSymbolMapper _symbolMapper;
        private string _wssAuthToken;
        private const int MaximumSubscriptionsPerSocket = 30;
        private ConcurrentDictionary<uint, Symbol> _subscriptionsById = new ConcurrentDictionary<uint, Symbol>();
        private RateGate _connectionRateLimiter = new RateGate(30, TimeSpan.FromMinutes(1));
        private ConcurrentDictionary<Symbol, List<ZerodhaWebSocketWrapper>> _subscriptionsBySymbol = new ConcurrentDictionary<Symbol, List<ZerodhaWebSocketWrapper>>();
        private ConcurrentDictionary<ZerodhaWebSocketWrapper, List<Channel>> _channelsByWebSocket = new ConcurrentDictionary<ZerodhaWebSocketWrapper, List<Channel>>();
        int _timerTick = 5;
        private int _interval = 5;
        // If set to true will print extra debug information
        private bool _debug = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="ZerodhaSubscriptionManager"/> class.
        /// </summary>
        public ZerodhaSubscriptionManager(ZerodhaBrokerage brokerage, string wssUrl, ZerodhaSymbolMapper symbolMapper, string wssAuthToken)
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

        private ZerodhaWebSocketWrapper GetFreeWebSocket(Channel channel)
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
                        Log.Trace($"ZerodhaSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
                        return kvp.Key;
                    }
                }
            }

            if (!_connectionRateLimiter.WaitToProceed(TimeSpan.Zero))
            {
                _connectionRateLimiter.WaitToProceed();
            }

            var webSocket = new ZerodhaWebSocketWrapper(
                new DefaultConnectionHandler
                {
                    MaximumIdleTimeSpan = TimeSpan.FromSeconds(15)
                });

            lock (_locker)
            {
                _channelsByWebSocket.TryAdd(webSocket, new List<Channel> { channel });

                count = _channelsByWebSocket.Sum(x => x.Value.Count);
                Log.Trace($"ZerodhaSubscriptionManager.GetFreeWebSocket(): Channel added: Total channels:{count}");
            }

            webSocket.Initialize(_wssUrl);
            webSocket.Message += OnMessage;
            webSocket.Error += OnError;
            webSocket.Closed += OnClosed;
            webSocket.Connect();

            webSocket.ConnectionHandler.ConnectionLost += OnConnectionLost;
            webSocket.ConnectionHandler.ConnectionRestored += OnConnectionRestored;
            webSocket.ConnectionHandler.ReconnectRequested += OnReconnectRequested;
            webSocket.ConnectionHandler.Initialize(webSocket.ConnectionId);

            Log.Trace("ZerodhaSubscriptionManager.GetFreeWebSocket(): New websocket added: " +
                      $"Hashcode: {webSocket.GetHashCode()}, " +
                      $"WebSocket connections: {_channelsByWebSocket.Count}");

            return webSocket;
        }

        private void OnClosed(object sender, WebSocketCloseData e)
        {
            Log.Error("ZerodhaSubscriptionManager.OnClosed(): WebSocket connection closed.");
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            Log.Error("ZerodhaSubscriptionManager.OnConnectionLost(): WebSocket connection lost.");
        }

        private void OnConnectionRestored(object sender, EventArgs e)
        {
            Log.Trace("ZerodhaSubscriptionManager.OnConnectionRestored(): WebSocket connection restored.");
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

            Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): WebSocket reconnection requested [Id: {connectionHandler.ConnectionId}]");

            ZerodhaWebSocketWrapper webSocket = null;

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
                Log.Error($"ZerodhaSubscriptionManager.OnReconnectRequested(): WebSocket ConnectionId not found: {connectionHandler.ConnectionId}");
                return;
            }

            Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            if (!webSocket.IsOpen)
            {
                Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): Websocket connecting. [Id: {connectionHandler.ConnectionId}]");
                webSocket.Connect();
            }

            if (!webSocket.IsOpen)
            {
                Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): Websocket not open: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");
                return;
            }

            Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): Reconnected: IsOpen:{webSocket.IsOpen} ReadyState:{webSocket.ReadyState} [Id: {connectionHandler.ConnectionId}]");

            List<Channel> channels;
            lock (_locker)
            {
                if (!_channelsByWebSocket.TryGetValue(webSocket, out channels))
                    return;
            }

            Log.Trace($"ZerodhaSubscriptionManager.OnReconnectRequested(): Resubscribing channels. [Id: {connectionHandler.ConnectionId}]");

            foreach (var channel in channels)
            {
                Log.Trace("Resubcribing quotes for: "+channel.Symbol);
                var ticker = _symbolMapper.GetBrokerageSymbol(channel.Symbol);
                var sub = new Subscription();
                sub.a = "subscribe";
                sub.v = new string[] { channel.ChannelId };
                var request = JsonConvert.SerializeObject(sub);
                Log.Trace(request);
                webSocket.Send(request);
                webSocket.Send("\n");
            }
        }




        private void OnError(object sender, WebSocketError e)
        {
            Log.Error($"ZerodhaSubscriptionManager.OnError(): Message: {e.Message} Exception: {e.Exception}");
        }


        private void OnMessage(object sender, MessageData e)
        {
           
            _timerTick = _interval;
            if (e.MessageType == WebSocketMessageType.Binary)
            {
                if (e.Count == 1)
                {
                    if (_debug) Console.WriteLine(DateTime.Now.ToLocalTime() + " Heartbeat");
                }
                else
                {
                    int offset = 0;
                    ushort count = ReadShort(e.Data, ref offset); //number of packets
                    if (_debug) Console.WriteLine("No of packets: " + count);
                    if (_debug) Console.WriteLine("No of bytes: " + e.Count);

                    for (ushort i = 0; i < count; i++)
                    {
                        ushort length = ReadShort(e.Data, ref offset); // length of the packet
                        if (_debug) Console.WriteLine("Packet Length " + length);
                        Messages.Tick tick = new Messages.Tick();
                        if (length == 8) // ltp
                            tick = ReadLTP(e.Data, ref offset);
                        else if (length == 28) // index quote
                            tick = ReadIndexQuote(e.Data, ref offset);
                        else if (length == 32) // index quote
                            tick = ReadIndexFull(e.Data, ref offset);
                        else if (length == 44) // quote
                            tick = ReadQuote(e.Data, ref offset);
                        else if (length == 184) // full with marketdepth and timestamp
                            tick = ReadFull(e.Data, ref offset);
                        // If the number of bytes got from stream is less that that is required
                        // data is invalid. This will skip that wrong tick
                        if (tick.InstrumentToken != 0 && offset <= e.Count)
                        {
                            var sym = _subscriptionsById[tick.InstrumentToken];
                            //TODO handle this
                            //EmitQuoteTick(sym, upd.bPr, tick.Bids.bSz, upd.aPr, upd.aSz);
                        }
                    }
                }
            }
            else if (e.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(e.Data.Take(e.Count).ToArray());
                if (_debug) Console.WriteLine("WebSocket Message: " + message);

                Dictionary<string, dynamic> messageDict = Utils.JsonDeserialize(message);
                if (messageDict["type"] == "order")
                {
                    //TODO handle this
                    //OnOrderUpdate?.Invoke(new Order(messageDict["data"]));
                    //EmitFillOrder();
                }
                else if (messageDict["type"] == "error")
                {
                    //TODO handle this
                    //OnError?.Invoke(messageDict["data"]);
                }
            }
            else if (e.MessageType == WebSocketMessageType.Close)
            {
                //Close();
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
                _subscriptionsById[quote.InstrumentToken] = symbol;
                Log.Trace("Subscribe symbol: " + quote.InstrumentToken.ToStringInvariant());

                var quotesSubscription = SubscribeChannel("quotes", quote.InstrumentToken.ToStringInvariant(), symbol);
                _subscriptionsBySymbol.TryAdd(symbol, new List<ZerodhaWebSocketWrapper> { quotesSubscription });
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                throw;
            }
        }

        private ZerodhaWebSocketWrapper SubscribeChannel(string channelName, string listingId, string symbol)
        {
            var ticker = _symbolMapper.GetBrokerageSymbol(symbol);
            var sub = new Subscription();
            sub.a = "subscribe";
            sub.v = new string[] { listingId };
            var request = JsonConvert.SerializeObject(sub);
            var channel = new Channel { Name = channelName, Symbol = ticker, ChannelId = listingId };
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
            List<ZerodhaWebSocketWrapper> subscriptions;
            if (_subscriptionsBySymbol.TryGetValue(symbol, out subscriptions))
            {
                foreach (var webSocket in subscriptions)
                {
                    try
                    {
                        lock (_locker)
                        {
                            List<Channel> channels;
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

        private void UnsubscribeChannel(ZerodhaWebSocketWrapper webSocket, Channel channel)
        {
            var sub = new Subscription();
            sub.a = "unsubcribe";
            sub.v = new string[] { channel.ChannelId };
            var request = JsonConvert.SerializeObject(sub);
            webSocket.Send(request);
            webSocket.Send("\n");
            OnUnsubscribe(webSocket, channel);
        }

        private void OnSubscribe(ZerodhaWebSocketWrapper webSocket, Channel data)
        {
            try
            {
                lock (_locker)
                {
                    List<Channel> channels;
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

        private void OnUnsubscribe(ZerodhaWebSocketWrapper webSocket, Channel data)
        {
            try
            {
                lock (_locker)
                {
                    List<Channel> channels;
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
                    _brokerage.EmitTick(new Data.Market.Tick
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
                _brokerage.EmitTick(new Data.Market.Tick
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

        /// <summary>
        /// Reads 2 byte short int from byte stream
        /// </summary>
        private ushort ReadShort(byte[] b, ref int offset)
        {
            ushort data = (ushort)(b[offset + 1] + (b[offset] << 8));
            offset += 2;
            return data;
        }

        /// <summary>
        /// Reads 4 byte int32 from byte stream
        /// </summary>
        private uint ReadInt(byte[] b, ref int offset)
        {
            uint data = BitConverter.ToUInt32(new byte[] { b[offset + 3], b[offset + 2], b[offset + 1], b[offset + 0] }, 0);
            offset += 4;
            return data;
        }

        /// <summary>
        /// Reads an ltp mode tick from raw binary data
        /// </summary>
        private Tick ReadLTP(byte[] b, ref int offset)
        {
            Tick tick = new Tick();
            tick.Mode = Constants.MODE_LTP;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            return tick;
        }

        /// <summary>
        /// Reads a index's quote mode tick from raw binary data
        /// </summary>
        private Tick ReadIndexQuote(byte[] b, ref int offset)
        {
            Tick tick = new Tick();
            tick.Mode = Constants.MODE_QUOTE;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;
            tick.Change = ReadInt(b, ref offset) / divisor;
            return tick;
        }

        private Tick ReadIndexFull(byte[] b, ref int offset)
        {
            Tick tick = new Tick();
            tick.Mode = Constants.MODE_FULL;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;
            tick.Change = ReadInt(b, ref offset) / divisor;
            uint time = ReadInt(b, ref offset);
            tick.Timestamp = Utils.UnixToDateTime(time);
            return tick;
        }

        /// <summary>
        /// Reads a quote mode tick from raw binary data
        /// </summary>
        private Tick ReadQuote(byte[] b, ref int offset)
        {
            Tick tick = new Tick
            {
                Mode = Constants.MODE_QUOTE,
                InstrumentToken = ReadInt(b, ref offset)
            };

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.LastQuantity = ReadInt(b, ref offset);
            tick.AveragePrice = ReadInt(b, ref offset) / divisor;
            tick.Volume = ReadInt(b, ref offset);
            tick.BuyQuantity = ReadInt(b, ref offset);
            tick.SellQuantity = ReadInt(b, ref offset);
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;

            return tick;
        }

        /// <summary>
        /// Reads a full mode tick from raw binary data
        /// </summary>
        private Tick ReadFull(byte[] b, ref int offset)
        {
            Tick tick = new Tick();
            tick.Mode = Constants.MODE_FULL;
            tick.InstrumentToken = ReadInt(b, ref offset);

            decimal divisor = (tick.InstrumentToken & 0xff) == 3 ? 10000000.0m : 100.0m;

            tick.Tradable = (tick.InstrumentToken & 0xff) != 9;
            tick.LastPrice = ReadInt(b, ref offset) / divisor;
            tick.LastQuantity = ReadInt(b, ref offset);
            tick.AveragePrice = ReadInt(b, ref offset) / divisor;
            tick.Volume = ReadInt(b, ref offset);
            tick.BuyQuantity = ReadInt(b, ref offset);
            tick.SellQuantity = ReadInt(b, ref offset);
            tick.Open = ReadInt(b, ref offset) / divisor;
            tick.High = ReadInt(b, ref offset) / divisor;
            tick.Low = ReadInt(b, ref offset) / divisor;
            tick.Close = ReadInt(b, ref offset) / divisor;

            // KiteConnect 3 fields
            tick.LastTradeTime = Utils.UnixToDateTime(ReadInt(b, ref offset));
            tick.OI = ReadInt(b, ref offset);
            tick.OIDayHigh = ReadInt(b, ref offset);
            tick.OIDayLow = ReadInt(b, ref offset);
            tick.Timestamp = Utils.UnixToDateTime(ReadInt(b, ref offset));


            tick.Bids = new DepthItem[5];
            for (int i = 0; i < 5; i++)
            {
                tick.Bids[i].Quantity = ReadInt(b, ref offset);
                tick.Bids[i].Price = ReadInt(b, ref offset) / divisor;
                tick.Bids[i].Orders = ReadShort(b, ref offset);
                offset += 2;
            }

            tick.Offers = new DepthItem[5];
            for (int i = 0; i < 5; i++)
            {
                tick.Offers[i].Quantity = ReadInt(b, ref offset);
                tick.Offers[i].Price = ReadInt(b, ref offset) / divisor;
                tick.Offers[i].Orders = ReadShort(b, ref offset);
                offset += 2;
            }
            return tick;
        }

        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            throw new NotImplementedException();
        }

        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            throw new NotImplementedException();
        }

        protected override string ChannelNameFromTickType(TickType tickType)
        {
            throw new NotImplementedException();
        }
    }
}