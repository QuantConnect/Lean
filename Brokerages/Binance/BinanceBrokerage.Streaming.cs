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

using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// UserData Session class used to manage listen key
    /// </summary>
    public partial class BinanceBrokerage
    {
        private object _listenKeyLocker = new object();
        private string userDataStreamEndpoint = "/api/v1/userDataStream";

        /// <summary>
        /// Represents UserData Session listen key
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Check User Data stream listen key is alive
        /// </summary>
        /// <returns></returns>
        private bool SessionKeepAlive()
        {
            if (string.IsNullOrEmpty(SessionId))
            {
                throw new Exception("BinanceBrokerage:UserStream. listenKey wasn't allocated or has been refused.");
            }

            var ping = new RestRequest(userDataStreamEndpoint, Method.PUT);
            ping.AddHeader(KeyHeader, ApiKey);
            ping.AddParameter(
                "application/x-www-form-urlencoded",
                Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                ParameterType.RequestBody
            );

            var pong = ExecuteRestRequest(ping);
            return pong.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Stops the session
        /// </summary>
        public void StopSession()
        {
            if (!string.IsNullOrEmpty(SessionId))
            {
                var request = new RestRequest(userDataStreamEndpoint, Method.DELETE);
                request.AddHeader(KeyHeader, ApiKey);
                request.AddParameter(
                    "application/x-www-form-urlencoded",
                    Encoding.UTF8.GetBytes($"listenKey={SessionId}"),
                    ParameterType.RequestBody
                );
                ExecuteRestRequest(request);
            }
        }


        private void CreateListenKey()
        {
            var request = new RestRequest(userDataStreamEndpoint, Method.POST);
            request.AddHeader(KeyHeader, ApiKey);

            var response = ExecuteRestRequest(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"BinanceBrokerage.StartSession: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var content = JObject.Parse(response.Content);
            lock (_listenKeyLocker)
            {
                SessionId = content.Value<string>("listenKey");
            }
        }
    }
}