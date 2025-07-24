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

using System.Net;
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Tests.Brokerages.Authentication
{
    [TestFixture]
    public class TokenHandlerTests
    {
        [Test]
        public async Task TokenHandlerRetriesOnUnauthorizedThenReturnsSuccess()
        {
            var responseSequence = new Queue<HttpResponseMessage>(
            [
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.OK)
            ]);

            using var innerHandler = new MockHttpMessageHandler(responseSequence);
            using var testTokenHandler = new TestTokenHandler(innerHandler);
            using var httpClient = new HttpClient(testTokenHandler);

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(3, innerHandler.RequestCount);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;
            public int RequestCount { get; private set; }

            public MockHttpMessageHandler(Queue<HttpResponseMessage> responses)
            {
                _responses = responses;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(Send(request, cancellationToken));
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;

                // Check if the Authorization header is set
                Assert.IsTrue(request.Headers.Authorization != null);
                Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
                Assert.AreEqual("123456", request.Headers.Authorization.Parameter);

                return _responses.Dequeue();
            }
        }

        private class TestTokenHandler : TokenHandler
        {
            public TestTokenHandler(HttpMessageHandler innerHandler)
            {
                InnerHandler = innerHandler;
            }

            public override TokenCredentials GetAccessToken(CancellationToken cancellationToken)
            {
                return new TokenCredentials(TokenType.Bearer, "123456");
            }
        }
    }
}
