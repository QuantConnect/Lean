using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Historical bar duration size for Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BarDuration
    {
        /// <summary>
        /// Minute bar.
        /// </summary>
        [EnumMember(Value = "1Min")]
        OneMinute,

        /// <summary>
        /// Five minutes bar.
        /// </summary>
        [EnumMember(Value = "5Min")]
        FiveMinutes,

        /// <summary>
        /// Fifteen minutes bar.
        /// </summary>
        [EnumMember(Value = "15Min")]
        FifteenMinutes,

        /// <summary>
        /// Hourly bar.
        /// </summary>
        [EnumMember(Value = "1H")]
        OneHour,

        /// <summary>
        /// Daily bar.
        /// </summary>
        [EnumMember(Value = "1D")]
        OneDay,
    }
}