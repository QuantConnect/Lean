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
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Threading.Tasks;
using RestSharp.Authenticators;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// API Connection and Hash Manager
    /// </summary>
    public class ApiConnection
    {
        private readonly static JsonSerializerSettings _jsonSettings = new() { Converters = { new LiveAlgorithmResultsJsonConverter(), new OrderJsonConverter() } };

        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        private RestClient _restSharpClient;

        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        private HttpClient _client;

        // Authorization Credentials
        private readonly string _userId;
        private readonly string _token;

        private LeanAuthenticator _authenticator;

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
            if (_client != null)
            {
                _client.DisposeSafely();
            }

            _client = new HttpClient() { BaseAddress = new Uri(baseUrl) };
            _restSharpClient = new RestClient(baseUrl);

            if (defaultHeaders != null)
            {
                foreach (var header in defaultHeaders)
                {
                    _client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                _restSharpClient.AddDefaultHeaders(defaultHeaders);
            }

            if (timeout > 0)
            {
                _client.Timeout = TimeSpan.FromSeconds(timeout);
                _restSharpClient.Timeout = timeout * 1000;
            }
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="result">Result object from the </param>
        /// <returns>T typed object response</returns>
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
        /// <returns>T typed object response</returns>
        public bool TryRequest<T>(HttpRequestMessage request, out T result)
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
        /// <returns>T typed object response</returns>
        public async Task<Tuple<bool, T>> TryRequestAsync<T>(RestRequest request)
            where T : RestResponse
        {
            var responseContent = string.Empty;
            T result;
            try
            {
                SetAuthenticator(request);

                // Execute the authenticated REST API Call
                var restsharpResponse = await _restSharpClient.ExecuteAsync(request).ConfigureAwait(false);

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
                result = JsonConvert.DeserializeObject<T>(responseContent, _jsonSettings);

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
        /// <returns>T typed object response</returns>
        public async Task<Tuple<bool, T>> TryRequestAsync<T>(HttpRequestMessage request)
            where T : RestResponse
        {
            var responseContent = string.Empty;
            T result;
            try
            {
                SetAuthenticator(request);

                // Execute the authenticated REST API Call
                using var response = await _client.SendAsync(request).ConfigureAwait(false);
                responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"ApiConnect.TryRequest({request.RequestUri}): Content: {responseContent}");
                }

                result = JsonConvert.DeserializeObject<T>(responseContent, _jsonSettings);
                if (result == null || !result.Success)
                {
                    Log.Debug($"ApiConnection.TryRequest({request.RequestUri}): Raw response: '{responseContent}'");
                    return new Tuple<bool, T>(false, result);
                }
            }
            catch (Exception err)
            {
                Log.Error($"ApiConnection.TryRequest({request.RequestUri}): Error: {err.Message}, Response content: {responseContent}");
                return new Tuple<bool, T>(false, null);
            }

            return new Tuple<bool, T>(true, result);
        }

        private void SetAuthenticator(RestRequest request)
        {
            ConfigureAuthentication();

            request.AddHeader("Timestamp", _authenticator.TimeStampStr);
        }

        private void SetAuthenticator(HttpRequestMessage request)
        {
            ConfigureAuthentication();

            request.Headers.Add("Timestamp", _authenticator.TimeStampStr);
        }

        private void ConfigureAuthentication()
        {
            var newTimeStamp = (int)Time.TimeStamp();

            var currentAuth = _authenticator;
            if (currentAuth == null || newTimeStamp - currentAuth.TimeStamp > 7000)
            {
                // Generate the hash each request
                // Add the UTC timestamp to the request header.
                // Timestamps older than 7200 seconds will not work.
                var hash = Api.CreateSecureHash(newTimeStamp, _token);
                var authenticator = new HttpBasicAuthenticator(_userId, hash);
                _authenticator = currentAuth = new LeanAuthenticator(authenticator, newTimeStamp);

                var authenticationString = $"{_userId}:{hash}";
                var base64EncodedAuthenticationString = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(authenticationString));
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

                _restSharpClient.Authenticator = currentAuth.Authenticator;
            }
        }

        private class LeanAuthenticator
        {
            public int TimeStamp { get; }
            public string TimeStampStr { get; }
            public HttpBasicAuthenticator Authenticator { get; }
            public LeanAuthenticator(HttpBasicAuthenticator authenticator, int timeStamp)
            {
                TimeStamp = timeStamp;
                Authenticator = authenticator;
                TimeStampStr = timeStamp.ToStringInvariant();
            }
        }
    }
}
