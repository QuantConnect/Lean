using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Position side in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PositionSide
    {
        /// <summary>
        /// Long position.
        /// </summary>
        [EnumMember(Value = "long")]
        Long,

        /// <summary>
        /// Short position.
        /// </summary>
        [EnumMember(Value = "short")]
        Short
    }
}