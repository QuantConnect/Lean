using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonError
    {
        [JsonProperty(PropertyName = "code", Required = Required.Always)]
        public Int32 Code { get; set; }

        [JsonProperty(PropertyName = "message", Required = Required.Always)]
        public String Message { get; set; }

    }
}
