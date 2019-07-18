/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAuthRequest
    {
        internal sealed class JsonData
        {
            [JsonProperty(PropertyName = "key_id", Required = Required.Always)]
            public String KeyId { get; set; }

            [JsonProperty(PropertyName = "secret_key", Required = Required.Always)]
            public String SecretKey { get; set; }
        }

        [JsonProperty(PropertyName = "action", Required = Required.Always)]
        public JsonAction Action { get; set; }

        [JsonProperty(PropertyName = "data", Required = Required.Always)]
        public JsonData Data { get; set; }
    }
}
