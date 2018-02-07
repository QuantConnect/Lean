using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class CancelReplacePost : PlaceOrderPost
    {
        [JsonProperty("order_id")]
        public long CancelOrderId { get; set; }
    }
#pragma warning restore 1591
}