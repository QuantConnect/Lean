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
using System.Runtime.Serialization;

namespace QuantConnect.Lean.DataSource.CascadeThetaData.Models.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum WebSocketHeaderType
{
    [EnumMember(Value = "STATUS")]
    Status = 0,

    [EnumMember(Value = "QUOTE")]
    Quote = 1,

    [EnumMember(Value = "TRADE")]
    Trade = 2,

    [EnumMember(Value = "OHLC")]
    Ohlc = 3,

    [EnumMember(Value = "REQ_RESPONSE")]
    ReqResponse = 4,

    [EnumMember(Value = "STATE")]
    State = 5
}
