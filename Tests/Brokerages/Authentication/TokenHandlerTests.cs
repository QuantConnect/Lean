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
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Tests.Brokerages.Authentication
{
    [TestFixture]
    public class TokenHandlerTests
    {
        [Test]
        public void TokenHandlerThrowsWhenGetAccessTokenAlwaysFails()
        {
            using var innerHandler = new CountingHandler(0, HttpStatusCode.OK);
            using var tokenHandler = new AlwaysFailingTokenHandler(innerHandler);
            using var client = new HttpClient(tokenHandler);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

            Assert.Throws<InvalidOperationException>(() => client.Send(request));
            Assert.AreEqual(0, innerHandler.CallCount);
        }

        [Test]
        public void TokenHandlerInvokesAuthenticationFailedEventOnFinalFailure()
        {
            using var innerHandler = new CountingHandler(0, HttpStatusCode.OK);
            using var tokenHandler = new AlwaysFailingTokenHandler(innerHandler);

            Exception capturedEx = null;
            tokenHandler.AuthenticationFailed += (_, ex) => capturedEx = ex;

            using var client = new HttpClient(tokenHandler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

            Assert.Throws<InvalidOperationException>(() => client.Send(request));
            Assert.IsNotNull(capturedEx);
            Assert.IsInstanceOf<InvalidOperationException>(capturedEx);
        }

        [Test]
        public void TokenHandlerSendsRequestWithValidToken()
        {
            using var innerHandler = new CountingHandler(0, HttpStatusCode.OK);
            using var tokenHandler = new ValidTokenHandler(innerHandler);
            using var client = new HttpClient(tokenHandler);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

            var response = client.Send(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, innerHandler.CallCount);
        }

        [Test]
        public async Task TokenHandlerSendsRequestWithValidTokenAsync()
        {
            using var innerHandler = new CountingHandler(0, HttpStatusCode.OK);
            using var tokenHandler = new ValidTokenHandler(innerHandler);
            using var client = new HttpClient(tokenHandler);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

            var response = await client.SendAsync(request).ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(1, innerHandler.CallCount);
        }

        private class CountingHandler : HttpMessageHandler
        {
            private readonly int _failCount;
            private readonly HttpStatusCode _failCode;
            private readonly HttpStatusCode _successCode;
            public int CallCount { get; private set; }

            public CountingHandler(int failCount, HttpStatusCode failCode, HttpStatusCode successCode = HttpStatusCode.OK)
            {
                _failCount = failCount;
                _failCode = failCode;
                _successCode = successCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Send(request, cancellationToken));
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                AssertAuthorization(request);

                return CallCount <= _failCount
                    ? new HttpResponseMessage(_failCode)
                    : new HttpResponseMessage(_successCode)
                    {
                        Content = new StringContent("success")
                    };
            }
        }

        private static void AssertAuthorization(HttpRequestMessage request)
        {
            Assert.IsNotNull(request.Headers.Authorization);
            Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
            Assert.AreEqual("123456", request.Headers.Authorization.Parameter);
        }

        private class ValidTokenHandler : LeanTokenHandler<LeanTokenCredentials>
        {
            public ValidTokenHandler(HttpMessageHandler innerHandler)
            {
                InnerHandler = innerHandler;
            }

            public override LeanTokenCredentials GetAccessToken(CancellationToken cancellationToken)
            {
                return new LeanTokenCredentials(TokenType.Bearer, "123456");
            }
        }

        private class AlwaysFailingTokenHandler : LeanTokenHandler<LeanTokenCredentials>
        {
            public AlwaysFailingTokenHandler(HttpMessageHandler innerHandler)
            {
                InnerHandler = innerHandler;
            }

            public override LeanTokenCredentials GetAccessToken(CancellationToken cancellationToken)
            {
                var exception = new InvalidOperationException("Simulated persistent token failure");
                OnAuthenticationFailed(exception);
                throw exception;
            }
        }
    }
}
