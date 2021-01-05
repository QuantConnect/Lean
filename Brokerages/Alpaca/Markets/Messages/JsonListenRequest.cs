/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updated from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made in update:
 *   - Added "Params" property
 *   - Updated "Data" Required from "Required.Always" to "Required.Default"
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

        [JsonProperty(PropertyName = "params", Required = Required.Default)]
        public String Params { get; set; }

        [JsonProperty(PropertyName = "data", Required = Required.Default)]
        public JsonData Data { get; set; }
    }
}
