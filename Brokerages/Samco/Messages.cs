using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Samco.Messages
{
    public class HoldingsResponse
    {
        public class HoldingDetail
        {
            public decimal averagePrice { get; set; }
            public string exchange { get; set; }
            public string markToMarketPrice { get; set; }
            public decimal lastTradedPrice { get; set; }
            public string previousClose { get; set; }
            public string productCode { get; set; }
            public string symbolDescription { get; set; }
            public string tradingSymbol { get; set; }
            public string calculatedNetQuantity { get; set; }
            public decimal holdingsQuantity { get; set; }
            public string collateralQuantity { get; set; }
            public string holdingsValue { get; set; }
            public string ISIN { get; set; }
            public string sellableQuantity { get; set; }
            public string totalMarketToMarketPrice { get; set; }
        }

        public string serverTime { get; set; }
        public string msgId { get; set; }
        public IList<HoldingDetail> holdingDetails { get; set; }
    }
    public class CandleResponse
    {
        public class IntradayCandleData
        {
            public DateTime dateTime { get; set; }
            public decimal open { get; set; }
            public decimal high { get; set; }
            public decimal low { get; set; }
            public decimal close { get; set; }
            public decimal volume { get; set; }
        }

        public string serverTime { get; set; }
        public string msgId { get; set; }
        public IList<IntradayCandleData> intradayCandleData { get; set; }
    }

    public class OrderResponse
    {
        public string serverTime { get; set; }
        public string msgId { get; set; }
        public string orderNumber { get; set; }
        public string status { get; set; }
        public string orderStatus { get; set; }
        public string statusMessage { get; set; }
        public string exchangeOrderStatus { get; set; }
        public string rejectionReason { get; set; }
        public OrderDetails orderDetails { get; set; }
        public IList<string> validationErrors { get; set; }
    }

    public class QuoteResponse
    {
        public class BestBid
        {
            public string number { get; set; }
            public string quantity { get; set; }
            public decimal price { get; set; }
        }

        public class BestAsk
        {
            public string number { get; set; }
            public string quantity { get; set; }
            public decimal price { get; set; }
        }

        public string msgId { get; set; }
        public string status { get; set; }
        public string statusMessage { get; set; }
        public string tradingSymbol { get; set; }
        public string exchange { get; set; }
        public string companyName { get; set; }
        public string lastTradedTime { get; set; }
        public string lastTradedPrice { get; set; }
        public string previousClose { get; set; }
        public string changeValue { get; set; }
        public string changePercentage { get; set; }
        public string lastTradedQuantity { get; set; }
        public string lowerCircuitLimit { get; set; }
        public string upperCircuitLimit { get; set; }
        public string averagePrice { get; set; }
        public string openValue { get; set; }
        public string highValue { get; set; }
        public string lowValue { get; set; }
        public string closeValue { get; set; }
        public string totalBuyQuantity { get; set; }
        public string totalSellQuantity { get; set; }
        public string totalTradedValue { get; set; }
        public decimal totalTradedVolume { get; set; }
        public string yearlyHighPrice { get; set; }
        public string yearlyLowPrice { get; set; }
        public string tickSize { get; set; }
        public string openInterest { get; set; }
        public IList<BestBid> bestBids { get; set; }
        public IList<BestAsk> bestAsks { get; set; }
        public string expiryDate { get; set; }
        public string spotPrice { get; set; }
        public string instrument { get; set; }
        public string lotQuantity { get; set; }
        public string listingId { get; set; }
        public string openInterestChange { get; set; }
        public string getoIChangePer { get; set; }
    }



    public class AuthRequest
    {
        public string userId { get; set; }
        public string password { get; set; }
        public string yob { get; set; }
    }

    public class Subscription
    {
        public class Symbol
        {
            public string symbol { get; set; }
        }

        public class Data
        {
            public List<Symbol> symbols { get; set; } = new List<Symbol>();
        }

        public class Request
        {
            public string streaming_type { get; set; } = "quote";
            public Data data { get; set; } = new Data();
            public string request_type { get; set; } = "subscribe";
            public string response_format { get; set; } = "json";
        }
        public Request request { get; set; } = new Request();
    }



    public class QuoteUpdate
    {
        public class Data
        {
            public decimal aPr { get; set; }
            public decimal aSz { get; set; }
            public string avgPr { get; set; }
            public decimal bPr { get; set; }
            public decimal bSz { get; set; }
            public string c { get; set; }
            public string ch { get; set; }
            public string chPer { get; set; }
            public string h { get; set; }
            public string l { get; set; }
            public DateTime lTrdT { get; set; }
            public decimal ltp { get; set; }
            public decimal ltq { get; set; }
            public string ltt { get; set; }
            public string lttUTC { get; set; }
            public string o { get; set; }
            public string oI { get; set; }
            public string oIChg { get; set; }
            public string sym { get; set; }
            public string tBQ { get; set; }
            public string tSQ { get; set; }
            public string ttv { get; set; }
            public string vol { get; set; }
            public string yH { get; set; }
            public string yL { get; set; }
        }

        public class Response
        {
            public Data data { get; set; }
            public string streaming_type { get; set; }
        }

        public Response response { get; set; }
    }

    public class OrderDetails
    {
        public string pendingQuantity { get; set; }
        public string avgExecutionPrice { get; set; }
        public string orderPlacedBy { get; set; }
        public string tradingSymbol { get; set; }
        public string triggerPrice { get; set; }
        public string exchange { get; set; }
        public string totalQuantity { get; set; }
        public string expiry { get; set; }
        public string transactionType { get; set; }
        public string productType { get; set; }
        public string orderType { get; set; }
        public string quantity { get; set; }
        public string filledQuantity { get; set; }
        public string orderPrice { get; set; }
        public string filledPrice { get; set; }
        public string exchangeOrderNo { get; set; }
        public string orderValidity { get; set; }

        public string orderNumber { get; set; }

        public string orderStatus { get; set; }
        public string orderTime { get; set; }
    }

    public class OrderBookResponse
    {
        public string serverTime { get; set; }
        public string msgId { get; set; }
        public string orderNumber { get; set; }
        public string status { get; set; }
        public string statusMessage { get; set; }

        public List<OrderDetails> orderBookDetails {get;set;}
    }

    public  class UserLimitResponse
    {
        [JsonProperty("serverTime")]
        public string ServerTime { get; set; }

        [JsonProperty("msgId")]
        public string MsgId { get; set; }

        [JsonProperty("equityLimit")]
        public SegmentLimit EquityLimit { get; set; }

        [JsonProperty("commodityLimit")]
        public SegmentLimit CommodityLimit { get; set; }
    }

    public class SegmentLimit
    {
        [JsonProperty("grossAvailableMargin")]
        public string GrossAvailableMargin { get; set; }

        [JsonProperty("payInToday")]
    
        public long PayInToday { get; set; }

        [JsonProperty("notionalCash")]
  
        public long NotionalCash { get; set; }

        [JsonProperty("collateralMarginAgainstShares")]
     
        public long CollateralMarginAgainstShares { get; set; }

        [JsonProperty("marginUsed")]
        public string MarginUsed { get; set; }

        [JsonProperty("netAvailableMargin")]
        public string NetAvailableMargin { get; set; }
    }

  
}

