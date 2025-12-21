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
using RestSharp;
using QuantConnect.Api;
using System.Threading;
using QuantConnect.Util;

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
        /// The serialized JSON body representing the token request model.
        /// </summary>
        private readonly string _jsonBodyRequest;

        /// <summary>
        /// Stores metadata about the Lean access token and its expiration details.
        /// </summary>
        private TResponse _accessTokenMetaData;

        /// <summary>
        /// API client for communicating with the Lean platform.
        /// </summary>
        private readonly ApiConnection _apiClient;

        /// <summary>
        /// Stores the current access token and its type used for authenticating requests to the Lean platform.
        /// </summary>
        private TokenCredentials _tokenCredentials;

        private bool _disposed;

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
        /// </summary>
        /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
        /// <returns>A tuple containing the token type and access token string.</returns>
        public override TokenCredentials GetAccessToken(CancellationToken cancellationToken)
        {
            if (_accessTokenMetaData != null && DateTime.UtcNow < _accessTokenMetaData.Expiration)
            {
                return _tokenCredentials;
            }

            try
            {
                var request = new RestRequest("live/auth0/refresh", Method.POST);
                request.AddJsonBody(_jsonBodyRequest);

                if (_apiClient.TryRequest<TResponse>(request, out var response))
                {
                    if (response.Success && !string.IsNullOrEmpty(response.AccessToken))
                    {
                        _accessTokenMetaData = response;
                        _tokenCredentials = new(response.TokenType, response.AccessToken);
                        return _tokenCredentials;
                    }
                }

                throw new InvalidOperationException(string.Join(",", response.Errors));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{nameof(OAuthTokenHandler<TRequest, TResponse>)}.{nameof(GetAccessToken)}: {ex.Message}");
            }
        }

        /// <summary>
        /// Disposes of resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _apiClient?.DisposeSafely();
            }

            base.Dispose(disposing);
        }
    }
}
