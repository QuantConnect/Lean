using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class PostBase
    {
        [JsonProperty("nonce")]
        public string Nonce { get; set; }
        [JsonProperty("request")]
        public string Request { get; set; }

    }
#pragma warning restore 1591
}