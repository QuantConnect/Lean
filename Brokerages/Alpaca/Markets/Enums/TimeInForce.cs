using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Supported order durations in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TimeInForce
    {
        /// <summary>
        /// Daily order.
        /// </summary>
        [EnumMember(Value = "day")]
        Day,

        /// <summary>
        /// Good-till-cancal order.
        /// </summary>
        [EnumMember(Value = "gtc")]
        Gtc,

        /// <summary>
        /// Market-on-open order.
        /// </summary>
        [EnumMember(Value = "opg")]
        Opg
    }
}