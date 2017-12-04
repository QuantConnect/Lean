using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class PostBase
    {
        [JsonProperty("nonce")]
        public double Nonce { get; set; }
    }
#pragma warning restore 1591
}