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

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Orders;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class ReadOrdersTests
    {
        private StubApiServer _server;
        private Api.Api _apiClient;

        [SetUp]
        public void SetUp()
        {
            _server = new StubApiServer();
            _apiClient = new StubbedApi(_server.BaseUrl);
            _apiClient.Initialize(123, "token", "");
        }

        [TearDown]
        public void TearDown()
        {
            _apiClient.DisposeSafely();
            _server.DisposeSafely();
        }

        [Test]
        public void ReadBacktestOrdersThrowsWhenWindowIsTooLarge()
        {
            Assert.Throws<ArgumentException>(() => _apiClient.ReadBacktestOrders(1, "id", 0, 101));
            Assert.IsNull(_server.LastRequestBody);
        }

        [Test]
        public void ReadLiveOrdersThrowsWhenWindowIsTooLarge()
        {
            Assert.Throws<ArgumentException>(() => _apiClient.ReadLiveOrders(1, 0, 101));
            Assert.IsNull(_server.LastRequestBody);
        }

        [Test]
        public void ReadBacktestOrdersAcceptsMaximumWindow()
        {
            Assert.That(() => _apiClient.ReadBacktestOrders(1, "id", 0, 100), Throws.Nothing);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(0, payload["start"].Value<int>());
            Assert.AreEqual(100, payload["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersAcceptsMaximumWindow()
        {
            Assert.That(() => _apiClient.ReadLiveOrders(1, 0, 100), Throws.Nothing);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(0, payload["start"].Value<int>());
            Assert.AreEqual(100, payload["end"].Value<int>());
        }

        [Test]
        public void ReadBacktestOrdersDefaultsEndToStartPlusOneHundred()
        {
            _apiClient.ReadBacktestOrders(1, "id", 500);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(500, payload["start"].Value<int>());
            Assert.AreEqual(600, payload["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersDefaultsEndToStartPlusOneHundred()
        {
            _apiClient.ReadLiveOrders(1, 500);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(500, payload["start"].Value<int>());
            Assert.AreEqual(600, payload["end"].Value<int>());
        }

        [Test]
        public void ReadBacktestOrdersReturnsTheTotalOrderCount()
        {
            var response = _apiClient.ReadBacktestOrders(1, "id");

            Assert.AreEqual(1234, response.Length);
            Assert.IsEmpty(response.Orders);
        }

        private class StubbedApi : Api.Api
        {
            private readonly string _baseUrl;

            public StubbedApi(string baseUrl)
            {
                _baseUrl = baseUrl;
            }

            protected override ApiConnection CreateApiConnection(int userId, string token)
            {
                return new ApiConnection(userId, token, _baseUrl);
            }
        }

        /// <summary>
        /// Local HTTP server that captures the request body and replies with a canned orders response
        /// </summary>
        private class StubApiServer : IDisposable
        {
            private readonly HttpListener _listener;
            private readonly Thread _thread;

            public string BaseUrl { get; }

            public string LastRequestBody { get; private set; }

            public StubApiServer()
            {
                BaseUrl = $"http://localhost:{GetAvailablePort()}/";
                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();

                _thread = new Thread(Listen) { IsBackground = true };
                _thread.Start();
            }

            public void Dispose()
            {
                _listener.Stop();
                _listener.Close();
                _thread.Join(TimeSpan.FromSeconds(5));
            }

            private void Listen()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = _listener.GetContext();
                    }
                    catch (Exception)
                    {
                        // the listener was stopped
                        return;
                    }

                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    {
                        LastRequestBody = reader.ReadToEnd();
                    }

                    var buffer = Encoding.UTF8.GetBytes(EmptyOrdersResponse);
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.Close();
                }
            }

            private static int GetAvailablePort()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
        }

        private const string EmptyOrdersResponse = @"{ ""orders"": [], ""length"": 1234, ""success"": true }";
    }
}
