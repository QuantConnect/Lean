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

public readonly struct WebSocketContract
{
    [JsonProperty("security_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ContractSecurityType SecurityType { get; }

    [JsonProperty("root")]
    public string Root { get; }

    [JsonProperty("expiration")]
    public string Expiration { get; }

    [JsonProperty("strike")]
    public decimal Strike { get; }

    [JsonProperty("right")]
    public string Right { get; }

    [JsonConstructor]
    public WebSocketContract(ContractSecurityType securityType, string root, string expiration, decimal strike, string right)
    {
        SecurityType = securityType;
        Root = root;
        Expiration = expiration;
        Strike = strike;
        Right = right;
    }
}
