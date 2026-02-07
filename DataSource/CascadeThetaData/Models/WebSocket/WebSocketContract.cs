/*
 * CASCADELABS.IO
 * Cascade Labs LLC
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
