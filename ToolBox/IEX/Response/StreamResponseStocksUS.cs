/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

namespace QuantConnect.ToolBox.IEX.Response
{
    public class StreamResponseStocksUS
    {
        /// <summary>
        /// Refers to the stock ticker.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Refers to the company name.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Refers to the primary listing exchange for the symbol.
        /// </summary>
        public string PrimaryExchange { get; set; }

        /// <summary>
        /// Refers to the source of the latest price. Possible values are tops, sip, previousclose, close, or iexlasttrade.
        /// The iexlastrade value indicates that the latest price is the price of the last trade on IEX rather than the SIP closing price.
        /// iexlasttrade is provided for Nasdaq-listed symbols between 4:00 p.m.and 8 p.m.E.T. if you do not have UTP authorization.
        /// </summary>
        public string CalculationPrice { get; set; }

        /// <summary>
        /// Refers to the official open price from the SIP. 15 minute delayed (can be null after 00:00 ET, before 9:45 and weekends)
        /// </summary>
        public decimal? Open { get; set; }

        /// <summary>
        /// Refers to the official listing exchange time for the open from the SIP. 15 minute delayed
        /// </summary>
        public long? OpenTime { get; set; }

        /// <summary>
        /// This will represent a human readable description of the source of open.
        /// </summary>
        public string OpenSource { get; set; }

        /// <summary>
        /// Refers to the 15-minute delayed official close price from the SIP. For Nasdaq-listed stocks, if you do not have UTP authorization,
        /// between 4:00 p.m. and 8 p.m. E.T. this field will return the price of the last trade on IEX rather than the SIP closing price.
        /// </summary>
        public decimal? Close { get; set; }

        /// <summary>
        /// Refers to the official listing exchange time for the close from the SIP. 15 minute delayed
        /// </summary>
        public long? CloseTime { get; set; }

        /// <summary>
        /// This will represent a human readable description of the source of close.
        /// </summary>
        public string CloseSource { get; set; }

        /// <summary>
        /// Refers to the market-wide highest price from the SIP. 15 minute delayed during normal market hours 9:30 - 16:00 (null before 9:45 and weekends).
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Refers to the official listing exchange time for the high from the SIP. 15 minute delayed
        /// </summary>
        public long? HighTime { get; set; }

        /// <summary>
        /// This will represent a human readable description of the source of high.
        /// </summary>
        public string HighSource { get; set; }

        /// <summary>
        /// Refers to the market-wide lowest price from the SIP. 15 minute delayed during normal market hours 9:30 - 16:00 (null before 9:45 and weekends).
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Refers to the official listing exchange time for the low from the SIP. 15 minute delayed
        /// </summary>
        public long LowTime { get; set; }

        /// <summary>
        /// This will represent a human readable description of the source of low.
        /// </summary>
        public string LowSource { get; set; }

        /// <summary>
        /// Refers to the latest relevant price of the security which is derived from multiple sources. We first look for an IEX real
        /// time price. If an IEX real time price is older than 15 minutes, 15 minute delayed market price is used. If a 15 minute
        /// delayed price is not available, we will use the current day close price. If a current day close price is not available,
        /// we will use the last available closing price (listed below as previousClose) IEX real time price represents trades
        /// on IEX only. Trades occur across over a dozen exchanges, so the last IEX price can be used to indicate the overall market price.
        /// This will not included pre or post market prices
        /// </summary>
        public decimal LatestPrice { get; set; }

        /// <summary>
        /// This will represent a human readable description of the source of latestPrice.
        /// Possible values are IEX real time price, 15 minute delayed price, Close or Previous close
        /// </summary>
        public string LatestSource { get; set; }

        /// <summary>
        /// Refers to a human readable time/date of when latestPrice was last updated. The format will vary based on latestSource is
        /// intended to be displayed to a user. Use latestUpdate for machine readable timestamp.
        /// </summary>
        public string LatestTime { get; set; }

        /// <summary>
        /// Refers to the machine readable epoch timestamp of when latestPrice was last updated. Represented in milliseconds since midnight Jan 1, 1970.
        /// </summary>
        public long LatestUpdate { get; set; }

        /// <summary>
        /// Refers to the latest total market volume of the stock across all markets. This will be the most recent volume of the stock during trading hours,
        /// or it will be the total volume of the last available trading day.
        /// </summary>
        public long LatestVolume { get; set; }

        /// <summary>
        /// Refers to the price of the last trade on IEX.
        /// </summary>
        public decimal? IexRealtimePrice { get; set; }

        /// <summary>
        /// Refers to the size of the last trade on IEX.
        /// </summary>
        public int? IexRealtimeSize { get; set; }

        /// <summary>
        /// Refers to the last update time of iexRealtimePrice in milliseconds since midnight Jan 1, 1970 UTC or -1 or 0.
        /// If the value is -1 or 0, IEX has not quoted the symbol in the trading day.
        /// </summary>
        public long? IexLastUpdated { get; set; }

