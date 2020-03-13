/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updated to: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made in update:
 *   - Updated NullValueHandling for existing entries to accurately reflect new JsonAuthRequest
 *   - Added new values "JsonData.OAuthToken" and "Params" to the request type
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAuthRequest
    {
        internal sealed class JsonData
        {
            [JsonProperty(PropertyName = "key_id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
            public String KeyId { get; set; }

            [JsonProperty(PropertyName = "secret_key", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
            public String SecretKey { get; set; }

            [JsonProperty(PropertyName = "oauth_token", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
            public String OAuthToken { get; set; }
        }

        [JsonProperty(PropertyName = "action", Required = Required.Always)]
        public JsonAction Action { get; set; }

        [JsonProperty(PropertyName = "data", Required = Required.Default)]
        public JsonData Data { get; set; }

        [JsonProperty(PropertyName = "params", Required = Required.Default)]
        public String Params { get; set; }
    }
}