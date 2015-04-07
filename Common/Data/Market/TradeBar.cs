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
using QuantConnect.Logging;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// TradeBar class for second and minute resolution data:
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    public class TradeBar : BaseData
    {
        /********************************************************
        * CLASS VARIABLES
        *********************************************************/
        /// <summary>
        /// Volume:
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price of the TradeBar during the time period.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price of the TradeBar during the time period.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price of the TradeBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        public decimal Close
        {
            get { return Value; }
            set { Value = value; }
        }

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public string Symbol;

        //In Base Class: DateTime Of this TradeBar
        //public DateTime Time;

        /********************************************************
        * CLASS CONSTRUCTORS
        *********************************************************/
        /// <summary>
        /// Default initializer to setup an empty tradebar.
        /// </summary>
        public TradeBar()
        {
            Symbol = "";
            Time = new DateTime();
            Value = 0;
            DataType = MarketDataType.TradeBar;
            Open = 0;
            High = 0;
            Low = 0;
            Close = 0;
            Volume = 0;
        }

        /// <summary>
        /// Cloner constructor for implementing fill forward.
        /// Return a new instance with the same values as this original.
        /// </summary>
        /// <param name="original">Original tradebar object we seek to clone</param>
        public TradeBar(TradeBar original)
        {
            Time = new DateTime(original.Time.Ticks);
            Symbol = original.Symbol;
            Value = original.Close;
            Open = original.Open;
            High = original.High;
            Low = original.Low;
            Close = original.Close;
            Volume = original.Volume;
        }

        /// <summary>
        /// Parse a line from CSV data sources into our trade bars.
        /// </summary>
        /// <param name="config">Configuration class object for this data subscription</param>
        /// <param name="baseDate">Base date of this tradebar line</param>
        /// <param name="line">CSV line from source data file</param>
        /// <param name="datafeed">Datafeed this csv line is sourced from (backtesting or live)</param>
        public TradeBar(SubscriptionDataConfig config, string line, DateTime baseDate, DataFeedEndpoint datafeed = DataFeedEndpoint.Backtesting)
        {
            try
            {
                //Parse the data into a trade bar:
                var csv = line.Split(',');
                const decimal scaleFactor = 10000m;
                Symbol = config.Symbol;

                switch (config.Security)
                {
                    //Equity File Data Format:
                    case SecurityType.Equity:
                        Time = baseDate.Date.AddMilliseconds(Convert.ToInt32(csv[0]));
                        Open = (csv[1].ToDecimal() / scaleFactor) * config.PriceScaleFactor;  //  Convert.ToDecimal(csv[1]) / scaleFactor;
                        High = (csv[2].ToDecimal() / scaleFactor) * config.PriceScaleFactor;  // Using custom "ToDecimal" conversion for speed.
                        Low = (csv[3].ToDecimal() / scaleFactor) * config.PriceScaleFactor;
                        Close = (csv[4].ToDecimal() / scaleFactor) * config.PriceScaleFactor;
                        Volume = Convert.ToInt64(csv[5]);
                        break;

                    //FOREX has a different data file format:
                    case SecurityType.Forex:
                        Time = DateTime.ParseExact(csv[0], "yyyyMMdd HH:mm:ss.ffff", CultureInfo.InvariantCulture);
                        Open = csv[1].ToDecimal();
                        High = csv[2].ToDecimal();
                        Low = csv[3].ToDecimal();
                        Close = csv[4].ToDecimal();
                        break;
                }
                //base.Value = Close;
            }
            catch (Exception err)
            {
                Log.Error("DataModels: TradeBar(): Error Initializing - " + config.Security + " - " + err.Message + " - " + line);
            }
        }

        /// <summary>
        /// Initialize Trade Bar with OHLC Values:
        /// </summary>
        /// <param name="time">DateTime Timestamp of the bar</param>
        /// <param name="symbol">Market MarketType Symbol</param>
        /// <param name="open">Decimal Opening Price</param>
        /// <param name="high">Decimal High Price of this bar</param>
        /// <param name="low">Decimal Low Price of this bar</param>
        /// <param name="close">Decimal Close price of this bar</param>
        /// <param name="volume">Volume sum over day</param>
        public TradeBar(DateTime time, string symbol, decimal open, decimal high, decimal low, decimal close, long volume)
        {
            base.Time = time;
            base.Symbol = symbol;
            base.Value = close;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
        /// </summary>
        /// <param name="datafeed">Destination for the this datafeed - live or backtesting</param>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, DataFeedEndpoint datafeed)
        {
            //Initialize:
            var tradeBar = new TradeBar();

            //Handle end of file:
            if (line == null)
            {
                return null;
            }

            //Select the URL source of the data depending on where the system is trading.
            switch (datafeed)
            {
                //Amazon S3 Backtesting Data:
                case DataFeedEndpoint.Backtesting:
                    //Create a new instance of our tradebar:
                    tradeBar = new TradeBar(config, line, date, datafeed);
                    break;

                //Localhost Data Source
                case DataFeedEndpoint.FileSystem:
                    //Create a new instance of our tradebar:
                    tradeBar = new TradeBar(config, line, date, datafeed);
                    break;

                //QuantConnect Live Tick Stream:
                case DataFeedEndpoint.LiveTrading:
                    break;
            }

            //Return initialized TradeBar:
            return tradeBar;
        }

        /// <summary>
        /// Implement the Clone Method for the TradeBar:
        /// </summary>
        /// <returns></returns>
        public override BaseData Clone()
        {
            //Cleanest way to clone an object is to create a new instance using itself as the arguement.
            return new TradeBar(this);
        }

        /// <summary>
        /// Update the tradebar - build the bar from this pricing information:
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
            Volume += Convert.ToInt32(volume);
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
        /// <param name="datafeed">Source of the datafeed</param>
        /// <returns>String source location of the file</returns>
        public override string GetSource(SubscriptionDataConfig config, DateTime date, DataFeedEndpoint datafeed)
        {
            var source = "";
            var dataType = TickType.Trade;

            switch (datafeed)
            {
                //Backtesting S3 Endpoint:
                case DataFeedEndpoint.Backtesting:
                case DataFeedEndpoint.FileSystem:

                    var dateFormat = "yyyyMMdd";
                    if (config.Security == SecurityType.Forex)
                    {
                        dataType = TickType.Quote;
                        dateFormat = "yyMMdd";
                    }

                    var symbol = String.IsNullOrEmpty(config.MappedSymbol) ? config.Symbol : config.MappedSymbol;
                    source = @"../../../Data/" + config.Security.ToString().ToLower();
                    source += @"/" + config.Resolution.ToString().ToLower() + @"/" + symbol.ToLower() + @"/";
                    source += date.ToString(dateFormat) + "_" + dataType.ToString().ToLower() + ".zip";
                    break;

                //Live Trading Endpoint: Fake, not actually used but need for consistency with backtesting system. Set to "" so will not use subscription reader.
                case DataFeedEndpoint.LiveTrading:
                    source = "";
                    break;
            }
            return source;
        }
    } // End Trade Bar Class
}
