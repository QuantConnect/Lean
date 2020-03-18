/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updated from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made in update:
 *   - Extended Enum definitions to include PolygonAuthenticate, PolygonSubscribe, PolygonUnsubscribe
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum JsonAction
    {
        [EnumMember(Value = "authenticate")]
        Authenticate,

        [EnumMember(Value = "listen")]
        Listen,

        [EnumMember(Value = "auth")]
        PolygonAuthenticate,

        [EnumMember(Value = "subscribe")]
        PolygonSubscribe,

        [EnumMember(Value = "unsubscribe")]
        PolygonUnsubscribe,
    }
}
