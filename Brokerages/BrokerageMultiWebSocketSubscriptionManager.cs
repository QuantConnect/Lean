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
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Handles brokerage data subscriptions with multiple websocket connections, with optional symbol weighting
    /// </summary>
    public class BrokerageMultiWebSocketSubscriptionManager : EventBasedDataQueueHandlerSubscriptionManager, IDisposable
    {
        private readonly string _webSocketUrl;
        private readonly int _maximumSymbolsPerWebSocket;
        private readonly int _maximumWebSocketConnections;
        private readonly Func<WebSocketClientWrapper> _webSocketFactory;
        private readonly Func<IWebSocket, Symbol, bool> _subscribeFunc;
        private readonly Func<IWebSocket, Symbol, bool> _unsubscribeFunc;
        private readonly Action<WebSocketMessage> _messageHandler;
        private readonly RateGate _connectionRateLimiter;
        private readonly System.Timers.Timer _reconnectTimer;

        private const int ConnectionTimeout = 30000;

        protected readonly object _locker = new();
        protected readonly List<BrokerageMultiWebSocketEntry> _webSocketEntries = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerageMultiWebSocketSubscriptionManager"/> class
        /// </summary>
        /// <param name="webSocketUrl">The URL for websocket connections</param>
        /// <param name="maximumSymbolsPerWebSocket">The maximum number of symbols per websocket connection</param>
        /// <param name="maximumWebSocketConnections">The maximum number of websocket connections allowed (if zero, symbol weighting is disabled)</param>
        /// <param name="symbolWeights">A dictionary for the symbol weights</param>
        /// <param name="webSocketFactory">A function which returns a new websocket instance</param>
        /// <param name="subscribeFunc">A function which subscribes a symbol</param>
        /// <param name="unsubscribeFunc">A function which unsubscribes a symbol</param>
        /// <param name="messageHandler">The websocket message handler</param>
        /// <param name="webSocketConnectionDuration">The maximum duration of the websocket connection, TimeSpan.Zero for no duration limit</param>
        /// <param name="connectionRateLimiter">The rate limiter for creating new websocket connections</param>
        public BrokerageMultiWebSocketSubscriptionManager(
            string webSocketUrl,
            int maximumSymbolsPerWebSocket,
            int maximumWebSocketConnections,
            Dictionary<Symbol, int> symbolWeights,
            Func<WebSocketClientWrapper> webSocketFactory,
            Func<IWebSocket, Symbol, bool> subscribeFunc,
            Func<IWebSocket, Symbol, bool> unsubscribeFunc,
            Action<WebSocketMessage> messageHandler,
            TimeSpan webSocketConnectionDuration,
            RateGate connectionRateLimiter = null)
        {
            _webSocketUrl = webSocketUrl;
            _maximumSymbolsPerWebSocket = maximumSymbolsPerWebSocket;
            _maximumWebSocketConnections = maximumWebSocketConnections;
            _webSocketFactory = webSocketFactory;
            _subscribeFunc = subscribeFunc;
            _unsubscribeFunc = unsubscribeFunc;
            _messageHandler = messageHandler;
            _connectionRateLimiter = connectionRateLimiter;

            if (_maximumWebSocketConnections > 0)
            {
                // symbol weighting enabled, create all websocket instances
                for (var i = 0; i < _maximumWebSocketConnections; i++)
                {
                    var webSocket = CreateWebSocket();

                    _webSocketEntries.Add(new BrokerageMultiWebSocketEntry(symbolWeights, webSocket));
                }
            }

            // Some exchanges (e.g. Binance) require a daily restart for websocket connections
            if (webSocketConnectionDuration != TimeSpan.Zero)
            {
                _reconnectTimer = new System.Timers.Timer
                {
                    Interval = webSocketConnectionDuration.TotalMilliseconds
                };
                _reconnectTimer.Elapsed += (_, _) =>
                {
                    Log.Trace("BrokerageMultiWebSocketSubscriptionManager(): Restarting websocket connections");

                    lock (_locker)
                    {
                        foreach (var entry in _webSocketEntries)
                        {
                            if (entry.WebSocket.IsOpen)
                            {
                                Task.Factory.StartNew(() =>
                                {
                                    Log.Trace($"BrokerageMultiWebSocketSubscriptionManager(): Websocket restart - disconnect: ({entry.WebSocket.GetHashCode()})");
                                    Disconnect(entry.WebSocket);

                                    Log.Trace($"BrokerageMultiWebSocketSubscriptionManager(): Websocket restart - connect: ({entry.WebSocket.GetHashCode()})");
                                    Connect(entry.WebSocket);
                                });
                            }
                        }
                    }
                };
                _reconnectTimer.Start();

                Log.Trace($"BrokerageMultiWebSocketSubscriptionManager(): WebSocket connections will be restarted every: {webSocketConnectionDuration}");
            }
        }

        /// <summary>
        /// Subscribes to the symbols
        /// </summary>
        /// <param name="symbols">Symbols to subscribe</param>
        /// <param name="tickType">Type of tick data</param>
        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            Log.Trace($"BrokerageMultiWebSocketSubscriptionManager.Subscribe(): {string.Join(",", symbols.Select(x => x.Value))}");

            var success = true;

            foreach (var symbol in symbols)
            {
                var webSocket = GetWebSocketForSymbol(symbol);

                success &= _subscribeFunc(webSocket, symbol);
            }

            return success;
        }

        /// <summary>
        /// Unsubscribes from the symbols
        /// </summary>
        /// <param name="symbols">Symbols to subscribe</param>
        /// <param name="tickType">Type of tick data</param>
        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            Log.Trace($"BrokerageMultiWebSocketSubscriptionManager.Unsubscribe(): {string.Join(",", symbols.Select(x => x.Value))}");

            var success = true;

            foreach (var symbol in symbols)
            {
                var entry = GetWebSocketEntryBySymbol(symbol);
                if (entry != null)
                {
                    entry.RemoveSymbol(symbol);

                    success &= _unsubscribeFunc(entry.WebSocket, symbol);
                }
            }

            return success;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _reconnectTimer?.Stop();
            _reconnectTimer.DisposeSafely();
            lock (_locker)
            {
                foreach (var entry in _webSocketEntries)
                {
                    try
                    {
                        entry.WebSocket.Open -= OnOpen;
                        entry.WebSocket.Message -= EventHandler;
                        entry.WebSocket.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
                _webSocketEntries.Clear();
            }
        }

        protected virtual BrokerageMultiWebSocketEntry GetWebSocketEntryBySymbol(Symbol symbol)
        {
            lock (_locker)
            {
                foreach (var entry in _webSocketEntries.Where(entry => entry.Contains(symbol)))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a symbol to an existing or new websocket connection
        /// </summary>
        protected virtual IWebSocket GetWebSocketForSymbol(Symbol symbol)
        {
            BrokerageMultiWebSocketEntry entry;

            lock (_locker)
            {
                if (_webSocketEntries.All(x => x.SymbolCount >= _maximumSymbolsPerWebSocket))
                {
                    if (_maximumWebSocketConnections > 0)
                    {
                        throw new NotSupportedException($"Maximum symbol count reached for the current configuration [MaxSymbolsPerWebSocket={_maximumSymbolsPerWebSocket}, MaxWebSocketConnections:{_maximumWebSocketConnections}]");
                    }

                    // symbol limit reached on all, create new websocket instance
                    var webSocket = CreateWebSocket();

                    _webSocketEntries.Add(new BrokerageMultiWebSocketEntry(webSocket));
                }

                // sort by weight ascending, taking into account the symbol limit per websocket
                _webSocketEntries.Sort((x, y) =>
                    x.SymbolCount >= _maximumSymbolsPerWebSocket
                    ? 1
                    : y.SymbolCount >= _maximumSymbolsPerWebSocket
                        ? -1
                        : Math.Sign(x.TotalWeight - y.TotalWeight));

                entry = _webSocketEntries.First();
            }

            if (!entry.WebSocket.IsOpen)
            {
                Connect(entry.WebSocket);
            }

            entry.AddSymbol(symbol);

            Log.Trace($"BrokerageMultiWebSocketSubscriptionManager.GetWebSocketForSymbol(): added symbol: {symbol} to websocket: {entry.WebSocket.GetHashCode()} - Count: {entry.SymbolCount}");

            return entry.WebSocket;
        }

        /// <summary>
        /// When we create a websocket we will subscribe to it's events once and initialize it
        /// </summary>
        /// <remarks>Note that the websocket is no connected yet <see cref="Connect(IWebSocket)"/></remarks>
        private IWebSocket CreateWebSocket()
        {
            var webSocket = _webSocketFactory();
            webSocket.Open += OnOpen;
            webSocket.Message += EventHandler;
            webSocket.Initialize(_webSocketUrl);

            return webSocket;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EventHandler(object _, WebSocketMessage message)
        {
            _messageHandler(message);
        }

        protected void Connect(IWebSocket webSocket)
        {
            var connectedEvent = new ManualResetEvent(false);
            EventHandler onOpenAction = (_, _) =>
            {
                connectedEvent.Set();
            };

            webSocket.Open += onOpenAction;

            if (_connectionRateLimiter is { IsRateLimited: false })
            {
                _connectionRateLimiter.WaitToProceed();
            }

            try
            {
                webSocket.Connect();

                if (!connectedEvent.WaitOne(ConnectionTimeout))
                {
                    throw new TimeoutException($"BrokerageMultiWebSocketSubscriptionManager.Connect(): WebSocket connection timeout: {webSocket.GetHashCode()}");
                }
            }
            finally
            {
                webSocket.Open -= onOpenAction;

                connectedEvent.DisposeSafely();
            }
        }

        protected void Disconnect(IWebSocket webSocket)
        {
            webSocket.Close();
        }

        protected void OnOpen(object sender, EventArgs e)
        {
            var webSocket = (IWebSocket)sender;

            lock (_locker)
            {
                foreach (var entry in _webSocketEntries)
                {
                    if (entry.WebSocket == webSocket && entry.Symbols.Count > 0)
                    {
                        Log.Trace($"BrokerageMultiWebSocketSubscriptionManager.Connect(): WebSocket opened: {webSocket.GetHashCode()} - Resubscribing existing symbols: {entry.Symbols.Count}");

                        Task.Factory.StartNew(() =>
                        {
                            foreach (var symbol in entry.Symbols)
                            {
                                _subscribeFunc(webSocket, symbol);
                            }
                        });
                    }
                }
            }
        }
    }
}
