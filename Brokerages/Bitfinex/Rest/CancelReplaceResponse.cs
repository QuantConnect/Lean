namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public class CancelReplaceResponse : PlaceOrderResponse
    {
        public long OriginalOrderId { get; set; }
    }
#pragma warning restore 1591
}