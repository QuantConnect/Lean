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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Container for timeseries array
    /// </summary>
    public class TradierTimeSeriesContainer
    {
        /// Data Time Series
        [JsonProperty(PropertyName = "data")]
        [JsonConverter(typeof(SingleValueListConverter<TradierTimeSeries>))]
        public List<TradierTimeSeries> TimeSeries;
    }

    /// <summary>
    /// One bar of historical Tradier data.
    /// </summary>
    public class TradierTimeSeries
    {
        /// Time of Price Sample
        [JsonProperty(PropertyName = "time")]
        public DateTime Time;

        /// Tick data requests:
        [JsonProperty(PropertyName = "price")]
        public decimal Price;

        /// Bar Requests: Open
        [JsonProperty(PropertyName = "open")]
        public decimal Open;

        /// Bar Requests: High
        [JsonProperty(PropertyName = "high")]
        public decimal High;

        /// Bar Requests: Low
        [JsonProperty(PropertyName = "low")]
        public decimal Low;

        /// Bar Requests: Close
        [JsonProperty(PropertyName = "close")]
        public decimal Close;

        /// Bar Requests: Volume
        [JsonProperty(PropertyName = "volume")]
        public long Volume;
    }


    /// <summary>
    /// Container for quotes:
    /// </summary>
    public class TradierQuoteContainer
    {
        /// Price Quotes:
        [JsonProperty(PropertyName = "quote")]
        [JsonConverter(typeof(SingleValueListConverter<TradierQuote>))]
        public List<TradierQuote> Quotes;
    }

    /// <summary>
    /// Quote data from Tradier:
    /// </summary>
    public class TradierQuote
    {
        /// Quote Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol = "";

        /// Quote Description
        [JsonProperty(PropertyName = "description")]
        public string Description = "";

        /// Quote Exchange
        [JsonProperty(PropertyName = "exch")]
        public string Exchange = "";

        /// Quote Type
        [JsonProperty(PropertyName = "type")]
        public string Type = "";

        /// Quote Last Price
        [JsonProperty(PropertyName = "last")]
        public decimal Last = 0;

        /// Quote Change Absolute
        [JsonProperty(PropertyName = "change")]
        public decimal Change = 0;

        /// Quote Change Percentage
        [JsonProperty(PropertyName = "change_percentage")]
        public decimal PercentageChange = 0;

        /// Quote Volume
        [JsonProperty(PropertyName = "volume")]
        public decimal Volume = 0;

        /// Quote Average Volume
        [JsonProperty(PropertyName = "average_volume")]
        public decimal AverageVolume = 0;

        /// Quote Last Volume
        [JsonProperty(PropertyName = "last_volume")]
        public decimal LastVolume = 0;

        /// Last Trade Date in Unix Time
        [JsonProperty(PropertyName = "trade_date")]
        public long TradeDateUnix = 0;

        /// Open Price
        [JsonProperty(PropertyName = "open")]
        public decimal? Open = 0;

        /// High Price
        [JsonProperty(PropertyName = "high")]
        public decimal? High = 0;

        /// Low Price
        [JsonProperty(PropertyName = "low")]
        public decimal? Low = 0;

        /// Closng Price
        [JsonProperty(PropertyName = "close")]
        public decimal? Close = 0;

        /// Previous Close
        [JsonProperty(PropertyName = "prevclose")]
        public decimal PreviousClose = 0;

        /// 52 W high
        [JsonProperty(PropertyName = "week_52_high")]
        public decimal Week52High = 0;

        /// 52 W Low
        [JsonProperty(PropertyName = "week_52_low")]
        public decimal Week52Low = 0;

        /// Bid Price
        [JsonProperty(PropertyName = "bid")]
        public decimal? Bid = 0;

        /// Bid Size:
        [JsonProperty(PropertyName = "bidsize")]
        public decimal BidSize = 0;

        /// Bid Exchange
        [JsonProperty(PropertyName = "bidexch")]
        public string BigExchange = "";

        /// Bid Date Unix
        [JsonProperty(PropertyName = "bid_date")]
        private long BidDateUnix = 0;

        /// Asking Price
        [JsonProperty(PropertyName = "ask")]
        public decimal? Ask = 0;

        /// Asking Quantity
        [JsonProperty(PropertyName = "asksize")]
        public decimal AskSize = 0;

        /// Ask Exchange
        [JsonProperty(PropertyName = "askexch")]
        public string AskExchange = "";

        /// Date of Ask
        [JsonProperty(PropertyName = "ask_date")]
        private long AskDateUnix = 0;

        /// Open Interest
        [JsonProperty(PropertyName = "open_interest")]
        private long Options_OpenInterest = 0;

        ///Option Underlying Asset
        [JsonProperty(PropertyName = "underlying")]
        private string Options_UnderlyingAsset = "";

        ///Option Strike Price
        [JsonProperty(PropertyName = "strike")]
        private decimal Options_Strike = 0;

        ///Option Constract Size
        [JsonProperty(PropertyName = "contract_size")]
        private int Options_ContractSize = 0;

        ///Option Exp Date
        [JsonProperty(PropertyName = "expiration_date")]
        private string Options_ExpirationDate;

        ///Option Exp Type
        [JsonProperty(PropertyName = "expiration_type")]
        private TradierOptionExpirationType Options_ExpirationType = TradierOptionExpirationType.Standard;

        /// Option Type
        [JsonProperty(PropertyName = "option_type")]
        private TradierOptionType Options_OptionType = TradierOptionType.Call;

        /// Empty Constructor
        public TradierQuote()
        { }
    }

    /// <summary>
    /// Container for deserializing history classes
    /// </summary>
    public class TradierHistoryDataContainer
    {
        /// Historical Data Contents
        [JsonProperty(PropertyName = "day")]
        [JsonConverter(typeof(SingleValueListConverter<TradierHistoryBar>))]
        public List<TradierHistoryBar> Data;
    }

    /// <summary>
    /// "Bar" for a history unit.
    /// </summary>
    public class TradierHistoryBar
    {
        /// Historical Data Bar: Date
        [JsonProperty(PropertyName = "date")]
        public DateTime Time;

        /// Historical Data Bar: Open
        [JsonProperty(PropertyName = "open")]
        public decimal Open;

        /// Historical Data Bar: High
        [JsonProperty(PropertyName = "high")]
        public decimal High;

        /// Historical Data Bar: Low
        [JsonProperty(PropertyName = "low")]
        public decimal Low;

        /// Historical Data Bar: Close
        [JsonProperty(PropertyName = "close")]
        public decimal Close;

        /// Historical Data Bar: Volume
        [JsonProperty(PropertyName = "volume")]
        public long Volume;
    }

    /// <summary>
    /// Current market status description
    /// </summary>
    public class TradierMarketStatus
    {
        /// Market Status: Date
        [JsonProperty(PropertyName = "date")]
        public DateTime Date;

        /// Market Status: Description
        [JsonProperty(PropertyName = "description")]
        public string Description;

        /// Market Status: Next Change in Status
        [JsonProperty(PropertyName = "next_change")]
        public string NextChange;

        /// Market Status: State
        [JsonProperty(PropertyName = "state")]
        public string State;

        /// Market Status: Timestamp
        [JsonProperty(PropertyName = "timestamp")]
        public long TimeStamp;
    }

    /// <summary>
    /// Calendar status:
    /// </summary>
    public class TradierCalendarStatus
    {
        /// Trading Calendar: Day
        [JsonProperty(PropertyName = "days")]
        public TradierCalendarDayContainer Days;

        /// Trading Calendar: month
        [JsonProperty(PropertyName = "month")]
        public int Month;

        /// Trading Calendar: year
        [JsonProperty(PropertyName = "year")]
        public int Year;
    }

    /// <summary>
    /// Container for the days array:
    /// </summary>
    public class TradierCalendarDayContainer
    {
        /// Trading Calendar: Days List
        [JsonProperty(PropertyName = "day")]
        [JsonConverter(typeof(SingleValueListConverter<TradierCalendarDay>))]
        public List<TradierCalendarDay> Days;
    }

    /// <summary>
    /// Single days properties from the calendar:
    /// </summary>
    public class TradierCalendarDay
    {
        /// Trading Calendar: Day
        [JsonProperty(PropertyName = "date")]
        public DateTime Date;

        /// Trading Calendar: Sattus
        [JsonProperty(PropertyName = "status")]
        public string Status;

        /// Trading Calendar: Description
        [JsonProperty(PropertyName = "description")]
        public string Description;

        /// Trading Calendar: Premarket Hours
        [JsonProperty(PropertyName = "premarket")]
        public TradierCalendarDayMarketHours Premarket;

        /// Trading Calendar: Open Hours
        [JsonProperty(PropertyName = "open")]
        public TradierCalendarDayMarketHours Open;

        /// Trading Calendar: Post Hours
        [JsonProperty(PropertyName = "postmarket")]
        public TradierCalendarDayMarketHours Postmarket;
    }

    /// <summary>
    /// Start and finish time of market hours for this market.
    /// </summary>
    public class TradierCalendarDayMarketHours
    {
        /// Trading Calendar: Start Hours
        [JsonProperty(PropertyName = "start")]
        public DateTime Start;

        /// Trading Calendar: End Hours
        [JsonProperty(PropertyName = "end")]
        public DateTime End;
    }

    /// <summary>
    /// Tradier Search Container for Deserialization:
    /// </summary>
    public class TradierSearchContainer
    {
        /// Trading Search container
        [JsonProperty(PropertyName = "security")]
        public List<TradierSearchResult> Results;
    }

    /// <summary>
    /// One search result from API
    /// </summary>
    public class TradierSearchResult
    {
        /// Trading Search: Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        /// Trading Search: Exch
        [JsonProperty(PropertyName = "exchange")]
        public string Exchange;

        /// Trading Search: Type
        [JsonProperty(PropertyName = "type")]
        public string Type;

        /// Trading Search: Description
        [JsonProperty(PropertyName = "description")]
        public string Description;
    }

    /// <summary>
    /// Create a new stream session
    /// </summary>
    public class TradierStreamSession
    {
        /// Trading Stream: Session Id
        public string SessionId;
        /// Trading Stream: Stream URL
        public string Url;
    }

    /// <summary>
    /// One data packet from a tradier stream:
    /// </summary>
    public class TradierStreamData
    {
        /// Trading Stream: Type
        [JsonProperty(PropertyName = "type")]
        public string Type;

        /// Unix date of the event
        [JsonProperty(PropertyName = "date")]
        public string UnixDate;

        /// Trading Stream: Symbol
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol;

        /// Trading Stream: Open
        [JsonProperty(PropertyName = "open")]
        public decimal SummaryOpen;

        /// Trading Stream: High
        [JsonProperty(PropertyName = "high")]
        public decimal SummaryHigh;

        /// Trading Stream: Low
        [JsonProperty(PropertyName = "low")]
        public decimal SummaryLow;

        /// Trading Stream: Close
        [JsonProperty(PropertyName = "close")]
        public decimal SummaryClose;

        /// Trading Stream: Bid Price
        [JsonProperty(PropertyName = "bid")]
        public decimal BidPrice;

        /// Trading Stream: BidSize
        [JsonProperty(PropertyName = "bidsz")]
        public int BidSize;

        /// Trading Stream: Bid Exhc
        [JsonProperty(PropertyName = "bidexch")]
        public string BidExchange;

        /// Trading Stream: Bid Time
        [JsonProperty(PropertyName = "biddate")]
        public long BidDateUnix;

        /// Trading Stream: Last Price
        [JsonProperty(PropertyName = "price")]
        public decimal TradePrice;

        /// Trading Stream: Last Size
        [JsonProperty(PropertyName = "size")]
        public decimal TradeSize;

        /// Trading Stream: Last Exh
        [JsonProperty(PropertyName = "exch")]
        public string TradeExchange;

        /// Trading Stream: Last Vol
        [JsonProperty(PropertyName = "cvol")]
        public long TradeCVol;

        /// Trading Stream: Ask Price
        [JsonProperty(PropertyName = "ask")]
        public decimal AskPrice;

        /// Trading Stream: Ask Size
        [JsonProperty(PropertyName = "asksz")]
        public int AskSize;

        /// Trading Stream: Ask Exhc
        [JsonProperty(PropertyName = "askexch")]
        public string AskExchange;

        /// Trading Stream: Ask Date
        [JsonProperty(PropertyName = "askdate")]
        public long AskDateUnix;

        /// <summary>
        /// Gets the tick timestamp (UTC)
        /// </summary>
        public DateTime GetTickTimestamp()
        {
            var unix = Type == "trade" ? UnixDate.ConvertInvariant<long>() : Type == "quote" ? BidDateUnix : 0;

            return Time.UnixMillisecondTimeStampToDateTime(unix);
        }
    }
}
