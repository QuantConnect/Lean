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
using QuantConnect.Util;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Authentication
{
    /// <summary>
    /// Handles OAuth token retrieval and caching by interacting with the Lean platform.
    /// Implements retry and expiration logic for secure HTTP communication.
    /// </summary>
    public class LeanOAuthTokenHandler : LeanOAuthTokenHandler<LeanTokenCredentials>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeanOAuthTokenHandler"/> class with default token credentials type.
        /// </summary>
        /// <param name="apiClient">The API client used to communicate with the Lean platform.</param>
        /// <param name="request">The request model used to generate the access token.</param>
        /// <param name="tokenLifetime">
        /// The expected lifetime of a fetched token. A 1-minute safety buffer is applied before expiry.
        /// Must be provided explicitly — each brokerage has a different token lifetime.
        /// </param>
        public LeanOAuthTokenHandler(ApiConnection apiClient, OAuthTokenRequest request, TimeSpan tokenLifetime)
            : base(apiClient, request, tokenLifetime)
        {
        }
    }

    /// <summary>
    /// Handles OAuth token retrieval and caching by interacting with the Lean platform.
    /// Implements retry and expiration logic for secure HTTP communication.
    /// </summary>
    public class LeanOAuthTokenHandler<T> : LeanTokenHandler<T>
        where T : LeanTokenCredentials
    {
        /// <summary>
        /// The maximum number of retry attempts when fetching an access token.
        /// </summary>
        protected virtual int MaxRetryCount { get; set; } = 5;

        /// <summary>
        /// The time interval to wait between retry attempts when fetching an access token.
        /// </summary>
        protected virtual TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// The time interval between attempts to recover a failed authentication.
        /// </summary>
        protected virtual TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Lock object used to synchronize token refresh across threads.
        /// </summary>
        private readonly Lock _lock = new();

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
        /// Always accessed inside <see cref="_lock"/>.
        /// </summary>
        private T _tokenCredentials;

        /// <summary>
        /// The UTC timestamp after which the cached token should be refreshed.
        /// Always accessed inside <see cref="_lock"/>.
        /// </summary>
        private DateTime _tokenExpiresAt;

        /// <summary>
        /// Whether the last authentication failed, while true the recovery task owns the retrying.
        /// Always accessed inside <see cref="_lock"/>.
        /// </summary>
        private bool _authenticationFailed;

        /// <summary>
        /// Cancels the recovery task, not null while one is running. Always accessed inside <see cref="_lock"/>.
        /// </summary>
        private CancellationTokenSource _recoveryCancellation;

        /// <summary>
        /// Some padding before expiration to request a new token
        /// </summary>
        public TimeSpan OffsetBeforeExpiration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Raised when authentication succeeds
        /// </summary>
        public event EventHandler AuthenticationSucceeded;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanOAuthTokenHandler"/> class.
        /// </summary>
        /// <param name="apiClient">The API client used to communicate with the Lean platform.</param>
        /// <param name="request">The request model used to generate the access token.</param>
        /// <param name="tokenLifetime">
        /// The expected lifetime of a fetched token. A 1-minute safety buffer is applied before expiry.
        /// Must be provided explicitly — each brokerage has a different token lifetime.
        /// </param>
        public LeanOAuthTokenHandler(ApiConnection apiClient, OAuthTokenRequest request, TimeSpan tokenLifetime)
        {
            _apiClient = apiClient;
            _jsonBodyRequest = request.ToJson();
            _tokenLifetime = tokenLifetime;
        }

        /// <summary>
        /// Retrieves a valid access token from the Lean platform.
        /// Caches and reuses tokens until expiration to minimize unnecessary requests.
        /// Retries up to <see cref="MaxRetryCount"/> times on failure. Thread-safe via a lock.
        /// </summary>
        /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
        /// <returns>A <see cref="LeanTokenCredentials"/> instance containing the token type and access token string.</returns>
        public override T GetAccessToken(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_tokenCredentials != null && DateTime.UtcNow < _tokenExpiresAt)
                {
                    return _tokenCredentials;
                }

                if (_authenticationFailed)
                {
                    // the recovery task is already retrying, fail fast instead of holding the lock for a token we know we can't get
                    throw new InvalidOperationException($"LeanOAuthTokenHandler.{nameof(GetAccessToken)}: Authentication failed, waiting for it to recover.");
                }

                for (var retryCount = 0; retryCount <= MaxRetryCount; retryCount++)
                {
                    try
                    {
                        return RequestAccessToken();
                    }
                    catch when (retryCount < MaxRetryCount)
                    {
                        if (cancellationToken.WaitHandle.WaitOne(RetryInterval))
                        {
                            throw new OperationCanceledException(
                                $"LeanOAuthTokenHandler.{nameof(GetAccessToken)}: Token fetch canceled during wait.",
                                cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _authenticationFailed = true;
                        StartRecovery();
                        OnAuthenticationFailed(ex);
                        throw;
                    }
                }

                // Unreachable — the loop always returns or throws
                throw new InvalidOperationException($"LeanOAuthTokenHandler.{nameof(GetAccessToken)}: Unexpected state in token retry loop.");
            }
        }

        /// <summary>
        /// Stops the recovery task, if any, before releasing the handler.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    StopRecovery();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Requests a new access token, caching it and reporting the recovery of a failed authentication.
        /// Always called inside <see cref="_lock"/>.
        /// </summary>
        /// <param name="logFailure">Whether to log a failed request, false for the recovery task so that a long
        /// outage does not fill the log with one error every <see cref="RecoveryInterval"/>.</param>
        /// <returns>A <see cref="LeanTokenCredentials"/> instance containing the token type and access token string.</returns>
        private T RequestAccessToken(bool logFailure = true)
        {
            using var request = ApiUtils.CreateJsonPostRequest("live/auth0/refresh", _jsonBodyRequest);

            if (_apiClient.TryRequest<T>(request, out var response) && response.Success)
            {
                _tokenExpiresAt = DateTime.UtcNow + _tokenLifetime - OffsetBeforeExpiration;
                _tokenCredentials = response;

                if (_authenticationFailed)
                {
                    _authenticationFailed = false;
                    StopRecovery();
                    AuthenticationSucceeded?.Invoke(this, EventArgs.Empty);
                }

                return response;
            }

            if (logFailure)
            {
                Logging.Log.Error($"LeanOAuthTokenHandler.{nameof(GetAccessToken)}: Failed to retrieve access token. Response: {response}. Last known expiry: {_tokenExpiresAt.ToStringInvariant()}.");
            }
            throw new InvalidOperationException($"Authentication failed. " +
                $"Details: {(response?.Errors?.Count > 0 ? string.Join(",", response.Errors) : "empty")}");
        }

        /// <summary>
        /// Starts the task that retries a failed authentication until it succeeds, so that recovering does not
        /// depend on the brokerage happening to request another token. Always called inside <see cref="_lock"/>.
        /// </summary>
        private void StartRecovery()
        {
            if (_recoveryCancellation != null)
            {
                return;
            }

            var recoveryCancellation = _recoveryCancellation = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // the task owns the disposal of its cancellation source, so it can never wait on a disposed handle
                    while (!recoveryCancellation.Token.WaitHandle.WaitOne(RecoveryInterval))
                    {
                        lock (_lock)
                        {
                            if (!_authenticationFailed)
                            {
                                break;
                            }

                            try
                            {
                                RequestAccessToken(logFailure: false);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Logging.Log.Debug($"LeanOAuthTokenHandler.{nameof(StartRecovery)}: {ex.Message}");
                            }
                        }
                    }
                }
                finally
                {
                    recoveryCancellation.DisposeSafely();
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Stops the recovery task, which disposes of its own cancellation source once it exits.
        /// Always called inside <see cref="_lock"/>.
        /// </summary>
        private void StopRecovery()
        {
            var recoveryCancellation = _recoveryCancellation;
            _recoveryCancellation = null;
            recoveryCancellation?.Cancel();
        }
    }
}
