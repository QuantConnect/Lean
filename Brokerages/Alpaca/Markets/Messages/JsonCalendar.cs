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
                TradingDate.Date, DateTimeKind.Utc);

#if NETSTANDARD1_6
            var estTradingDate = TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(TradingDate.Date, DateTimeKind.Utc),
                TimeZoneInfo.Utc, CustomTimeZone.Est).Date;

            TradingOpenTime = TimeZoneInfo.ConvertTime(
                estTradingDate.Add(TradingOpenTime.TimeOfDay),
                CustomTimeZone.Est, TimeZoneInfo.Utc);
            TradingCloseTime = TimeZoneInfo.ConvertTime(
                estTradingDate.Add(TradingCloseTime.TimeOfDay),
                CustomTimeZone.Est, TimeZoneInfo.Utc);
#else
            var estTradingDate = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(TradingDate.Date, DateTimeKind.Utc),
                CustomTimeZone.Est).Date;

            TradingOpenTime = TimeZoneInfo.ConvertTimeToUtc(
                estTradingDate.Add(TradingOpenTime.TimeOfDay),
                CustomTimeZone.Est);
            TradingCloseTime = TimeZoneInfo.ConvertTimeToUtc(
                estTradingDate.Add(TradingCloseTime.TimeOfDay),
                CustomTimeZone.Est);
#endif
        }
    }
}