        /// <summary>
        /// Refers to the 15 minute delayed market price from the SIP during normal market hours 9:30 - 16:00 ET.
        /// </summary>
        public decimal? DelayedPrice { get; set; }

        /// <summary>
        /// Refers to the last update time of the delayed market price during normal market hours 9:30 - 16:00 ET.
        /// </summary>
        public long? DelayedPriceTime { get; set; }

        /// <summary>
        /// Refers to the 15 minute delayed odd Lot trade price from the SIP during normal market hours 9:30 - 16:00 ET.
        /// </summary>
        public decimal? OddLotDelayedPrice { get; set; }

        /// <summary>
        /// Refers to the last update time of the odd Lot trade price during normal market hours 9:30 - 16:00 ET.
        /// </summary>
        public long? OddLotDelayedPriceTime { get; set; }

        /// <summary>
        /// Refers to the 15 minute delayed price outside normal market hours 0400 - 0930 ET and 1600 - 2000 ET.
        /// This provides pre market and post market price. This is purposefully separate from latestPrice so users can display the two prices separately.
        /// </summary>
        public decimal? ExtendedPrice { get; set; }

        /// <summary>
        /// Refers to the price change between extendedPrice and latestPrice
        /// </summary>
        public decimal? ExtendedChange { get; set; }

        /// <summary>
        /// Refers to the price change percent between extendedPrice and latestPrice
        /// </summary>
        public decimal? ExtendedChangePercent { get; set; }

        /// <summary>
        /// Refers to the last update time of extendedPrice
        /// </summary>
        public long? ExtendedPriceTime { get; set; }

        /// <summary>
        /// Refers to the previous trading day closing price.
        /// </summary>
        public decimal PreviousClose { get; set; }

        /// <summary>
        /// Refers to the previous trading day volume.
        /// </summary>
        public long PreviousVolume { get; set; }

        /// <summary>
        /// Refers to the change in price between latestPrice and previousClose
        /// </summary>
        public decimal Change { get; set; }

        /// <summary>
        /// Refers to the percent change in price between latestPrice and previousClose. For example, a 5% change would be represented as 0.05.
        /// You can use the query string parameter displayPercent to return this field multiplied by 100. So, 5% change would be represented as 5.
        /// </summary>
        public decimal ChangePercent { get; set; }

        /// <summary>
        /// Total volume for the stock, but only updated after market open. To get premarket volume, use latestVolume
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Refers to IEX’s percentage of the market in the stock.
        /// </summary>
        public decimal? IexMarketPercent { get; set; }

        /// <summary>
        /// Refers to shares traded in the stock on IEX.
        /// </summary>
        public int? IexVolume { get; set; }

        /// <summary>
        /// Refers to the 30 day average volume.
        /// </summary>
        public int AvgTotalVolume { get; set; }

        /// <summary>
        /// Refers to the best bid price on IEX.
        /// </summary>
        public decimal? IexBidPrice { get; set; }

        /// <summary>
        /// Refers to amount of shares on the bid on IEX.
        /// </summary>
        public int? IexBidSize { get; set; }

        /// <summary>
        /// Refers to the best ask price on IEX.
        /// </summary>
        public decimal? IexAskPrice { get; set; }

        /// <summary>
        /// Refers to amount of shares on the ask on IEX.
        /// </summary>
        public int? IexAskSize { get; set; }

        /// <summary>
        /// Refers to IEX previous day open price
        /// </summary>
        public decimal? IexOpen { get; set; }

        /// <summary>
        /// Refers to IEX previous day open time
        /// </summary>
        public long? IexOpenTime { get; set; }

        /// <summary>
        /// Refers to IEX previous day close price
        /// </summary>
        public decimal? IexClose { get; set; }

        /// <summary>
        /// Refers to IEX previous day close time
        /// </summary>
        public long? IexCloseTime { get; set; }

        /// <summary>
        /// is calculated in real time using latestPrice.
        /// </summary>
        public long MarketCap { get; set; }

        /// <summary>
        /// Refers to the price-to-earnings ratio for the company.
        /// </summary>
        public decimal? PeRatio { get; set; }

        /// <summary>
        /// Refers to the adjusted 52 week high.
        /// </summary>
        public decimal Week52High { get; set; }

        /// <summary>
        /// Refers to the adjusted 52 week low.
        /// </summary>
        public decimal Week52Low { get; set; }

        /// <summary>
        /// Refers to the price change percentage from start of year to previous close.
        /// </summary>
        public decimal YtdChange { get; set; }

        /// <summary>
        /// Epoch timestamp in milliseconds of the last market hours trade excluding the closing auction trade.
        /// </summary>
        public long? LastTradeTime { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Symbol},{LatestTime},{LatestPrice},{LatestVolume}";
        }

    }
}
