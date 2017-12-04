using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class OrderStatusPost : PostBase
    {
        /// <summary>
        /// This class can be used to send a cancel message in addition to 
        /// retrieving the current status of an order.
        /// </summary>
        [JsonProperty("order_id")]
        public long OrderId { get; set; }
    }
#pragma warning restore 1591
}