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
                using (var client = new WebClient())
                {
                    var tx = JsonConvert.SerializeObject(packet);
                    if (tx.Length > 10000)
                    {
                        Log.Trace("StreamingApi.Transmit(): Packet too long: " + packet.GetType());
                    }

                    var response = client.UploadValues("http://streaming.quantconnect.com", new NameValueCollection
                    {
                        {"uid", userId.ToString()},
                        {"token", apiToken},
                        {"tx", tx}
                    });

                    //Deserialize the response from the streaming API and throw in error case.
                    var result = JsonConvert.DeserializeObject<Response>(System.Text.Encoding.UTF8.GetString(response));
                    if (result.Type == "error")
                    {
                        throw new Exception(result.Message);
                    }
                }
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