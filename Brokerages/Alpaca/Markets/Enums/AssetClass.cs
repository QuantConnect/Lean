using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Supported assed classes for Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AssetClass
    {
        /// <summary>
        /// US equity asset class.
        /// </summary>
        [EnumMember(Value = "us_equity")]
        UsEquity
    }
}