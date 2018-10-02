/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonFundamental : IFundamental
    {
        [JsonProperty(PropertyName = "asset_id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "full_name", Required = Required.Always)]
        public String FullName { get; set; }

        [JsonProperty(PropertyName = "industry_name", Required = Required.Always)]
        public String Industry { get; set; }

        [JsonProperty(PropertyName = "industry_group", Required = Required.Always)]
        public String IndustryGroup { get; set; }

        [JsonProperty(PropertyName = "sector", Required = Required.Always)]
        public String Sector { get; set; }

        [JsonProperty(PropertyName = "short_description", Required = Required.Always)]
        public String ShortDescription { get; set; }

        [JsonProperty(PropertyName = "long_description", Required = Required.Always)]
        public String LongDescription { get; set; }

        [JsonProperty(PropertyName = "pe_ratio", Required = Required.Always)]
        public Decimal pe_ratio { get; set; }

        [JsonProperty(PropertyName = "peg_ratio", Required = Required.Always)]
        public Decimal peg_ratio { get; set; }

        [JsonProperty(PropertyName = "beta", Required = Required.Always)]
        public Decimal beta { get; set; }

        [JsonProperty(PropertyName = "eps", Required = Required.Always)]
        public Decimal eps { get; set; }

        [JsonProperty(PropertyName = "market_cap", Required = Required.Always)]
        public Decimal MarketCapitalization { get; set; }

        [JsonProperty(PropertyName = "shares_outstanding", Required = Required.Always)]
        public Decimal SharesOutstanding { get; set; }

        [JsonProperty(PropertyName = "avg_vol", Required = Required.Always)]
        public Decimal AvgVolume { get; set; }

        [JsonProperty(PropertyName = "fifty_two_week_high", Required = Required.Always)]
        public Decimal FiftyTwoWeekHigh { get; set; }

        [JsonProperty(PropertyName = "fifty_two_week_low", Required = Required.Always)]
        public Decimal FiftyTwoWeekLow { get; set; }

        [JsonProperty(PropertyName = "div_rate", Required = Required.Always)]
        public Decimal DividentsRate { get; set; }

        [JsonProperty(PropertyName = "roa", Required = Required.Always)]
        public Decimal roa { get; set; }

        [JsonProperty(PropertyName = "roe", Required = Required.Always)]
        public Decimal roe { get; set; }

        [JsonProperty(PropertyName = "ps", Required = Required.Always)]
        public Decimal ps { get; set; }

        [JsonProperty(PropertyName = "pc", Required = Required.Always)]
        public Decimal pc { get; set; }

        [JsonProperty(PropertyName = "gross_margin", Required = Required.Always)]
        public Decimal GrossMargin { get; set; }
    }
}