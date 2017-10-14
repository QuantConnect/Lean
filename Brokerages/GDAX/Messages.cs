using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.GDAX.Messages
{

    //several simple objects to facilitate json conversion
#pragma warning disable 1591

    public class BaseMessage
    {
        public string Type { get; set; }
        public long Sequence { get; set; }
        public DateTime Time { get; set; }
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
    }

    public class Done : BaseMessage
    {
        public decimal Price { get; set; }
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        public string Reason { get; set; }
        public string Side { get; set; }
        public decimal RemainingSize { get; set; }
    }

    public class Matched : BaseMessage
    {
        [JsonProperty("trade_id")]
        public int TradeId { get; set; }
        [JsonProperty("maker_order_id")]
        public string MakerOrderId { get; set; }
        [JsonProperty("taker_order_id")]
        public string TakerOrderId { get; set; }
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public string Side { get; set; }
        [JsonProperty("taker_user_id")]
        public string TakerUserId { get; set; }
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        [JsonProperty("taker_profile_id")]
        public string TakerProfileId { get; set; }
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
    }

    public class Heartbeat : BaseMessage
    {
        [JsonProperty("last_trade_id")]
        public int LastTradeId { get; set; }
    }

    public class Error : BaseMessage
    {
        public string Message { get; set; }
        public string Reason { get; set; }
    }

    public class Subscribe
    {
        public string Type { get; set; }
        [JsonProperty("product_ids")]
        public IList<string> ProductIds { get; set; }
        public string Signature { get; set; }
        public string Key { get; set; }
        public string Passphrase { get; set; }
        public string Timestamp { get; set; }
    }

    public class Open : BaseMessage
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        public decimal Price { get; set; }
        [JsonProperty("remaining_size")]
        public decimal RemainingSize { get; set; }
        public string Side { get; set; }
    }

    public class Change : Open
    {
        [JsonProperty("new_funds")]
        public decimal NewFunds { get; set; }
        [JsonProperty("old_funds")]
        public decimal OldFunds { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
        public string Side { get; set; }
        public string Stp { get; set; }
        public string Type { get; set; }
        [JsonProperty("fill_fees")]
        public decimal FillFees { get; set; }
        [JsonProperty("filled_size")]
        public decimal FilledSize { get; set; }
        [JsonProperty("executed_value")]
        public decimal ExecutedValue { get; set; }
        public string Status { get; set; }
        public bool Settled { get; set; }
    }

    public class Account
    {
        public string Id { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
        public decimal Hold { get; set; }
        public decimal Available { get; set; }
        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }
    }

    public class Tick
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }
        [JsonProperty("trade_id")]
        public string TradeId { get; set; }
        public decimal Price { get; set; }
        public decimal Size { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Volume { get; set; }
        public DateTime Time { get; set; }
    }

    public class Ticker : BaseMessage
    {
        [JsonProperty("trade_id")]
        public string TradeId { get; set; }
        [JsonProperty("last_size")]
        public decimal LastSize { get; set; }
        [JsonProperty("best_bid")]
        public decimal BestBid { get; set; }
        [JsonProperty("best_ask")]
        public decimal BestAsk { get; set; }
        public decimal Price { get; set; }
        public string Side { get; set; }
    }

#pragma warning restore 1591

}
