using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class TickerGet
    {
        [JsonProperty("mid")]
        public decimal Mid { get; set; }

        [JsonProperty("bid")]
        public decimal Bid { get; set; }

        [JsonProperty("ask")]
        public decimal Ask { get; set; }

        [JsonProperty("last_price")]
        public decimal LastPrice { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }
    }
#pragma warning restore 1591
}