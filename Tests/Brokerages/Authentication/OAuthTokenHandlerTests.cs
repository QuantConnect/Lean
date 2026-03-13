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
using System.Net.Http;
using System.Threading;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Api;
using QuantConnect.Brokerages.Authentication;

namespace QuantConnect.Tests.Brokerages.Authentication
{
    [TestFixture]
    public class LeanOAuthTokenHandlerTests
    {
        // CharlesSchwab: no tokenType field in the response body — relies on the TokenType.Bearer default.
        [Test]
        public void GetAccessTokenCharlesSchwabStyleResponseReturnsBearerCredentials()
        {
            const string responseJson = "{\"accessToken\":\"cs-token-xyz\",\"refreshToken\":\"\",\"success\":true}";

            using var apiClient = new FakeApiConnection(responseJson);
            var request = new OAuthTokenRequest("charlesschwab", "ACC123", deployId: "deploy-1");
            using var handler = new LeanOAuthTokenHandler(apiClient, request, TimeSpan.FromMinutes(30));

            var credentials = handler.GetAccessToken(CancellationToken.None);

            Assert.AreEqual(TokenType.Bearer, credentials.TokenType);
            Assert.AreEqual("cs-token-xyz", credentials.AccessToken);
        }

        // Tastytrade: response includes tokenType, expiresIn and tokenId — all extra fields ignored by the handler.
        [Test]
        public void GetAccessTokenTastytradeStyleResponseReturnsBearerCredentials()
        {
            const string responseJson = "{\"accessToken\":\"tt-token-abc\",\"tokenType\":\"Bearer\",\"expiresIn\":900,\"tokenId\":\"id-1\",\"success\":true}";

            using var apiClient = new FakeApiConnection(responseJson);
            var request = new OAuthTokenRequest("tastytrade", "ACC456", refreshToken: "rt-token");
            using var handler = new LeanOAuthTokenHandler(apiClient, request, TimeSpan.FromMinutes(15));

            var credentials = handler.GetAccessToken(CancellationToken.None);

            Assert.AreEqual(TokenType.Bearer, credentials.TokenType);
            Assert.AreEqual("tt-token-abc", credentials.AccessToken);
        }

        /// <summary>
        /// Intercepts <see cref="ApiConnection.TryRequest{T}"/> to return a pre-configured JSON body
        /// without sending any real HTTP requests.
        /// </summary>
        private sealed class FakeApiConnection : ApiConnection
        {
            private readonly string _responseJson;

            private static readonly JsonSerializerSettings _deserializeSettings = new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            public FakeApiConnection(string responseJson)
                : base(0, "fake-token", "https://fake.example.com/")
            {
                _responseJson = responseJson;
            }

            public override bool TryRequest<T>(HttpRequestMessage request, out T result,
                TimeSpan? timeout = null)
            {
                result = JsonConvert.DeserializeObject<T>(_responseJson, _deserializeSettings);
                return result?.Success ?? false;
            }
        }
    }
}
