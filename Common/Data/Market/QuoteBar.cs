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
*/

using System;
using System.Globalization;
using QuantConnect.Util;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// QuoteBar class for second and minute resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class QuoteBar : BaseData, IBar
    {
        // scale factor used in QC equity/forex data files
        private const decimal _scaleFactor = 1 / 10000m;

        /// <summary>
        /// Average bid size
        /// </summary>
        public long LastBidSize { get; set; }
        
        /// <summary>
        /// Average ask size
        /// </summary>
        public long LastAskSize { get; set; }

        /// <summary>
        /// Bid OHLC
        /// </summary>
        public Bar Bid { get; set; }

        /// <summary>
        /// Ask OHLC
        /// </summary>
        public Bar Ask { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open
        {
            get
            {
                if (Bid != null && Ask != null)
                {
                    return (Bid.Open + Ask.Open) / 2m;
                }
                if (Bid != null)
                {
                    return Bid.Open;
                }
                if (Ask != null)
                {
                    return Ask.Open;
                }
                return 0m;
            }
        }

        /// <summary>
        /// High price of the QuoteBar during the time period.
        /// </summary>
        public decimal High
        {
            get
            {
                if (Bid != null && Ask != null)
                {
                    return (Bid.High + Ask.High) / 2m;
                }
                if (Bid != null)
                {
                    return Bid.High;
                }
                if (Ask != null)
                {
                    return Ask.High;
                }
                return 0m;
            }
        }

        /// <summary>
        /// Low price of the QuoteBar during the time period.
        /// </summary>
        public decimal Low
        {
            get
            {
                if (Bid != null && Ask != null)
                {
                    return (Bid.Low + Ask.Low) / 2m;
                }
                if (Bid != null)
                {
                    return Bid.Low;
                }
                if (Ask != null)
                {
                    return Ask.Low;
                }
                return 0m;
            }
        }

        /// <summary>
        /// Closing price of the QuoteBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get
            {
                if (Bid != null && Ask != null)
                {
                    return (Bid.Close + Ask.Close) / 2m;
                }
                if (Bid != null)
                {
                    return Bid.Close;
                }
                if (Ask != null)
                {
                    return Ask.Close;
                }
                return Value;
            }
        }

        /// <summary>
        /// The closing time of this bar, computed via the Time and Period
        /// </summary>
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Period = value - Time; }
        }

        /// <summary>
        /// The period of this quote bar, (second, minute, daily, ect...)
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Default initializer to setup an empty quotebar.
        /// </summary>
        public QuoteBar()
        {
            Symbol = Symbol.Empty;
            Time = new DateTime();
            Bid = new Bar();
            Ask = new Bar();
            Value = 0;
            Period = TimeSpan.FromMinutes(1);
            DataType = MarketDataType.QuoteBar;
        }

        /// <summary>
        /// Initialize Quote Bar with Bid(OHLC) and Ask(OHLC) Values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="bid">Bid OLHC bar</param>
        /// <param name="lastBidSize">Average bid size over period</param>
        /// <param name="ask">Ask OLHC bar</param>
        /// <param name="lastAskSize">Average ask size over period</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public QuoteBar(DateTime time, Symbol symbol, IBar bid, long lastBidSize, IBar ask, long lastAskSize, TimeSpan? period = null)
        {
            Symbol = symbol;
            Time = time;
            Bid = bid == null ? null : new Bar(bid.Open, bid.High, bid.Low, bid.Close);
            Ask = ask == null ? null : new Bar(ask.Open, ask.High, ask.Low, ask.Close);
            if (Bid != null) LastBidSize = lastBidSize;
            if (Ask != null) LastAskSize = lastAskSize;
            Value = Close;
            Period = period ?? TimeSpan.FromMinutes(1);
            DataType = MarketDataType.QuoteBar;
        }

        /// <summary>
        /// Update the quotebar - build the bar from this pricing information:
        /// </summary>
        /// <param name="lastTrade">The last trade price</param>
        /// <param name="bidPrice">Current bid price</param>
        /// <param name="askPrice">Current asking price</param>
        /// <param name="volume">Volume of this trade</param>
        /// <param name="bidSize">The size of the current bid, if available, if not, pass 0</param>
        /// <param name="askSize">The size of the current ask, if available, if not, pass 0</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume, decimal bidSize, decimal askSize)
        {
            // update our bid and ask bars - handle null values, this is to give good values for midpoint OHLC
            if (Bid == null && bidPrice != 0) Bid = new Bar();
            if (Bid != null) Bid.Update(bidPrice);

            if (Ask == null && askPrice != 0) Ask = new Bar();
            if (Ask != null) Ask.Update(askPrice);

            if (bidSize > 0) 
            {
                LastBidSize = (long) bidSize;
            }
            
            if (askSize > 0)
            {
                LastAskSize = (long) askSize;
            }

            // be prepared for updates without trades
            if (lastTrade != 0) Value = lastTrade;
            else if (askPrice != 0) Value = askPrice;
            else if (bidPrice != 0) Value = bidPrice;
        }

        /// <summary>
        /// QuoteBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var quoteBar = new QuoteBar
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            var csv = line.ToCsv(10);
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                quoteBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                quoteBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }

            // only create the bid if it exists in the file
            if (csv[1].Length != 0 || csv[2].Length != 0 || csv[3].Length != 0 || csv[4].Length != 0)
            {
                quoteBar.Bid = new Bar
                {
                    Open = config.GetNormalizedPrice(csv[1].ToDecimal()*_scaleFactor),
                    High = config.GetNormalizedPrice(csv[2].ToDecimal()*_scaleFactor),
                    Low = config.GetNormalizedPrice(csv[3].ToDecimal()*_scaleFactor),
                    Close = config.GetNormalizedPrice(csv[4].ToDecimal()*_scaleFactor)
                };
                quoteBar.LastBidSize = csv[5].ToInt64();
            }
            else
            {
                quoteBar.Bid = null;
            }

            // only create the ask if it exists in the file
            if (csv[6].Length != 0 || csv[7].Length != 0 || csv[8].Length != 0 || csv[9].Length != 0)
            {
                quoteBar.Ask = new Bar
                {
                    Open = config.GetNormalizedPrice(csv[6].ToDecimal()*_scaleFactor),
                    High = config.GetNormalizedPrice(csv[7].ToDecimal()*_scaleFactor),
                    Low = config.GetNormalizedPrice(csv[8].ToDecimal()*_scaleFactor),
                    Close = config.GetNormalizedPrice(csv[9].ToDecimal()*_scaleFactor)
                };
                quoteBar.LastAskSize = csv[10].ToInt64();
            }
            else
            {
                quoteBar.Ask = null;
            }

            quoteBar.Value = quoteBar.Close;

            return quoteBar;
        }

        /// <summary>
        /// Get Source for Custom Data File
        /// >> What source file location would you prefer for each type of usage:
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request if source spread across multiple files</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String source location of the file</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.LocalFile);
            }

            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
            if (config.SecurityType == SecurityType.Option)
            {
                source += "#" + LeanData.GenerateZipEntryName(config.Symbol, date, config.Resolution, config.TickType);
            }
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Return a new instance clone of this quote bar, used in fill forward
        /// </summary>
        /// <returns>A clone of the current quote bar</returns>
        public override BaseData Clone()
        {
            return new QuoteBar
            {
                Ask = Ask == null ? null : Ask.Clone(),
                Bid = Bid == null ? null : Bid.Clone(),
                LastAskSize = LastAskSize,
                LastBidSize = LastBidSize,
                Symbol = Symbol,
                Time = Time,
                Period = Period,
                Value = Value,
                DataType = DataType
            };
        }
    }
}
