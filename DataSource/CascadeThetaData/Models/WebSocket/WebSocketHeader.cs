/*
 * CASCADELABS.IO
 * Cascade Labs LLC
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
