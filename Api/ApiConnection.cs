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
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.API;
using QuantConnect.Logging;
using QuantConnect.Orders;
using RestSharp;
using RestSharp.Authenticators;

namespace QuantConnect.Api
{
    /// <summary>
    /// API Connection and Hash Manager
    /// </summary>
    public class ApiConnection
    {
        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        public RestClient Client;

        // Authorization Credentials
        private readonly string _userId;
        private readonly string _token;

        /// <summary>
        /// Create a new Api Connection Class.
        /// </summary>
        /// <param name="userId">User Id number from QuantConnect.com account. Found at www.quantconnect.com/account </param>
        /// <param name="token">Access token for the QuantConnect account. Found at www.quantconnect.com/account </param>
        public ApiConnection(int userId, string token)
        {
            _token = token;
            _userId = userId.ToString();
            Client = new RestClient("https://www.quantconnect.com/api/v2/");
        }

        /// <summary>
        /// Return true if connected successfully.
        /// </summary>
        public bool Connected
        {
            get
            {
                var request = new RestRequest("authenticate", Method.GET);
                AuthenticationResponse response;
                if (TryRequest(request, out response))
                {
                    return response.Success;
                }
                return false;
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
            try
            {
                //Generate the hash each request
                // Add the UTC timestamp to the request header.
                // Timestamps older than 1800 seconds will not work.
                var timestamp = (int)Time.TimeStamp();
                var hash = CreateSecureHash(timestamp);
                request.AddHeader("Timestamp", timestamp.ToString());
                Client.Authenticator = new HttpBasicAuthenticator(_userId, hash);
                
                // Execute the authenticated REST API Call
                var restsharpResponse = Client.Execute(request);

                // Use custom converter for deserializing live results data
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Converters = { new LiveAlgorithmResultsJsonConverter(), new OrderJsonConverter() }
                };

                //Verify success
                result = JsonConvert.DeserializeObject<T>(restsharpResponse.Content);
                if (!result.Success)
                {
                    //result;
                    return false;
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "Api.ApiConnection() Failed to make REST request.");
                result = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Generate a secure hash for the authorization headers.
        /// </summary>
        /// <returns>Time based hash of user token and timestamp.</returns>
        private string CreateSecureHash(int timestamp)
        {
            // Create a new hash using current UTC timestamp.
            // Hash must be generated fresh each time.
            var data = string.Format("{0}:{1}", _token, timestamp);
            return SHA256(data);
        }

        /// <summary>
        /// Encrypt the token:time data to make our API hash.
        /// </summary>
        /// <param name="data">Data to be hashed by SHA256</param>
        /// <returns>Hashed string.</returns>
        private string SHA256(string data)
        {
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetByteCount(data));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
