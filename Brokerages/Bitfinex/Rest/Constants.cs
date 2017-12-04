namespace QuantConnect.Brokerages.Bitfinex.Rest
{
#pragma warning disable 1591
    public static class Constants
    {
        public const string ApiBfxKey = "X-BFX-APIKEY";
        public const string ApiBfxPayload = "X-BFX-PAYLOAD";
        public const string ApiBfxSig = "X-BFX-SIGNATURE";

        public const string PubTickerRequestUrl = "/v1/pubticker";

        public const string SymbolDetailsRequestUrl = "/v1/symbols_details";
        public const string BalanceRequestUrl = "/v1/balances";
        public const string DepthOfBookRequestUrl = "v1/book/";
        public const string NewOrderRequestUrl = "/v1/order/new";
        public const string OrderStatusRequestUrl = "/v1/order/status";
        public const string OrderCancelRequestUrl = "/v1/order/cancel";
        public const string CancelAllRequestUrl = "/all";
        public const string CancelReplaceRequestUrl = "/replace";
        public const string MultipleRequestUrl = "/multi";

        public const string ActiveOrdersRequestUrl = "/v1/orders";
        public const string ActivePositionsRequestUrl = "/v1/positions";
        public const string HistoryRequestUrl = "/v1/history";
        public const string MyTradesRequestUrl = "/v1/mytrades";

        public const string LendbookRequestUrl = "/v1/lendbook/";
        public const string LendsRequestUrl = "/v1/lends/";

        public const string DepositRequestUrl = "/v1/deposit/new";
        public const string AccountInfoRequestUrl = "@/v1/account_infos";
        public const string MarginInfoRequstUrl = "/v1/margin_infos";

        public const string NewOfferRequestUrl = "/v1/offer/new";
        public const string CancelOfferRequestUrl = "/v1/offer/cancel";
        public const string OfferStatusRequestUrl = "/v1/offer/status";

        public const string ActiveOffersRequestUrl = "/v1/offers";
        public const string ActiveCreditsRequestUrl = "/v1/credits";

        public const string ActiveMarginSwapsRequestUrl = "/v1/taken_swaps";
        public const string CloseSwapRequestUrl = "/v1/swap/close";
        public const string ClaimPosRequestUrl = "/v1/position/claim";
    }
#pragma warning restore 1591
}