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
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class BrokerageMultiWebSocketSubscriptionManagerTests
    {
        private const int SymbolCount = 20;
        private static readonly TimeSpan SubscribeDelay = TimeSpan.FromMilliseconds(50);

        private HttpListener _server;
        private string _webSocketUrl;
        private ConcurrentDictionary<Symbol, int> _subscribeCalls;
        private List<TestWebSocketClientWrapper> _webSockets;
        private BrokerageMultiWebSocketSubscriptionManager _subscriptionManager;

        [SetUp]
        public void SetUp()
        {
            var port = GetFreePort();
            _webSocketUrl = $"ws://127.0.0.1:{port}/";
            _server = new HttpListener();
            _server.Prefixes.Add($"http://127.0.0.1:{port}/");
            _server.Start();
            Task.Run(AcceptWebSocketConnections);

            _subscribeCalls = new ConcurrentDictionary<Symbol, int>();
            _webSockets = new List<TestWebSocketClientWrapper>();
            _subscriptionManager = new BrokerageMultiWebSocketSubscriptionManager(
                _webSocketUrl,
                SymbolCount,
                0,
                null,
                () =>
                {
                    var webSocket = new TestWebSocketClientWrapper();
                    _webSockets.Add(webSocket);
                    return webSocket;
                },
                (_, symbol) =>
                {
                    _subscribeCalls.AddOrUpdate(symbol, 1, (_, count) => count + 1);
                    Thread.Sleep(SubscribeDelay);
                    return true;
                },
                (_, _) => true,
                _ => { },
                TimeSpan.Zero);

            for (var i = 0; i < SymbolCount; i++)
            {
                var symbol = Symbol.Create($"SYMBOL{i}USDT", SecurityType.Crypto, Market.Binance);
                _subscriptionManager.Subscribe(new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute,
                    TimeZones.Utc, TimeZones.Utc, true, true, false));
            }
        }

        [TearDown]
        public void TearDown()
        {
            _subscriptionManager.Dispose();
            _server.Close();
        }

        [Test]
        public void ReconnectionCancelsResubscriptionInProgress()
        {
            Assert.AreEqual(1, _webSockets.Count);
            Assert.AreEqual(SymbolCount, _subscribeCalls.Count);
            Assert.IsTrue(_subscribeCalls.Values.All(count => count == 1));

            var webSocket = _webSockets.Single();
            for (var i = 0; i < 4; i++)
            {
                webSocket.RaiseOpen();
                Thread.Sleep(SubscribeDelay * SymbolCount / 5);
            }

            _subscribeCalls.Clear();
            webSocket.RaiseOpen();
            Thread.Sleep(SubscribeDelay * SymbolCount * 3);

            // the latest resubscription subscribes every symbol once, the cancelled ones only finish the call they were in
            Assert.AreEqual(SymbolCount, _subscribeCalls.Count);
            Assert.LessOrEqual(_subscribeCalls.Values.Sum(), SymbolCount + 1);
        }

        [Test]
        public void DisposeCancelsResubscriptionInProgress()
        {
            _subscribeCalls.Clear();
            _webSockets.Single().RaiseOpen();
            Thread.Sleep(SubscribeDelay * SymbolCount / 5);

            _subscriptionManager.Dispose();
            Thread.Sleep(SubscribeDelay * SymbolCount * 2);

            Assert.Less(_subscribeCalls.Values.Sum(), SymbolCount);
        }

        private async Task AcceptWebSocketConnections()
        {
            while (_server.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _server.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var webSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
                        var buffer = new byte[1024];
                        while (webSocket.State == WebSocketState.Open)
                        {
                            await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        }
                    }
                    catch (Exception)
                    {
                        // the client went away
                    }
                });
            }
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private class TestWebSocketClientWrapper : WebSocketClientWrapper
        {
            public void RaiseOpen()
            {
                OnOpen();
            }
        }
    }
}
