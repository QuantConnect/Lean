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
 *
*/

using System;
using System.Web;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace QuantConnect.Api
{
    /// <summary>
    /// Helper methods for api authentication and interaction
    /// </summary>
    public static class Authentication
    {
        /// <summary>
        /// Generate a secure hash for the authorization headers.
        /// </summary>
        /// <returns>Time based hash of user token and timestamp.</returns>
        public static string Hash(int timestamp)
        {
            return Hash(timestamp, Globals.UserToken);
        }

        /// <summary>
        /// Generate a secure hash for the authorization headers.
        /// </summary>
        /// <returns>Time based hash of user token and timestamp.</returns>
        public static string Hash(int timestamp, string token)
        {
            // Create a new hash using current UTC timestamp.
            // Hash must be generated fresh each time.
            var data = $"{token}:{timestamp.ToStringInvariant()}";
            return data.ToSHA256();
        }

        /// <summary>
        /// Create an authenticated link for the target endpoint using the optional given payload
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <param name="payload">The payload</param>
        /// <returns>The authenticated link to trigger the request</returns>
        public static string Link(string endpoint, IEnumerable<KeyValuePair<string, object>> payload = null)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            var timestamp = (int)Time.TimeStamp();
            queryString.Add("authorization", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Globals.UserId}:{Hash(timestamp)}")));
            queryString.Add("timestamp", timestamp.ToStringInvariant());

            PopulateQueryString(queryString, payload);

            return $"{Globals.Api}{endpoint.RemoveFromStart("/").RemoveFromEnd("/")}?{queryString}";
        }

        /// <summary>
        /// Helper method to populate a query string with the given payload
        /// </summary>
        /// <remarks>Useful for testing purposes</remarks>
        public static void PopulateQueryString(NameValueCollection queryString, IEnumerable<KeyValuePair<string, object>> payload = null)
        {
            if (payload != null)
            {
                foreach (var kv in payload)
                {
                    AddToQuery(queryString, kv);
                }
            }
        }

        /// <summary>
        /// Will add the given key value pairs to the query encoded as xform data
        /// </summary>
        private static void AddToQuery(NameValueCollection queryString, KeyValuePair<string, object> keyValuePairs)
        {
            var objectType = keyValuePairs.Value.GetType();
            if (objectType.IsValueType || objectType == typeof(string))
            {
                // straight
                queryString.Add(keyValuePairs.Key, keyValuePairs.Value.ToString());
            }
            else
            {
                // let's take advantage of json to load the properties we should include
                var serialized = JsonConvert.SerializeObject(keyValuePairs.Value);
                foreach (var jObject in JObject.Parse(serialized))
                {
                    var subKey = $"{keyValuePairs.Key}[{jObject.Key}]";
                    if (jObject.Value is JObject)
                    {
                        // inception
                        AddToQuery(queryString, new KeyValuePair<string, object>(subKey, jObject.Value.ToObject<object>()));
                    }
                    else if(jObject.Value is JArray jArray)
                    {
                        var counter = 0;
                        foreach (var value in jArray.ToObject<List<object>>())
                        {
                            queryString.Add($"{subKey}[{counter++}]", value.ToString());
                        }
                    }
                    else
                    {
                        queryString.Add(subKey, jObject.Value.ToString());
                    }
                }
            }
        }
    }
}
