using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Kraken.DataType {

    public class ResponseBase {
        public List<string> Error;
    }

    public class GetServerTimeResult {
        public int UnixTime;
        public string Rfc1123;
    }

    public class GetServerTimeResponse : ResponseBase {
        public GetServerTimeResult Result;
    }


    public class GetAssetInfoResponse : ResponseBase {
        public Dictionary<string, AssetInfo> Result;
    }

    public class GetAssetPairsResponse : ResponseBase {
        public Dictionary<string, AssetPair> Result;
    }

    public class GetTickerResponse : ResponseBase {
        public Dictionary<string, Ticker> Result;
    }

    public class GetOHLCResult {
        public Dictionary<string, List<OHLC>> Pairs;

        // <summary>
        /// Id to be used as since when polling for new, committed OHLC data.
        /// </summary>
        public long Last;
    }

    public class GetOHLCResponse : ResponseBase {
        public GetOHLCResult Result;
    }

    public class GetOrderBookResponse : ResponseBase {
        public Dictionary<string, OrderBook> Result;
    }

    public class GetRecentTradesResult {
        public Dictionary<string, List<Trade>> Trades;

        /// <summary>
        /// Id to be used as since when polling for new trade data.
        /// </summary>
        public long Last;
    }

    public class GetRecentSpreadResult {
        public Dictionary<string, List<SpreadItem>> Spread;

        /// <summary>
        /// Id to be used as since when polling for new spread data
        /// </summary>
        public long Last;
    }

    public class GetBalanceResponse : ResponseBase {
        public Dictionary<string, decimal> Result;
    }

    public class GetTradeBalanceResponse : ResponseBase {
        public TradeBalanceInfo Result;
    }

    public class QueryOrdersResponse : ResponseBase {
        public Dictionary<string, OrderInfo> Result;
    }

    public class GetTradesHistoryResult {
        public Dictionary<string, TradeInfo> Trades;
        public int Count;
    }

    public class GetTradesHistoryResponse : ResponseBase {
        public GetTradesHistoryResult Result;
    }

    public class QueryTradesResponse : ResponseBase {
        public Dictionary<string, TradeInfo> Result;
    }

    public class GetOpenPositionsResponse : ResponseBase {
        public Dictionary<string, PositionInfo> Result;
    }

    public class GetLedgerResult {
        public Dictionary<string, LedgerInfo> Ledger;
        public int Count;
    }

    public class GetLedgerResponse : ResponseBase {
        public GetLedgerResult Result;
    }

    public class QueryLedgersResponse : ResponseBase {
        public Dictionary<string, LedgerInfo> Result;
    }

    public class GetTradeVolumeResult {
        /// <summary>
        /// Volume currency.
        /// </summary>
        public string Currency;

        /// <summary>
        /// Current discount volume.
        /// </summary>
        public decimal Volume;

        /// <summary>
        /// Fee tier info (if requested).
        /// </summary>
        public Dictionary<string, FeeInfo> Fees;

        /// <summary>
        /// Maker fee tier info (if requested) for any pairs on maker/taker schedule.
        /// </summary>
        [JsonProperty(PropertyName = "fees_maker")]
        public Dictionary<string, FeeInfo> FeesMaker;
    }

    public class GetTradeVolumeResponse : ResponseBase {
        public GetTradeVolumeResult Result;
    }
    public class AddOrderDescr
    {
        /// <summary>
        /// Order description.
        /// </summary>
        public string Order;

        /// <summary>
        /// Conditional close order description (if conditional close set).
        /// </summary>
        public string Close;
    }

    public class AddOrderResult
    {
        /// <summary>
        /// Order description info.
        /// </summary>
        public AddOrderDescr Descr;

        /// <summary>
        /// Array of transaction ids for order (if order was added successfully).
        /// </summary>
        public string[] Txid;
    }

    public class AddOrderResponse : ResponseBase
    {
        public AddOrderResult Result;
    }

    public class CancelOrderResult
    {
        /// <summary>
        /// Number of orders canceled.
        /// </summary>
        public int Count;

        /// <summary>
        /// If set, order(s) is/are pending cancellation.
        /// </summary>
        public bool? Pending;
    }

    public class CancelOrderResponse : ResponseBase
    {
        public CancelOrderResult Result;
    }

    public class GetDepositMethodsResult
    {
        /// <summary>
        /// Name of deposit method.
        /// </summary>
        public string Method;

        /// <summary>
        /// Maximum net amount that can be deposited right now, or false if no limit
        /// </summary>
        public string Limit;

        /// <summary>
        /// Amount of fees that will be paid.
        /// </summary>
        public string Fee;

        /// <summary>
        /// Whether or not method has an address setup fee (optional).
        /// </summary>
        [JsonProperty(PropertyName = "address-setup-fee")]
        public bool? AddressSetupFee;
    }

    public class GetDepositMethodsResponse : ResponseBase
    {
        public GetDepositMethodsResult[] Result;
    }

    public class GetDepositAddressesResult
    {
    }

    public class GetDepositAddressesResponse : ResponseBase
    {
        public GetDepositAddressesResult Result;
    }

    public class GetDepositStatusResult
    {
        /// <summary>
        /// Name of the deposit method used.
        /// </summary>
        public string Method;

        /// <summary>
        /// Asset class.
        /// </summary>
        public string Aclass;

        /// <summary>
        /// Asset X-ISO4217-A3 code.
        /// </summary>
        public string Asset;

        /// <summary>
        /// Reference id.
        /// </summary>
        public string RefId;

        /// <summary>
        /// Method transaction id.
        /// </summary>
        public string Txid;

        /// <summary>
        /// Method transaction information.
        /// </summary>
        public string Info;

        /// <summary>
        /// Amount deposited.
        /// </summary>
        public decimal Amount;

        /// <summary>
        /// Fees paid.
        /// </summary>
        public decimal Fee;

        /// <summary>
        /// Unix timestamp when request was made.
        /// </summary>
        public int Time;

        /// <summary>
        /// status of deposit
        /// </summary>
        public string Status;

        // status-prop = additional status properties(if available)
        //    return = a return transaction initiated by Kraken
        //    onhold = deposit is on hold pending review
    }

    public class GetDepositStatusResponse : ResponseBase
    {
        public GetDepositStatusResult[] Result;
    }

    public class GetWithdrawInfoResult
    {
        /// <summary>
        /// Name of the withdrawal method that will be used
        /// </summary>
        public string Method;

        /// <summary>
        /// Maximum net amount that can be withdrawn right now.
        /// </summary>
        public decimal Limit;

        /// <summary>
        /// Amount of fees that will be paid.
        /// </summary>
        public decimal Fee;
    }

    public class GetWithdrawInfoResponse : ResponseBase
    {
        public GetWithdrawInfoResult Result;
    }

    public class WithdrawResult
    {
        public string RefId;
    }

    public class WithdrawResponse : ResponseBase
    {
        public WithdrawResult Result;
    }

    public class GetWithdrawStatusResult
    {
        /// <summary>
        /// Name of the withdrawal method used.
        /// </summary>
        public string Method;

        /// <summary>
        /// Asset class.
        /// </summary>
        public string Aclass;

        /// <summary>
        /// Asset X-ISO4217-A3 code.
        /// </summary>
        public string Asset;

        /// <summary>
        /// Reference id.
        /// </summary>
        public string RefId;

        /// <summary>
        /// Method transaction id.
        /// </summary>
        public string Txid;

        /// <summary>
        /// Method transaction information.
        /// </summary>
        public string Info;

        /// <summary>
        /// Amount withdrawn.
        /// </summary>
        public decimal Amount;

        /// <summary>
        /// Fees paid.
        /// </summary>
        public decimal Fee;

        /// <summary>
        /// Unix timestamp when request was made.
        /// </summary>
        public int Time;

        /// <summary>
        /// Status of withdrawal.
        /// </summary>
        public string Status;

        //status-prop = additional status properties(if available).
        //cancel-pending = cancelation requested.
        //canceled = canceled.
        //cancel-denied = cancelation requested but was denied.
        //return = a return transaction initiated by Kraken; it cannot be canceled.
        //onhold = withdrawal is on hold pending review.
    }

    public class GetWithdrawStatusResponse : ResponseBase
    {
        public GetWithdrawStatusResult Result;
    }

    public class WithdrawCancelResponse : ResponseBase
    {
        public bool Result;
    }

}
