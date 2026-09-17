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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Orders;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the order reading endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OrdersTests
    {
        private const string SuccessfulOrdersResponse = @"{
            ""orders"": [
                {
                    ""id"": 1,
                    ""contingentId"": 0,
                    ""brokerId"": [ ""1"" ],
                    ""symbol"": { ""value"": ""SPY"", ""id"": ""SPY R735QTJ8XC9X"", ""permtick"": ""SPY"" },
                    ""limitPrice"": 145.0,
                    ""stopPrice"": 144.0,
                    ""stopTriggered"": true,
                    ""price"": 144.5,
                    ""priceCurrency"": ""USD"",
                    ""time"": ""2013-10-07T13:31:00Z"",
                    ""createdTime"": ""2013-10-07T13:31:00Z"",
                    ""quantity"": 10.0,
                    ""type"": 3,
                    ""status"": 3,
                    ""tag"": ""stop limit"",
                    ""securityType"": 1,
                    ""direction"": 0,
                    ""value"": 1445.0,
                    ""events"": [
                        {
                            ""algorithmId"": ""1234"",
                            ""orderId"": 1,
                            ""orderEventId"": 1,
                            ""symbol"": ""SPY R735QTJ8XC9X"",
                            ""symbolValue"": ""SPY"",
                            ""time"": 1381152660.0,
                            ""status"": ""filled"",
                            ""fillPrice"": 144.5,
                            ""fillPriceCurrency"": ""USD"",
                            ""fillQuantity"": 10.0,
                            ""direction"": ""buy""
                        }
                    ]
                },
                {
                    ""id"": 2,
                    ""brokerId"": [ ""2"" ],
                    ""symbol"": { ""value"": ""SPY"", ""id"": ""SPY R735QTJ8XC9X"", ""permtick"": ""SPY"" },
                    ""limitPrice"": 146.0,
                    ""triggerPrice"": 145.5,
                    ""triggerTouched"": true,
                    ""price"": 145.5,
                    ""time"": ""2013-10-07T13:32:00Z"",
                    ""quantity"": 5.0,
                    ""type"": 7,
                    ""status"": 1,
                    ""securityType"": 1,
                    ""events"": []
                },
                {
                    ""id"": 3,
                    ""brokerId"": [ ""3"" ],
                    ""symbol"": { ""value"": ""SPY"", ""id"": ""SPY R735QTJ8XC9X"", ""permtick"": ""SPY"" },
                    ""stopPrice"": 143.0,
                    ""trailingAmount"": 0.05,
                    ""trailingPercentage"": true,
                    ""price"": 144.0,
                    ""time"": ""2013-10-07T13:33:00Z"",
                    ""quantity"": -5.0,
                    ""type"": 11,
                    ""status"": 1,
                    ""securityType"": 1,
                    ""events"": []
                }
            ],
            ""length"": 42,
            ""success"": true
        }";

        private const string UnsuccessfulOrdersResponse = @"{ ""success"": false, ""errors"": [ ""Backtest not found"" ] }";

        [Test]
        public void ReadBacktestOrdersSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", 10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/orders/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("26c7bb06b8487cff1c7b3c44652b30f1", request.Body["backtestId"].Value<string>());
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            api.ReadLiveOrders(23456789, "L-6e9d8a78f5af89d401f630585be90e43", 10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/orders/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("L-6e9d8a78f5af89d401f630585be90e43", request.Body["algorithmId"].Value<string>());
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersOmitsTheAlgorithmIdWhenNotProvided()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            api.ReadLiveOrders(23456789);

            Assert.IsNull(server.GetSingleRequest().Body["algorithmId"]);
        }

        [TestCase(0, 100)]
        [TestCase(250, 350)]
        public void ReadBacktestOrdersDefaultsTheEndIndexToAFullWindow(int start, int expectedEnd)
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", start);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(start, body["start"].Value<int>());
            Assert.AreEqual(expectedEnd, body["end"].Value<int>());
        }

        [TestCase(0, 100)]
        [TestCase(250, 350)]
        public void ReadLiveOrdersDefaultsTheEndIndexToAFullWindow(int start, int expectedEnd)
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            api.ReadLiveOrders(23456789, start: start);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(start, body["start"].Value<int>());
            Assert.AreEqual(expectedEnd, body["end"].Value<int>());
        }

        [Test]
        public void ReadBacktestOrdersRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", 0, 101));
        }

        [Test]
        public void ReadLiveOrdersRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadLiveOrders(23456789, start: 0, end: 101));
        }

        [Test]
        public void ReadBacktestOrdersExposesTheTotalOrderCount()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            var response = api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1");

            Assert.IsTrue(response.Success);
            Assert.AreEqual(42, response.Length);
            Assert.AreEqual(3, response.Orders.Count);
        }

        [Test]
        public void ReadLiveOrdersExposesTheTotalOrderCount()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            var response = api.ReadLiveOrders(23456789);

            Assert.IsTrue(response.Success);
            Assert.AreEqual(42, response.Length);
            Assert.AreEqual(3, response.Orders.Count);
        }

        [Test]
        public void ReadBacktestOrdersDeserializesTheDocumentedOrderFields()
        {
            using var server = new StubApiServer(SuccessfulOrdersResponse);
            using var api = server.CreateApi();

            var orders = api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1").Orders;

            var stopLimit = (StopLimitOrder)orders[0].Order;
            Assert.AreEqual(Symbols.SPY, orders[0].Symbol);
            Assert.AreEqual(145m, stopLimit.LimitPrice);
            Assert.AreEqual(144m, stopLimit.StopPrice);
            Assert.IsTrue(stopLimit.StopTriggered);
            Assert.AreEqual(1, orders[0].Events.Count);
            Assert.AreEqual(144.5m, orders[0].Events[0].FillPrice);

            var limitIfTouched = (LimitIfTouchedOrder)orders[1].Order;
            Assert.AreEqual(145.5m, limitIfTouched.TriggerPrice);
            Assert.IsTrue(limitIfTouched.TriggerTouched);

            var trailingStop = (TrailingStopOrder)orders[2].Order;
            Assert.AreEqual(0.05m, trailingStop.TrailingAmount);
            Assert.IsTrue(trailingStop.TrailingAsPercentage);
        }

        [Test]
        public void ReadBacktestOrdersThrowsOnAnUnsuccessfulResponse()
        {
            using var server = new StubApiServer(UnsuccessfulOrdersResponse);
            using var api = server.CreateApi();

            var exception = Assert.Throws<WebException>(() => api.ReadBacktestOrders(23456789, "26c7bb06b8487cff1c7b3c44652b30f1"));
            Assert.IsTrue(exception.Message.Contains("Backtest not found", StringComparison.InvariantCulture));
        }

        [Test]
        public void ReadLiveOrdersThrowsOnAnUnsuccessfulResponse()
        {
            using var server = new StubApiServer(UnsuccessfulOrdersResponse);
            using var api = server.CreateApi();

            Assert.Throws<WebException>(() => api.ReadLiveOrders(23456789));
        }

        [Test]
        public void ReadBacktestInsightsRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadBacktestInsights(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", 0, 101));
        }

        [Test]
        public void ReadLiveInsightsRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadLiveInsights(23456789, 0, 101));
        }

        [Test]
        public void ReadLiveLogsRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43", 0, 251));
        }

        [TestCase(0, 100)]
        [TestCase(250, 350)]
        public void ReadBacktestInsightsDefaultsTheEndIndexToAFullWindow(int start, int expectedEnd)
        {
            using var server = new StubApiServer(@"{ ""insights"": [], ""length"": 0, ""success"": true }");
            using var api = server.CreateApi();

            api.ReadBacktestInsights(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", start);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(start, body["start"].Value<int>());
            Assert.AreEqual(expectedEnd, body["end"].Value<int>());
        }

        [TestCase(0, 250)]
        [TestCase(500, 750)]
        public void ReadLiveLogsDefaultsTheEndLineToAFullWindow(int startLine, int expectedEndLine)
        {
            using var server = new StubApiServer(@"{ ""logs"": [], ""length"": 0, ""success"": true }");
            using var api = server.CreateApi();

            api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43", startLine);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(startLine, body["startLine"].Value<int>());
            Assert.AreEqual(expectedEndLine, body["endLine"].Value<int>());
        }

        private sealed class CapturedRequest
        {
            public string Path { get; set; }
            public JObject Body { get; set; }
        }

        /// <summary>
        /// Minimal loopback http endpoint that records the requests the api client sends and replies with a canned body
        /// </summary>
        private sealed class StubApiServer : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly byte[] _response;
            private readonly ConcurrentQueue<CapturedRequest> _requests;

            public string BaseUrl { get; }

            public StubApiServer(string responseBody)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _requests = new ConcurrentQueue<CapturedRequest>();
                var payload = Encoding.UTF8.GetBytes(responseBody);
                _response = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n" +
                    $"Content-Length: {payload.Length}\r\nConnection: close\r\n\r\n").Concat(payload).ToArray();

                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                BaseUrl = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
                Task.Run(() => Serve(_cancellationTokenSource.Token));
            }

            public Api.Api CreateApi()
            {
                var api = new StubbedApi(BaseUrl);
                api.Initialize(0, "token", Globals.DataFolder);
                return api;
            }

            public CapturedRequest GetSingleRequest()
            {
                Assert.AreEqual(1, _requests.Count, "Expected exactly one request to reach the stub server");
                _requests.TryPeek(out var request);
                return request;
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _listener.Stop();
                _cancellationTokenSource.Dispose();
            }

            private async Task Serve(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    using (client)
                    {
                        using var stream = client.GetStream();
                        var request = await ReadRequest(stream, cancellationToken).ConfigureAwait(false);
                        if (request != null)
                        {
                            _requests.Enqueue(request);
                        }
                        await stream.WriteAsync(_response, cancellationToken).ConfigureAwait(false);
                        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private static async Task<CapturedRequest> ReadRequest(Stream stream, CancellationToken cancellationToken)
            {
                var buffer = new byte[8192];
                var received = new List<byte>();
                var headerEnd = -1;
                while (headerEnd < 0)
                {
                    var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        return null;
                    }
                    received.AddRange(buffer.Take(read));
                    headerEnd = IndexOfHeaderEnd(received);
                }

                var header = Encoding.ASCII.GetString(received.ToArray(), 0, headerEnd);
                var body = received.Skip(headerEnd + 4).ToList();
                var contentLength = GetContentLength(header);
                while (body.Count < contentLength)
                {
                    var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }
                    body.AddRange(buffer.Take(read));
                }

                return new CapturedRequest
                {
                    Path = header.Split("\r\n")[0].Split(' ')[1],
                    Body = JObject.Parse(Encoding.UTF8.GetString(body.ToArray()))
                };
            }

            private static int IndexOfHeaderEnd(List<byte> received)
            {
                for (var i = 0; i + 3 < received.Count; i++)
                {
                    if (received[i] == '\r' && received[i + 1] == '\n' && received[i + 2] == '\r' && received[i + 3] == '\n')
                    {
                        return i;
                    }
                }
                return -1;
            }

            private static int GetContentLength(string header)
            {
                var line = header.Split("\r\n")
                    .FirstOrDefault(x => x.StartsWith("Content-Length:", StringComparison.InvariantCultureIgnoreCase));
                return line == null ? 0 : Parse.Int(line.Split(':')[1].Trim());
            }
        }

        private sealed class StubbedApi : Api.Api
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
    }
}
