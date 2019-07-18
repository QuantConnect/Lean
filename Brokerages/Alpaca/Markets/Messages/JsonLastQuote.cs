/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonLastQuote : ILastQuote
    {
        internal sealed class Last
        {
            [JsonProperty(PropertyName = "bidexchange", Required = Required.Always)]
            public Int64 BidExchange { get; set; }

            [JsonProperty(PropertyName = "askexchange", Required = Required.Always)]
            public Int64 AskExchange { get; set; }

            [JsonProperty(PropertyName = "bidprice", Required = Required.Default)]
            public Decimal BidPrice { get; set; }

            [JsonProperty(PropertyName = "askprice", Required = Required.Default)]
            public Decimal AskPrice { get; set; }

            [JsonProperty(PropertyName = "bidsize", Required = Required.Default)]
            public Int64 BidSize { get; set; }

            [JsonProperty(PropertyName = "asksize", Required = Required.Default)]
            public Int64 AskSize { get; set; }

            [JsonProperty(PropertyName = "timestamp", Required = Required.Always)]
            public Int64 Timestamp { get; set; }
        }

        [JsonProperty(PropertyName = "last", Required = Required.Always)]
        public Last Nested { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public String Status { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonIgnore]
        public Int64 BidExchange => Nested.BidExchange;

        [JsonIgnore]
        public Int64 AskExchange => Nested.AskExchange;

        [JsonIgnore]
        public Decimal BidPrice => Nested.BidPrice;

        [JsonIgnore]
        public Decimal AskPrice => Nested.AskPrice;

        [JsonIgnore]
        public Int64 BidSize => Nested.BidSize;

        [JsonIgnore]
        public Int64 AskSize => Nested.AskSize;

        [JsonIgnore]
        public DateTime Time { get; private set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            Time = DateTimeHelper.FromUnixTimeMilliseconds(Nested.Timestamp);
        }
    }
}
