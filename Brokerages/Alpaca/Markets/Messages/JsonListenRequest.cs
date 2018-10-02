/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonListenRequest
    {
        internal sealed class JsonData
        {
            [JsonProperty(PropertyName = "streams", Required = Required.Always)]
            public List<String> Streams { get; set; }
        }

        [JsonProperty(PropertyName = "action", Required = Required.Always)]
        public JsonAction Action { get; set; }

        [JsonProperty(PropertyName = "data", Required = Required.Always)]
        public JsonData Data { get; set; }
    }
}