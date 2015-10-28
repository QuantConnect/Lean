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
using System.IO;
using QuantConnect.Logging;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// QuoteBar class for second and minute resolution data: 
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class QuoteBar : BaseData, IBar
    {
        // scale factor used in QC equity/forex data files
        private const decimal _scaleFactor = 10000m;

        /// <summary>
        /// Average bid size
        /// </summary>
        public long AvgBidSize { get; set; }
        
        /// <summary>
        /// Average ask size
        /// </summary>
        public long AvgAskSize { get; set; }

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
        public decimal Open { get; set; }

        /// <summary>
        /// High price of the QuoteBar during the time period.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price of the QuoteBar during the time period.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price of the QuoteBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get { return Value; }
            set { Value = value; }
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

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public Symbol Symbol;

        //In Base Class: DateTime Of this QuoteBar
        //public DateTime Time;

        /// <summary>
        /// Default initializer to setup an empty quotebar.
        /// </summary>
        public QuoteBar()
        {
            Symbol = Symbol.Empty;
            Time = new DateTime();
            Value = 0;
            DataType = MarketDataType.QuoteBar;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            AvgBidSize = 0;
            AvgAskSize = 0;
            Bid = new Bar();
            Ask = new Bar();
            Period = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Cloner constructor for implementing fill forward. 
        /// Return a new instance with the same values as this original.
        /// </summary>
        /// <param name="original">Original quotebar object we seek to clone</param>
        public QuoteBar(QuoteBar original)
        {
            DataType = MarketDataType.QuoteBar;
            Time = new DateTime(original.Time.Ticks);
            Symbol = original.Symbol;
            Value = original.Close;
            Open = original.Open;
            High = original.High;
            Low = original.Low;
            Close = original.Close;
            AvgBidSize = original.AvgBidSize;
            AvgAskSize = original.AvgAskSize;
            Bid = original.Bid;
            Ask = original.Ask;
            Period = original.Period;
        }

        /// <summary>
        /// Initialize Quote Bar with Bid(OHLC) and Ask(OHLC) Values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="bidopen">Decimal Opening Price of Bid</param>
        /// <param name="bidhigh">Decimal High Price of this bid bar</param>
        /// <param name="bidlow">Decimal Low Price of this bid bar</param>
        /// <param name="bidclose">Decimal Close price of this bid bar</param>
        /// <param name="avgbidsize">Average bid size over period</param>
        /// <param name="askopen">Decimal Opening Price of Ask</param>
        /// <param name="askhigh">Decimal High Price of this ask bar</param>
        /// <param name="asklow">Decimal Low Price of this ask bar</param>
        /// <param name="askclose">Decimal Close price of this ask bar</param>
        /// <param name="avgasksize">Average ask size over period</param>
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public QuoteBar(DateTime time, Symbol symbol, decimal bidopen, decimal bidhigh, decimal bidlow, decimal bidclose, long avgbidsize, decimal askopen, decimal askhigh, decimal asklow, decimal askclose, long avgasksize, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = (bidclose + askclose) / 2;
            Open = (bidopen + askopen) / 2;
            High = (bidhigh + askhigh) / 2;
            Low = (bidlow + asklow) / 2;
            Close = (bidclose + askclose) / 2;
            AvgBidSize = avgbidsize;
            AvgAskSize = avgasksize;
            Period = period ?? TimeSpan.FromMinutes(1);
            Bid = new Bar(bidopen, bidhigh, bidlow, bidclose);
            Ask = new Bar(askopen, askhigh, asklow, askclose);
            DataType = MarketDataType.QuoteBar;
        }

        /// <summary>
        /// Update the quotebar - build the bar from this pricing information:
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price (not used) </param>
        /// <param name="askPrice">Current asking price (not used) </param>
        /// <param name="bidSize">Size of of this bid</param>
        /// <param name="askSize">Size of this ask</param>
        public void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal bidSize, decimal askSize)
        {
            var midpoint = (bidPrice + askPrice) / 2;
            var midhigh = Math.Max(lastTrade, midpoint);
            var midlow = Math.Min(lastTrade, midpoint);

            //Assumed not set yet. Will fail for custom time series where "price" $0 is a possibility.
            if (Open == 0) Open = lastTrade;
            if (Bid.Open == 0) Bid.Open = bidPrice;
            if (Ask.Open == 0) Ask.Open = askPrice;

            if (midhigh > High) High = midhigh;
            if (bidPrice > Bid.High) Bid.High = bidPrice;
            if (askPrice > Ask.High) Ask.High = askPrice;

            if (midlow < Low) Low = midlow;
            if (bidPrice < Bid.Low) Bid.Low = bidPrice;
            if (askPrice < Ask.Low) Ask.Low = askPrice;

            // Note: we are summing instead of averaging!!!
            AvgBidSize += Convert.ToInt32(bidSize);
            AvgAskSize += Convert.ToInt32(askSize);

            //Always set the closing price;
            Close = lastTrade;
            Bid.Close = bidPrice;
            Ask.Close = askPrice;
            Value = lastTrade;
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
            //Handle end of file:
            if (line == null)
            {
                return null;
            }

            if (isLiveMode)
            {
                return new QuoteBar();
            }

            try
            {
                switch (config.SecurityType)
                {
                    //Equity File Data Format:
                    case SecurityType.Equity:
                        return ParseEquity<QuoteBar>(config, line, date);

                    //FOREX has a different data file format:
                    case SecurityType.Forex:
                        return ParseForex<QuoteBar>(config, line, date);
                }
            }
            catch (Exception err)
            {
                Log.Error("DataModels: QuoteBar(): Error Initializing - " + config.SecurityType + " - " + err.Message + " - " + line);
            }

            // if we couldn't parse it above return a default instance
            return new QuoteBar { Symbol = config.Symbol, Period = config.Increment };
        }

        /// <summary>
        /// Parses the quote bar data line assuming QC data formats
        /// </summary>
        public static QuoteBar Parse(SubscriptionDataConfig config, string line, DateTime baseDate)
        {
            if (config.SecurityType == SecurityType.Forex)
            {
                return ParseForex<QuoteBar>(config, line, baseDate);
            }
            if (config.SecurityType == SecurityType.Equity)
            {
                return ParseEquity<QuoteBar>(config, line, baseDate);
            }

            return null;
        }

        /// <summary>
        /// Parses equity quote bar data into the specified quotebar type, useful for custom types with OHLC data deriving from QuoteBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from QuoteBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static T ParseEquity<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : QuoteBar, new()
        {
            var quoteBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };

            var csv = line.Split(',');
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                quoteBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                quoteBar.Open = config.GetNormalizedPrice(Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture) / _scaleFactor);
                quoteBar.High = config.GetNormalizedPrice(Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture) / _scaleFactor);
                quoteBar.Low = config.GetNormalizedPrice(Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture) / _scaleFactor);
                quoteBar.Close = config.GetNormalizedPrice(Convert.ToDecimal(csv[4], CultureInfo.InvariantCulture) / _scaleFactor);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                quoteBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32());
                quoteBar.Open = config.GetNormalizedPrice(csv[1].ToDecimal() / _scaleFactor);
                quoteBar.High = config.GetNormalizedPrice(csv[2].ToDecimal() / _scaleFactor);
                quoteBar.Low = config.GetNormalizedPrice(csv[3].ToDecimal() / _scaleFactor);
                quoteBar.Close = config.GetNormalizedPrice(csv[4].ToDecimal() / _scaleFactor);
            }

            quoteBar.AvgBidSize = csv[5].ToInt64();
            return quoteBar;
        }

        /// <summary>
        /// Parses forex quote bar data into the specified quotebar type, useful for custom types with OHLC data deriving from QuoteBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from QuoteBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseForex<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : QuoteBar, new()
        {
            var quoteBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };

            var csv = line.Split(',');
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                quoteBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                quoteBar.Open = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture);
                quoteBar.High = Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture);
                quoteBar.Low = Convert.ToDecimal(csv[3], CultureInfo.InvariantCulture);
                quoteBar.Close = Convert.ToDecimal(csv[4], CultureInfo.InvariantCulture);
            }
            else
            {
                //Fast decimal conversion
                quoteBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32());
                quoteBar.Open = csv[1].ToDecimal();
                quoteBar.High = csv[2].ToDecimal();
                quoteBar.Low = csv[3].ToDecimal();
                quoteBar.Close = csv[4].ToDecimal();
            }
            return quoteBar;
        }


        /// <summary>
        /// Update the quotebar - build the bar from this pricing information:
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price (not used) </param>
        /// <param name="askPrice">Current asking price (not used) </param>
        /// <param name="volume">Volume of this trade</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume)
        {
            //Assumed not set yet. Will fail for custom time series where "price" $0 is a possibility.
            if (Open == 0) Open = lastTrade;
            if (lastTrade > High) High = lastTrade;
            if (lastTrade < Low) Low = lastTrade;
            //Volume is the total summed volume of trades in this bar:
            AvgBidSize += Convert.ToInt32(volume);
            //Always set the closing price;
            Close = lastTrade;
            Value = lastTrade;
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

            var dataType = config.SecurityType == SecurityType.Forex ? TickType.Quote : TickType.Trade;
            var securityTypePath = config.SecurityType.ToString().ToLower();
            var resolutionPath = config.Resolution.ToString().ToLower();
            var symbolPath = (string.IsNullOrEmpty(config.MappedSymbol) ? config.Symbol.Permtick : config.MappedSymbol).ToLower();
            var market = config.Market.ToLower();
            var filename = date.ToString(DateFormat.EightCharacter) + "_" + dataType.ToString().ToLower() + ".zip";


            if (config.Resolution == Resolution.Hour || config.Resolution == Resolution.Daily)
            {
                // hourly/daily data is all in a single file, no sub directories
                filename = symbolPath + ".zip";
                symbolPath = string.Empty;
            }

            var source = Path.Combine(Constants.DataFolder, securityTypePath, market, resolutionPath, symbolPath, filename);

            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>Cloned QuoteBar</returns>
        public override BaseData Clone()
        {
            return (BaseData)MemberwiseClone();
        }

    } // End Quote Bar Class
}
