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

using Newtonsoft.Json;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.WebSocket
{

    public class WebSocketResponse
    {
        [JsonProperty("header")]
        public WebSocketHeader Header { get; }

        [JsonProperty("contract")]
        public WebSocketContract? Contract { get; }

        [JsonProperty("trade")]
        public WebSocketTrade? Trade { get; }

        [JsonProperty("quote")]
        public WebSocketQuote? Quote { get; }

        public WebSocketResponse(WebSocketHeader header, WebSocketContract? contract, WebSocketTrade? trade, WebSocketQuote? quote)
        {
            Header = header;
            Contract = contract;
            Trade = trade;
            Quote = quote;

        }
    }
}
