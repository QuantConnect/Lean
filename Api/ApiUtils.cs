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

using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace QuantConnect.Api
{
    /// <summary>
    /// API utility methods
    /// </summary>
    public static class ApiUtils
    {
        /// <summary>
        /// Creates a POST <see cref="HttpRequestMessage"/> with the specified endpoint and payload as form url encoded content.
        /// </summary>
        /// <param name="endpoint">The request endpoint</param>
        /// <param name="payload">The request payload</param>
        /// <returns>The POST request</returns>
        public static HttpRequestMessage CreatePostRequest(string endpoint, IEnumerable<KeyValuePair<string, string>> payload = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (payload != null)
            {
                request.Content = new FormUrlEncodedContent(payload);
            }

            return request;
        }

        /// <summary>
        /// Creates a POST <see cref="HttpRequestMessage"/> with the specified endpoint and payload as json body
        /// </summary>
        /// <param name="endpoint">The request endpoint</param>
        /// <param name="payload">The request payload</param>
        /// <param name="jsonSerializerSettings">Settings for the json serializer</param>
        /// <returns>The POST request</returns>
        public static HttpRequestMessage CreateJsonPostRequest(string endpoint, object payload = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (payload != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(payload, jsonSerializerSettings),
                    new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
            }

            return request;
        }
    }
}
