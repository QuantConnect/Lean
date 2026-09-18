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

namespace QuantConnect.Tests.API
{
    internal sealed class CapturedRequest
    {
        public string Path { get; set; }
        public JObject Body { get; set; }
    }

    /// <summary>
    /// Minimal loopback http endpoint that records the requests the api client sends and replies with a canned body
    /// </summary>
    internal sealed class StubApiServer : IDisposable
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

    internal sealed class StubbedApi : Api.Api
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
