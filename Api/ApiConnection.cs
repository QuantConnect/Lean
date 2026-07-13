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
using QuantConnect.Logging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using QuantConnect.Util;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Api
{
    /// <summary>
    /// API Connection and Hash Manager
    /// </summary>
    public class ApiConnection : IDisposable
    {
        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        private HttpClient _httpClient;

        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        [Obsolete("RestSharp is deprecated and will be removed in a future release. Please use the SetClient method or the request methods that take an HttpRequestMessage")]
        public RestClient Client { get; set; }

        // Authorization Credentials
        private readonly string _userId;
        private readonly string _token;

        private LeanAuthenticator _authenticator;

        /// <summary>
        /// Create a new Api Connection Class.
        /// </summary>
        /// <param name="userId">User Id number from QuantConnect.com account. Found at www.quantconnect.com/account </param>
        /// <param name="token">Access token for the QuantConnect account. Found at www.quantconnect.com/account </param>
        public ApiConnection(int userId, string token)
            : this(userId, token, null)
        {
        }

        /// <summary>
        /// Create a new Api Connection Class.
        /// </summary>
        /// <param name="userId">User Id number from QuantConnect.com account. Found at www.quantconnect.com/account </param>
        /// <param name="token">Access token for the QuantConnect account. Found at www.quantconnect.com/account </param>
        /// <param name="baseUrl">The client's base address</param>
        /// <param name="defaultHeaders">Default headers for the client</param>
        /// <param name="timeout">The client timeout in seconds</param>
        public ApiConnection(int userId, string token, string baseUrl = null, Dictionary<string, string> defaultHeaders = null, int timeout = 0)
        {
            _token = token;
            _userId = userId.ToStringInvariant();
            SetClient(!string.IsNullOrEmpty(baseUrl) ? baseUrl : Globals.Api, defaultHeaders, timeout);
        }

        /// <summary>
        /// Return true if connected successfully.
        /// </summary>
        public bool Connected
        {
            get
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "authenticate");
                return TryRequest(request, out AuthenticationResponse response) && response.Success;
            }
        }

        /// <summary>
        /// Overrides the current client
        /// </summary>
        /// <param name="baseUrl">The client's base address</param>
        /// <param name="defaultHeaders">Default headers for the client</param>
        /// <param name="timeout">The client timeout in seconds</param>
        public void SetClient(string baseUrl, Dictionary<string, string> defaultHeaders = null, int timeout = 0)
        {
            if (_httpClient != null)
            {
                _httpClient.DisposeSafely();
            }

            var baseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");
#pragma warning disable CA2000 // Dispose objects before losing scope: the client takes ownership of the handler
            var handler = new SocketsHttpHandler
            {
                // fail fast when a connection can't be established instead of consuming the whole request timeout
                ConnectTimeout = TimeSpan.FromMinutes(1),
                // recycle pooled connections so DNS/load balancer changes and stale NAT state are picked up
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            _httpClient = new HttpClient(handler, disposeHandler: true) { BaseAddress = baseAddress };
#pragma warning restore CA2000
            Client = new RestClient(baseUrl);

            if (defaultHeaders != null)
            {
                foreach (var header in defaultHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                Client.AddDefaultHeaders(defaultHeaders);
            }

            if (timeout > 0)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                Client.Timeout = timeout * 1000;
            }
            else
            {
                _httpClient.Timeout = Timeout.InfiniteTimeSpan;
            }
        }

        /// <summary>
        /// Disposes of the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="result">Result object from the </param>
        /// <returns>T typed object response</returns>
        [Obsolete("RestSharp is deprecated and will be removed in a future release. Please use the TryRequest(HttpRequestMessage)")]
        public bool TryRequest<T>(RestRequest request, out T result)
            where T : RestResponse
        {
            var resultTuple = TryRequestAsync<T>(request).SynchronouslyAwaitTaskResult();
            result = resultTuple.Item2;
            return resultTuple.Item1;
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="result">Result object from the </param>
        /// <param name="timeout">Timeout for the request</param>
        /// <returns>T typed object response</returns>
        public bool TryRequest<T>(HttpRequestMessage request, out T result, TimeSpan? timeout = null)
            where T : RestResponse
        {
            var resultTuple = TryRequestAsync<T>(request, timeout).SynchronouslyAwaitTaskResult();
            result = resultTuple.Item2;
            return resultTuple.Item1;
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns>T typed object response</returns>
        [Obsolete("RestSharp is deprecated and will be removed in a future release. Please use the TryRequestAsync(HttpRequestMessage)")]
        public async Task<Tuple<bool, T>> TryRequestAsync<T>(RestRequest request)
            where T : RestResponse
        {
            var responseContent = string.Empty;
            T result;
            try
            {
                SetAuthenticator(request);

                // Execute the authenticated REST API Call
                var restsharpResponse = await Client.ExecuteAsync(request).ConfigureAwait(false);

                //Verify success
                if (restsharpResponse.ErrorException != null)
                {
                    Log.Error($"ApiConnection.TryRequest({request.Resource}): Error: {restsharpResponse.ErrorException.Message}");
                    return new Tuple<bool, T>(false, null);
                }

                if (!restsharpResponse.IsSuccessful)
                {
                    Log.Error($"ApiConnect.TryRequest({request.Resource}): Content: {restsharpResponse.Content}");
                }

                responseContent = restsharpResponse.Content;
                result = responseContent.DeserializeJson<T>();

                if (result == null || !result.Success)
                {
                    Log.Debug($"ApiConnection.TryRequest({request.Resource}): Raw response: '{responseContent}'");
                    return new Tuple<bool, T>(false, result);
                }
            }
            catch (Exception err)
            {
                Log.Error($"ApiConnection.TryRequest({request.Resource}): Error: {err.Message}, Response content: {responseContent}");
                return new Tuple<bool, T>(false, null);
            }

            return new Tuple<bool, T>(true, result);
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="timeout">Timeout for the request</param>
        /// <returns>T typed object response</returns>
        public async Task<Tuple<bool, T>> TryRequestAsync<T>(HttpRequestMessage request, TimeSpan? timeout = null)
            where T : RestResponse
        {
            HttpResponseMessage response = null;
            Stream responseContentStream = null;
            T result = null;

            // Default to 100 seconds (since we disabled the default client timeout)
            timeout ??= TimeSpan.FromSeconds(100);

            using var cancellationTokenSource = new CancellationTokenSource(timeout.Value);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (request.RequestUri != null && request.RequestUri.OriginalString.StartsWith('/'))
                {
                    request.RequestUri = new Uri(request.RequestUri.ToString().TrimStart('/'), UriKind.Relative);
                }

                SetAuthenticator(request);

                // Execute the authenticated REST API Call
                response = await _httpClient.SendAsync(request, cancellationTokenSource.Token).ConfigureAwait(false);
                responseContentStream = await response.Content.ReadAsStreamAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                try
                {
                    result = responseContentStream.DeserializeJson<T>(leaveOpen: true);
                }
                catch (Exception err)
                {
                    // a non json payload, for example an html error page from a proxy or load balancer,
                    // the http status and raw content describe the failure better than the parse exception
                    Log.Error($"ApiConnection.TryRequest({request.RequestUri}): failed to deserialize response: {err.GetType().Name}: {err.Message}." +
                        $" HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Content: {GetRawResponseContent(responseContentStream)}");
                    return new Tuple<bool, T>(false, null);
                }

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"ApiConnect.TryRequest({request.RequestUri}): HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}. " +
                        $"Content: {GetRawResponseContent(responseContentStream)}");
                }
                if (result == null || !result.Success)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"ApiConnection.TryRequest({request.RequestUri}): Raw response: '{GetRawResponseContent(responseContentStream)}'");
                    }
                    return new Tuple<bool, T>(false, result);
                }
            }
            catch (Exception err)
            {
                Log.Error($"ApiConnection.TryRequest({request.RequestUri}): {DescribeError(err, cancellationTokenSource.IsCancellationRequested, stopwatch.ElapsedMilliseconds, timeout.Value)}, Response content: {GetRawResponseContent(responseContentStream)}");
                return new Tuple<bool, T>(false, null);
            }
            finally
            {
                response?.DisposeSafely();
                responseContentStream?.DisposeSafely();
            }

            return new Tuple<bool, T>(true, result);
        }

        /// <summary>
        /// Builds a diagnostic description of a failed request: distinguishes a timeout from an external
        /// cancellation, includes how long the request ran, the exception cause chain with socket/http error
        /// codes and the thread pool state, to help tell network failures apart from process-level issues
        /// </summary>
        private static string DescribeError(Exception exception, bool timeoutElapsed, long elapsedMilliseconds, TimeSpan timeout)
        {
            try
            {
                var builder = new StringBuilder("Error: ");
                if (exception is OperationCanceledException)
                {
                    builder.Append(timeoutElapsed
                        ? Invariant($"request timed out, no response received (timeout {timeout.TotalSeconds}s)")
                        : "request was canceled before the timeout elapsed");
                }
                else
                {
                    for (var inner = exception; inner != null; inner = inner.InnerException)
                    {
                        if (inner != exception)
                        {
                            builder.Append(" -> ");
                        }
                        builder.Append(inner.GetType().Name);
                        if (inner is HttpRequestException httpException)
                        {
                            builder.Append(Invariant($"[{httpException.HttpRequestError}]"));
                        }
                        else if (inner is SocketException socketException)
                        {
                            builder.Append(Invariant($"[{socketException.SocketErrorCode}]"));
                        }
                        builder.Append(": ").Append(inner.Message);
                    }
                }
                builder.Append(Invariant($" after {elapsedMilliseconds}ms. ThreadPool: {ThreadPool.ThreadCount} threads, {ThreadPool.PendingWorkItemCount} pending work items"));
                if (exception is not HttpRequestException and not OperationCanceledException)
                {
                    // network failures are described by their error codes above and are frequent during outages,
                    // any other exception type is unexpected so include the stack trace to locate its source
                    builder.Append(Environment.NewLine).Append(exception.StackTrace);
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                // we are already handling a request failure, describing it should never throw
                return $"Error: {exception?.Message}";
            }
        }

        private static string GetRawResponseContent(Stream stream)
        {
            if (stream == null)
            {
                return string.Empty;
            }

            try
            {
                stream.Position = 0;
                using var reader = new StreamReader(stream, leaveOpen: true);
                return reader.ReadToEnd();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void SetAuthenticator(RestRequest request)
        {
            var base64EncodedAuthenticationString = GetAuthenticatorHeader(out var timeStamp);
            request.AddOrUpdateHeader("Authorization", $"Basic {base64EncodedAuthenticationString}");
            request.AddOrUpdateHeader("Timestamp", timeStamp);
        }

        private void SetAuthenticator(HttpRequestMessage request)
        {
            request.Headers.Remove("Timestamp");

            var base64EncodedAuthenticationString = GetAuthenticatorHeader(out var timeStamp);
            // set the authorization on the request itself, the client default headers are shared across
            // concurrent requests and mutating them while requests are in flight is not thread safe
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            request.Headers.Add("Timestamp", timeStamp);
        }

        private string GetAuthenticatorHeader(out string timeStamp)
        {
            var newTimeStamp = (int)Time.TimeStamp();
            var currentAuth = _authenticator;
            if (currentAuth == null || newTimeStamp - currentAuth.TimeStamp > 7000)
            {
                // Generate the hash each request
                // Add the UTC timestamp to the request header.
                // Timestamps older than 7200 seconds will not work.
                var hash = Api.CreateSecureHash(newTimeStamp, _token);
                var authenticationString = $"{_userId}:{hash}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
                _authenticator = currentAuth = new LeanAuthenticator(newTimeStamp, base64EncodedAuthenticationString);
            }

            timeStamp = currentAuth.TimeStampStr;
            return currentAuth.Base64EncodedAuthenticationString;
        }

        private class LeanAuthenticator
        {
            public int TimeStamp { get; }
            public string TimeStampStr { get; }
            public string Base64EncodedAuthenticationString { get; }
            public LeanAuthenticator(int timeStamp, string base64EncodedAuthenticationString)
            {
                TimeStamp = timeStamp;
                TimeStampStr = timeStamp.ToStringInvariant();
                Base64EncodedAuthenticationString = base64EncodedAuthenticationString;
            }
        }
    }
}
