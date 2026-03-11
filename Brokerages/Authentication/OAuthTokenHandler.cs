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
    /// <typeparam name="TRequest">The request type used to acquire the access token.</typeparam>
    /// <typeparam name="TResponse">The response type containing access token metadata.</typeparam>
    public sealed class OAuthTokenHandler<TRequest, TResponse> : TokenHandler
        where TRequest : AccessTokenMetaDataRequest
        where TResponse : AccessTokenMetaDataResponse
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
        /// Stores metadata about the Lean access token and its expiration details.
        /// Written inside <see cref="_lock"/>; read outside the lock via volatile fast path.
        /// </summary>
        private volatile TResponse _accessTokenMetaData;

        /// <summary>
        /// API client for communicating with the Lean platform.
        /// </summary>
        private readonly ApiConnection _apiClient;

        /// <summary>
        /// Stores the current access token and its type used for authenticating requests to the Lean platform.
        /// </summary>
        private TokenCredentials _tokenCredentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthTokenHandler{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="apiClient">The API client used to communicate with the Lean platform.</param>
        /// <param name="modelRequest">The request model used to generate the access token.</param>
        public OAuthTokenHandler(ApiConnection apiClient, TRequest modelRequest)
        {
            _apiClient = apiClient;
            _jsonBodyRequest = modelRequest.ToJson();
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
            if (_accessTokenMetaData != null && DateTime.UtcNow < _accessTokenMetaData.Expiration)
            {
                return _tokenCredentials;
            }

            lock (_lock)
            {
                // Second check: another thread may have refreshed while we waited for the lock
                if (_accessTokenMetaData != null && DateTime.UtcNow < _accessTokenMetaData.Expiration)
                {
                    return _tokenCredentials;
                }

                for (var retryCount = 0; retryCount <= _maxRetryCount; retryCount++)
                {
                    try
                    {
                        using var request = ApiUtils.CreateJsonPostRequest("live/auth0/refresh", _jsonBodyRequest);

                        if (_apiClient.TryRequest<TResponse>(request, out var response))
                        {
                            if (response.Success && !string.IsNullOrEmpty(response.AccessToken))
                            {
                                _accessTokenMetaData = response;
                                _tokenCredentials = new(response.TokenType, response.AccessToken);
                                return _tokenCredentials;
                            }
                        }

                        Logging.Log.Error($"{nameof(OAuthTokenHandler<TRequest, TResponse>)}.{nameof(GetAccessToken)}: Failed to retrieve access token. Response: {response}. Last known expiration: {_accessTokenMetaData?.Expiration.ToStringInvariant() ?? "Not requested yet"}.");
                        throw new InvalidOperationException($"Authentication failed. " +
                            $"Details: {(response?.Errors?.Count > 0 ? string.Join(",", response.Errors) : "empty")}");
                    }
                    catch when (retryCount < _maxRetryCount)
                    {
                        if (cancellationToken.WaitHandle.WaitOne(_retryInterval))
                        {
                            throw new OperationCanceledException(
                                $"{nameof(OAuthTokenHandler<TRequest, TResponse>)}.{nameof(GetAccessToken)}: Token fetch canceled during wait.",
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
                throw new InvalidOperationException($"{nameof(OAuthTokenHandler<TRequest, TResponse>)}.{nameof(GetAccessToken)}: Unexpected state in token retry loop.");
            }
        }
    }
}
