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
using Newtonsoft.Json.Converters;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Enums;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.WebSocket;

public readonly struct WebSocketHeader
{
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public WebSocketHeaderType Type { get; }

    [JsonProperty("status")]
    public string Status { get; }

    [JsonProperty("response")]
    public string Response { get; }

    [JsonProperty("req_id")]
    public int RequestId { get; }

    [JsonProperty("state")]
    public string State { get; }

    [JsonConstructor]
    public WebSocketHeader(WebSocketHeaderType type, string status, string response, int requestId, string state)
    {
        Type = type;
        Status = status;
        Response = response;
        RequestId = requestId;
        State = state;
    }
}
