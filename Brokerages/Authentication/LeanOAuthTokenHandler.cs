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
using QuantConnect.Api;
using System.Threading;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Handles OAuth token retrieval and caching by interacting with the Lean platform.
    /// Implements retry and expiration logic for secure HTTP communication.
    /// </summary>
    public sealed class LeanOAuthTokenHandler : TokenHandler
    {
        /// <summary>
        /// The maximum number of retry attempts when fetching an access token.
        /// </summary>
        private readonly int _maxRetryCount = 3;

        /// <summary>
        /// The time interval to wait between retry attempts when fetching an access token.
        /// </summary>
        private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Lock object used to synchronize token refresh across threads.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// The serialized JSON body representing the token request model.
        /// </summary>
        private readonly string _jsonBodyRequest;

        /// <summary>
        /// The total lifetime of a fetched token, used to compute the expiry timestamp.
        /// A 1-minute safety buffer is subtracted before the token is considered expired.
        /// </summary>
        private readonly TimeSpan _tokenLifetime;

        /// <summary>
        /// API client for communicating with the Lean platform.
        /// </summary>
        private readonly ApiConnection _apiClient;

        /// <summary>
        /// Stores the current access token and its type used for authenticating requests to the Lean platform.
        /// Written inside <see cref="_lock"/>; read outside the lock via volatile fast path.
        /// </summary>
        private volatile TokenCredentials _tokenCredentials;

        /// <summary>
        /// The UTC timestamp after which the cached token should be refreshed.
        /// Always written inside <see cref="_lock"/> before <see cref="_tokenCredentials"/> is set,
        /// so that a volatile read of <see cref="_tokenCredentials"/> guarantees visibility of this field.
        /// </summary>
        private DateTime _tokenExpiresAt;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanOAuthTokenHandler"/> class.
        /// </summary>
        /// <param name="apiClient">The API client used to communicate with the Lean platform.</param>
        /// <param name="request">The request model used to generate the access token.</param>
        /// <param name="tokenLifetime">
        /// The expected lifetime of a fetched token. A 1-minute safety buffer is applied before expiry.
        /// Must be provided explicitly — each brokerage has a different token lifetime.
        /// </param>
        public LeanOAuthTokenHandler(ApiConnection apiClient, OAuthTokenRequest request,
            TimeSpan tokenLifetime)
        {
            _apiClient = apiClient;
            _jsonBodyRequest = request.ToJson();
            _tokenLifetime = tokenLifetime;
        }

        /// <summary>
        /// Retrieves a valid access token from the Lean platform.
        /// Caches and reuses tokens until expiration to minimize unnecessary requests.
        /// Retries up to <see cref="_maxRetryCount"/> times on failure, and is thread-safe via double-checked locking.
        /// </summary>
        /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
        /// <returns>A <see cref="TokenCredentials"/> containing the token type and access token string.</returns>
        public override TokenCredentials GetAccessToken(CancellationToken cancellationToken)
        {
            // Fast path: return cached token without acquiring the lock
            if (_tokenCredentials != null && DateTime.UtcNow < _tokenExpiresAt)
            {
                return _tokenCredentials;
            }

            lock (_lock)
            {
                // Second check: another thread may have refreshed while we waited for the lock
                if (_tokenCredentials != null && DateTime.UtcNow < _tokenExpiresAt)
                {
                    return _tokenCredentials;
                }

                for (var retryCount = 0; retryCount <= _maxRetryCount; retryCount++)
                {
                    try
                    {
                        using var request = ApiUtils.CreateJsonPostRequest("live/auth0/refresh", _jsonBodyRequest);

                        if (_apiClient.TryRequest<OAuthTokenResponse>(request, out var response))
                        {
                            if (response.Success && !string.IsNullOrEmpty(response.AccessToken))
                            {
                                // Write expiry before credentials — the volatile write of _tokenCredentials
                                // acts as a release fence, ensuring the fast-path reader sees _tokenExpiresAt.
                                _tokenExpiresAt = DateTime.UtcNow + _tokenLifetime - TimeSpan.FromMinutes(1);
                                _tokenCredentials = new(response.TokenType, response.AccessToken);
                                return _tokenCredentials;
                            }
                        }

                        Logging.Log.Error($"{nameof(LeanOAuthTokenHandler)}.{nameof(GetAccessToken)}: Failed to retrieve access token. Response: {response}. Last known expiry: {_tokenExpiresAt.ToStringInvariant()}.");
                        throw new InvalidOperationException($"Authentication failed. " +
                            $"Details: {(response?.Errors?.Count > 0 ? string.Join(",", response.Errors) : "empty")}");
                    }
                    catch when (retryCount < _maxRetryCount)
                    {
                        if (cancellationToken.WaitHandle.WaitOne(_retryInterval))
                        {
                            throw new OperationCanceledException(
                                $"{nameof(LeanOAuthTokenHandler)}.{nameof(GetAccessToken)}: Token fetch canceled during wait.",
                                cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnAuthenticationFailed(ex);
                        throw;
                    }
                }

                // Unreachable — the loop always returns or throws
                throw new InvalidOperationException($"{nameof(LeanOAuthTokenHandler)}.{nameof(GetAccessToken)}: Unexpected state in token retry loop.");
            }
        }
    }
}
