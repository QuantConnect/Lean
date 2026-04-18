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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Provides base functionality for token-based HTTP request handling, 
    /// including automatic retries and token refresh on unauthorized responses.
    /// </summary>
    public abstract class TokenHandler : DelegatingHandler
    {
        /// <summary>
        /// The maximum number of retry attempts for an authenticated request.
        /// </summary>
        private readonly int _maxRetryCount = 3;

        /// <summary>
        /// The time interval to wait between retry attempts for an authenticated request.
        /// </summary>
        private readonly TimeSpan _retryInterval;

        /// <summary>
        /// A delegate used to construct an <see cref="AuthenticationHeaderValue"/> from a token type and access token string.
        /// </summary>
        private readonly Func<TokenType, string, AuthenticationHeaderValue> _createAuthHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenHandler"/> class.
        /// </summary>
        /// <param name="createAuthHeader">
        /// An optional delegate for creating an <see cref="AuthenticationHeaderValue"/> 
        /// from the token type and access token. If not provided, a default implementation is used.
        /// </param>
        /// <param name="retryInterval">
        /// An optional time interval to wait between retry attempts when fetching the token or retrying a failed request.
        /// If <c>null</c>, the default interval of 5 seconds is used.
        /// </param>
        protected TokenHandler(Func<TokenType, string, AuthenticationHeaderValue> createAuthHeader = null, TimeSpan? retryInterval = null)
            : base(new HttpClientHandler())
        {
            _createAuthHeader = createAuthHeader ?? ((tokenType, accessToken) => new AuthenticationHeaderValue(tokenType.ToString(), accessToken));
            _retryInterval = retryInterval ?? TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Retrieves a valid access token for authenticating HTTP requests.
        /// Must be implemented by derived classes to provide token type and value,
        /// with optional support for caching and refresh logic.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the token retrieval operation.</param>
        /// <returns>
        /// A <see cref="TokenCredentials"/> instance containing the token type and access token string.
        /// </returns>
        public abstract TokenCredentials GetAccessToken(CancellationToken cancellationToken);

        /// <summary>
        /// Sends an HTTP request asynchronously by internally invoking the synchronous <see cref="Send(HttpRequestMessage, CancellationToken)"/> method.
        /// This is useful for compatibility with components that require an asynchronous pipeline, even though the core logic is synchronous.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing the HTTP response message.
        /// </returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request, cancellationToken));
        }

        /// <summary>
        /// Sends an HTTP request synchronously with retry support.
        /// This override includes token-based authentication and refresh logic on 401 Unauthorized responses.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = default;

            for (var retryCount = 0; retryCount <= _maxRetryCount; retryCount++)
            {
                var accessToken = default(TokenCredentials);

                try
                {
                    accessToken = GetAccessToken(cancellationToken);
                }
                catch when (retryCount < _maxRetryCount)
                {
                    if (cancellationToken.WaitHandle.WaitOne(_retryInterval))
                    {
                        throw new OperationCanceledException($"{nameof(TokenHandler)}.{nameof(Send)}: Token fetch canceled during wait.", cancellationToken);
                    }
                    continue;
                }
                catch
                {
                    throw;
                }

                request.Headers.Authorization = _createAuthHeader(accessToken.TokenType, accessToken.AccessToken);

                response = base.Send(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    break;
                }

                if (cancellationToken.WaitHandle.WaitOne(_retryInterval))
                {
                    break;
                }
            }

            return response;
        }
    }
}
