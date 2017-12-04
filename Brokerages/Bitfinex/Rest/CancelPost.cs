using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class CancelPost : PostBase
    {
        [JsonProperty("order_id")]
        public long OrderId { get; set; }
    }
#pragma warning restore 1591
}