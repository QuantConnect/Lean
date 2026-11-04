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
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class WebSocketClientWrapperTests
    {
        [Test]
        public void CloseWhileTheOpenHandlerRunsClosesTheConnection()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            using var server = new HttpListener();
            server.Prefixes.Add($"http://127.0.0.1:{port}/");
            server.Start();
            var serverConnectionEnded = Task.Run(async () =>
            {
                var context = await server.GetContextAsync();
                var webSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
                try
                {
                    await webSocket.ReceiveAsync(new byte[1024], CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    // the client aborted the connection
                }
            });

            var webSocketClient = new WebSocketClientWrapper();
            webSocketClient.Initialize($"ws://127.0.0.1:{port}/");
            using var opened = new ManualResetEventSlim();
            using var closing = new ManualResetEventSlim();
            webSocketClient.Open += (_, _) =>
            {
                opened.Set();
                // hold the open handler until Close() has cancelled the connection loop, before the first receive
                closing.Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            };

            webSocketClient.Connect();
            Assert.IsTrue(opened.Wait(TimeSpan.FromSeconds(10)));

            closing.Set();
            webSocketClient.Close();

            Assert.IsFalse(webSocketClient.IsOpen);
            Assert.IsTrue(serverConnectionEnded.Wait(TimeSpan.FromSeconds(5)), "The connection is still open on the server after Close()");
        }
    }
}
