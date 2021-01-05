/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

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
