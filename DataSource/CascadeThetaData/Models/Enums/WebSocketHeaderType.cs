/*
 * CASCADELABS.IO
 * Cascade Labs LLC
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
