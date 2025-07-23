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
        private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(2);

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
        protected TokenHandler(Func<TokenType, string, AuthenticationHeaderValue> createAuthHeader = null)
            : base(new HttpClientHandler())
        {
            _createAuthHeader = createAuthHeader ?? ((tokenType, accessToken) => new AuthenticationHeaderValue(tokenType.ToString(), accessToken));
        }

        /// <summary>
        /// Retrieves a valid access token to use in the Authorization header.
        /// This method must be implemented by derived classes.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A tuple containing the token type and access token string.</returns>
        public abstract (TokenType TokenType, string AccessToken) GetAccessToken(CancellationToken cancellationToken);

        /// <summary>
        /// Sends an HTTP request asynchronously with retry support.
        /// This override includes token-based authentication and refresh logic on 401 Unauthorized responses.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation, containing the HTTP response.</returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
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
            return SendWithRetryAsync(request, cancellationToken).SynchronouslyAwaitTaskResult();
        }

        /// <summary>
        /// Sends the HTTP request with retry logic for unauthorized responses.
        /// If a 401 Unauthorized response is encountered, it refreshes the token and retries up to the maximum retry count.
        /// </summary>
        /// <param name="request">The outgoing HTTP request.</param>
        /// <param name="cancellationToken">A token to observe for cancellation.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = default;
            var accessToken = GetAccessToken(cancellationToken);

            for (var retryCount = 0; retryCount < _maxRetryCount; retryCount++)
            {
                request.Headers.Authorization = _createAuthHeader(accessToken.TokenType, accessToken.AccessToken);

                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    accessToken = GetAccessToken(cancellationToken);
                }
                else
                {
                    break;
                }

                // Wait for retry interval or cancellation request
                if (cancellationToken.WaitHandle.WaitOne(_retryInterval))
                {
                    break;
                }
            }

            return response;
        }
    }
}
