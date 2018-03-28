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
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Packets;
using RestSharp;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Provides a common transmit method for utilizing the QC streaming API
    /// </summary>
    public static class StreamingApi
    {
        /// <summary>
        /// Gets a flag indicating whether or not the streaming api is enabled
        /// </summary>
        public static readonly bool IsEnabled = Config.GetBool("send-via-api");

        // Client for sending asynchronous requests.
        private static readonly RestClient Client = new RestClient("http://streaming.quantconnect.com")
        {
            Timeout = 300000
        };

        /// <summary>
        /// Send a message to the QuantConnect Chart Streaming API.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="apiToken">API token for authentication</param>
        /// <param name="packet">Packet to transmit</param>
        public static void Transmit(int userId, string apiToken, Packet packet)
        {
            try
            {
                var tx = JsonConvert.SerializeObject(packet);
                if (tx.Length > 10000)
                {
                    Log.Trace("StreamingApi.Transmit(): Packet too long: " + packet.GetType());
                    return;
                }
                if (userId == 0)
                {
                    Log.Error("StreamingApi.Transmit(): UserId is not set. Check your config.json file 'job-user-id' property.");
                    return;
                }
                if (apiToken == "")
                {
                    Log.Error("StreamingApi.Transmit(): API Access token not set. Check your config.json file 'api-access-token' property.");
                    return;
                }

                var request = new RestRequest();
                request.AddParameter("uid", userId);
                request.AddParameter("token", apiToken);
                request.AddParameter("tx", tx);
                Client.ExecuteAsyncPost(request, (response, handle) =>
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<Response>(response.Content);
                        if (result.Type == "error")
                        {
                            Log.Error(new Exception(result.Message), "PacketType: " + packet.Type);
                        }
                    }
                    catch
                    {
                        Log.Error("StreamingApi.Client.ExecuteAsyncPost(): Error deserializing JSON content.");
                    }
                }, "POST");
            }
            catch (Exception err)
            {
                Log.Error(err, "PacketType: " + packet.Type);
            }
        }

        /// <summary>
        /// Response object from the Streaming API.
        /// </summary>
        private class Response
        {
            /// <summary>
            /// Type of response from the streaming api.
            /// </summary>
            /// <remarks>success or error</remarks>
            public string Type;

            /// <summary>
            /// Message description of the error or success state.
            /// </summary>
            public string Message;
        }
    }
}