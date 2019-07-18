/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonCalendar : ICalendar
    {
        [JsonConverter(typeof(DateConverter))]
        [JsonProperty(PropertyName = "date", Required = Required.Always)]
        public DateTime TradingDate { get; set; }

        [JsonConverter(typeof(TimeConverter))]
        [JsonProperty(PropertyName = "open", Required = Required.Always)]
        public DateTime TradingOpenTime { get; set; }

        [JsonConverter(typeof(TimeConverter))]
        [JsonProperty(PropertyName = "close", Required = Required.Always)]
        public DateTime TradingCloseTime { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            TradingDate = DateTime.SpecifyKind(
                TradingDate.Date, DateTimeKind.Unspecified).Date;

            TradingOpenTime = TimeZoneInfo.ConvertTimeToUtc(
                TradingDate.Add(TradingOpenTime.TimeOfDay),
                CustomTimeZone.Est);
            TradingCloseTime = TimeZoneInfo.ConvertTimeToUtc(
                TradingDate.Add(TradingCloseTime.TimeOfDay),
                CustomTimeZone.Est);

            TradingDate = DateTime.SpecifyKind(
                TradingDate.Date, DateTimeKind.Utc);
        }
    }
}
