using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Conditions map type for <see cref="RestClient.GetConditionMapAsync"/> call form Polygon REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TickType
    {
        /// <summary>
        /// Method <see cref="RestClient.GetConditionMapAsync"/> returns trades conditions.
        /// </summary>
        [EnumMember(Value = "trades")]
        Trades,

        /// <summary>
        /// Method <see cref="RestClient.GetConditionMapAsync"/> returns quotes conditions.
        /// </summary>
        [EnumMember(Value = "quotes")]
        Quotes
    }
}
