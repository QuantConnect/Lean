using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class ErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
#pragma warning restore 1591
}