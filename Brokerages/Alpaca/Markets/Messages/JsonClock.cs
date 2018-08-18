using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonClock : IClock
    {
        [JsonProperty(PropertyName = "timestamp", Required = Required.Always)]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "is_open", Required = Required.Always)]
        public Boolean IsOpen { get; set; }

        [JsonProperty(PropertyName = "next_open", Required = Required.Always)]
        public DateTime NextOpen { get; set; }

        [JsonProperty(PropertyName = "next_close", Required = Required.Always)]
        public DateTime NextClose { get; set; }
    }
}