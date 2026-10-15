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
using System.Text;
using NUnit.Framework;
using Newtonsoft.Json;
using QuantConnect.Api;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Tests.Brokerages.Authentication
{
    [TestFixture]
    public class LeanOAuthTokenHandlerTests
    {
        [Test]
        public void RequestsANewTokenAfterTheBrokerageRejectsIt()
        {
            using var platform = new FakePlatform("first");
            using var tokenHandler = new LeanOAuthTokenHandler(platform.ApiClient, new OAuthTokenRequest("TestBrokerage", "account"), TimeSpan.FromHours(1));

            var token = tokenHandler.GetAccessToken(CancellationToken.None);
            Assert.AreEqual("first", token.AccessToken);
            Assert.AreSame(token, tokenHandler.GetAccessToken(CancellationToken.None));
            Assert.AreEqual(1, platform.Requests);

            platform.Token = "second";
            tokenHandler.InvalidateAccessToken();
            Assert.AreEqual("second", tokenHandler.GetAccessToken(CancellationToken.None).AccessToken);
            Assert.AreEqual(2, platform.Requests);
        }

        /// <summary>
        /// Serves the platform token refresh endpoint, always returning <see cref="Token"/>
        /// </summary>
        private sealed class FakePlatform : IDisposable
        {
            private readonly HttpListener _listener = new();
            private int _requests;

            public volatile string Token;

            public int Requests => Volatile.Read(ref _requests);

            public ApiConnection ApiClient { get; }

            public FakePlatform(string token)
            {
                Token = token;
                var url = $"http://localhost:{GetFreePort()}/";
                _listener.Prefixes.Add(url);
                _listener.Start();
                Task.Run(Serve);
                ApiClient = new ApiConnection(1, "token", url);
            }

            private async Task Serve()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // the listener was stopped
                        return;
                    }

                    if (context.Request.Url.AbsolutePath != "/live/auth0/refresh")
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                        continue;
                    }

                    Interlocked.Increment(ref _requests);
                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { success = true, tokenType = "Bearer", accessToken = Token }));
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
                    context.Response.Close();
                }
            }

            private static int GetFreePort()
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }

            public void Dispose()
            {
                _listener.Stop();
                _listener.Close();
                ApiClient.Dispose();
            }
        }
    }
}
