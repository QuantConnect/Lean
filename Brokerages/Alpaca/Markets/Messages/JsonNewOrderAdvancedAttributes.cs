/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonNewOrderAdvancedAttributes
    {
        [JsonProperty(PropertyName = "limit_price", Required = Required.Default)]
        public Decimal? LimitPrice { get; set; }

        [JsonProperty(PropertyName = "stop_price", Required = Required.Default)]
        public Decimal? StopPrice { get; set; }
    }
}
