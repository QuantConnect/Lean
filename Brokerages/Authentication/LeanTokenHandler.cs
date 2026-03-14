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
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Provides base functionality for token-based HTTP request handling.
    /// Token acquisition and retry logic are delegated entirely to <see cref="GetAccessToken"/>,
    /// implemented by derived classes (e.g., <see cref="LeanOAuthTokenHandler"/>).
    /// </summary>
    public abstract class LeanTokenHandler<T> : DelegatingHandler
        where T : LeanTokenCredentials
    {
        /// <summary>
        /// Raised when authentication fails after all retry attempts are exhausted.
        /// Subscribers can use this to trigger graceful application shutdown.
        /// </summary>
        public event EventHandler<Exception> AuthenticationFailed;

        /// <summary>
        /// A delegate used to construct an <see cref="AuthenticationHeaderValue"/> from a token type and access token string.
        /// </summary>
        private readonly Func<TokenType, string, AuthenticationHeaderValue> _createAuthHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanTokenHandler"/> class.
        /// </summary>
        /// <param name="createAuthHeader">
        /// An optional delegate for creating an <see cref="AuthenticationHeaderValue"/>
        /// from the token type and access token. If not provided, a default implementation is used.
        /// </param>
        protected LeanTokenHandler(Func<TokenType, string, AuthenticationHeaderValue> createAuthHeader = null, HttpClientHandler handler = null)
            : base(handler ?? new HttpClientHandler())
        {
            _createAuthHeader = createAuthHeader ?? ((tokenType, accessToken) => new AuthenticationHeaderValue(tokenType.ToString(), accessToken));
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
        public abstract T GetAccessToken(CancellationToken cancellationToken);

        /// <summary>
        /// Invokes the <see cref="AuthenticationFailed"/> event.
        /// Derived classes call this when authentication fails after exhausting all retry attempts.
        /// </summary>
        /// <param name="exception">The exception that caused the authentication failure.</param>
        protected void OnAuthenticationFailed(Exception exception)
        {
            AuthenticationFailed?.Invoke(this, exception);
        }

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
        /// Sends an HTTP request synchronously, applying token-based authentication.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The HTTP response message.</returns>
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = GetAccessToken(cancellationToken);
            request.Headers.Authorization = _createAuthHeader(accessToken.TokenType, accessToken.AccessToken);
            return base.Send(request, cancellationToken);
        }
    }
}
