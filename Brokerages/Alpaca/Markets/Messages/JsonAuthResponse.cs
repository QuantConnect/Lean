using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAuthResponse
    {
        [JsonProperty(PropertyName = "action", Required = Required.Always)]
        public String Action { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public AuthStatus Status { get; set; }
    }
}