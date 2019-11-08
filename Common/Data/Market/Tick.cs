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
using QuantConnect.Util;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Tick class is the base representation for tick data. It is grouped into a Ticks object
    /// which implements IDictionary and passed into an OnData event handler.
    /// </summary>
    public class Tick : BaseData
    {
        /// <summary>
        /// Type of the Tick: Trade or Quote.
        /// </summary>
        public TickType TickType = TickType.Trade;

        /// <summary>
        /// Quantity exchanged in a trade.
        /// </summary>
        public decimal Quantity = 0;

        /// <summary>
        /// Exchange we are executing on. String short code expanded in the MarketCodes.US global dictionary
        /// </summary>
        public string Exchange = "";

        /// <summary>
        /// Sale condition for the tick.
        /// </summary>
        public string SaleCondition = "";

        /// <summary>
        /// Bool whether this is a suspicious tick
        /// </summary>
        public bool Suspicious = false;

        /// <summary>
        /// Bid Price for Tick
        /// </summary>
        /// <remarks>QuantConnect does not currently have quote data but was designed to handle ticks and quotes</remarks>
        public decimal BidPrice = 0;

        /// <summary>
        /// Asking price for the Tick quote.
        /// </summary>
        /// <remarks>QuantConnect does not currently have quote data but was designed to handle ticks and quotes</remarks>
        public decimal AskPrice = 0;

        /// <summary>
        /// Alias for "Value" - the last sale for this asset.
        /// </summary>
        public decimal LastPrice
        {
            get
            {
                return Value;
            }
        }

        /// <summary>
        /// Size of bid quote.
        /// </summary>
        public decimal BidSize = 0;

        /// <summary>
        /// Size of ask quote.
        /// </summary>
        public decimal AskSize = 0;

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public Symbol Symbol;

        //In Base Class: DateTime Of this TradeBar
        //public DateTime Time;

        /// <summary>
        /// Initialize tick class with a default constructor.
        /// </summary>
        public Tick()
        {
            Value = 0;
            Time = new DateTime();
            DataType = MarketDataType.Tick;
            Symbol = Symbol.Empty;
            TickType = TickType.Trade;
            Quantity = 0;
            Exchange = "";
            SaleCondition = "";
            Suspicious = false;
            BidSize = 0;
            AskSize = 0;
        }

        /// <summary>
        /// Cloner constructor for fill forward engine implementation. Clone the original tick into this new tick:
        /// </summary>
        /// <param name="original">Original tick we're cloning</param>
        public Tick(Tick original)
        {
            Symbol = original.Symbol;
            Time = new DateTime(original.Time.Ticks);
            Value = original.Value;
            BidPrice = original.BidPrice;
            AskPrice = original.AskPrice;
            Exchange = original.Exchange;
            SaleCondition = original.SaleCondition;
            Quantity = original.Quantity;
            Suspicious = original.Suspicious;
            DataType = MarketDataType.Tick;
            TickType = original.TickType;
            BidSize = original.BidSize;
            AskSize = original.AskSize;
        }

        /// <summary>
        /// Constructor for a FOREX tick where there is no last sale price. The volume in FX is so high its rare to find FX trade data.
        /// To fake this the tick contains bid-ask prices and the last price is the midpoint.
        /// </summary>
        /// <param name="time">Full date and time</param>
        /// <param name="symbol">Underlying currency pair we're trading</param>
        /// <param name="bid">FX tick bid value</param>
        /// <param name="ask">FX tick ask value</param>
        public Tick(DateTime time, Symbol symbol, decimal bid, decimal ask)
        {
            DataType = MarketDataType.Tick;
            Time = time;
            Symbol = symbol;
            Value = (bid + ask) / 2;
            TickType = TickType.Quote;
            BidPrice = bid;
            AskPrice = ask;
        }

        /// <summary>
        /// Initializer for a last-trade equity tick with bid or ask prices.
        /// </summary>
        /// <param name="time">Full date and time</param>
        /// <param name="symbol">Underlying equity security symbol</param>
        /// <param name="bid">Bid value</param>
        /// <param name="ask">Ask value</param>
        /// <param name="last">Last trade price</param>
        public Tick(DateTime time, Symbol symbol, decimal last, decimal bid, decimal ask)
        {
            DataType = MarketDataType.Tick;
            Time = time;
            Symbol = symbol;
            Value = last;
            TickType = TickType.Quote;
            BidPrice = bid;
            AskPrice = ask;
        }

        /// <summary>
        /// Constructor for QuantConnect FXCM Data source:
        /// </summary>
        /// <param name="symbol">Symbol for underlying asset</param>
        /// <param name="line">CSV line of data from FXCM</param>
        public Tick(Symbol symbol, string line)
        {
            var csv = line.Split(',');
            DataType = MarketDataType.Tick;
            Symbol = symbol;
            Time = DateTime.ParseExact(csv[0], DateFormat.Forex, CultureInfo.InvariantCulture);
            Value = (BidPrice + AskPrice) / 2;
            TickType = TickType.Quote;
            BidPrice = Convert.ToDecimal(csv[1], CultureInfo.InvariantCulture);
            AskPrice = Convert.ToDecimal(csv[2], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Constructor for QuantConnect tick data
        /// </summary>
        /// <param name="symbol">Symbol for underlying asset</param>
        /// <param name="line">CSV line of data from QC tick csv</param>
        /// <param name="baseDate">The base date of the tick</param>
        public Tick(Symbol symbol, string line, DateTime baseDate)
        {
            var csv = line.Split(',');
            DataType = MarketDataType.Tick;
            Symbol = symbol;
            Time = baseDate.Date.AddMilliseconds(csv[0].ToInt32());
            Value = csv[1].ToDecimal() / GetScaleFactor(symbol.SecurityType);
            TickType = TickType.Trade;
            Quantity = csv[2].ToDecimal();
            Exchange = csv[3].Trim();
            SaleCondition = csv[4];
            Suspicious = csv[5].ToInt32() == 1;
        }

        /// <summary>
        /// Parse a tick data line from quantconnect zip source files.
        /// </summary>
        /// <param name="line">CSV source line of the compressed source</param>
        /// <param name="date">Base date for the tick (ticks date is stored as int milliseconds since midnight)</param>
        /// <param name="config">Subscription configuration object</param>
        public Tick(SubscriptionDataConfig config, string line, DateTime date)
        {
            try
            {
                DataType = MarketDataType.Tick;

                // Which security type is this data feed:
                var scaleFactor = GetScaleFactor(config.SecurityType);

                switch (config.SecurityType)
                {
                    case SecurityType.Equity:
                    {
                        var csv = line.ToCsv(6);
                        Symbol = config.Symbol;
                        Time = date.Date.AddMilliseconds(csv[0].ToInt64()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
                        Value = csv[1].ToDecimal() / scaleFactor;
                        TickType = TickType.Trade;
                        Quantity = csv[2].ToDecimal();
                        if (csv.Count > 3)
                        {
                            Exchange = csv[3];
                            SaleCondition = csv[4];
                            Suspicious = (csv[5] == "1");
                        }
                        break;
                    }

                    case SecurityType.Forex:
                    case SecurityType.Cfd:
                    {
                        var csv = line.ToCsv(3);
                        Symbol = config.Symbol;
                        TickType = TickType.Quote;
                        var ticks = (long)(csv[0].ToDecimal() * TimeSpan.TicksPerMillisecond);
                        Time = date.Date.AddTicks(ticks)
                            .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
                        BidPrice = csv[1].ToDecimal();
                        AskPrice = csv[2].ToDecimal();
                        Value = (BidPrice + AskPrice) / 2;
                        break;
                    }

                    case SecurityType.Crypto:
                    {
                        TickType = config.TickType;
                        Symbol = config.Symbol;
                        Exchange = config.Market;

                        if (TickType == TickType.Trade)
                        {
                            var csv = line.ToCsv(3);
                            Time = date.Date.AddMilliseconds((double)csv[0].ToDecimal())
                                .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
                            Value = csv[1].ToDecimal();
                            Quantity = csv[2].ToDecimal();
                        }

                        if (TickType == TickType.Quote)
                        {
                            var csv = line.ToCsv(6);
                            Time = date.Date.AddMilliseconds((double)csv[0].ToDecimal())
                                .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
                            BidPrice = csv[1].ToDecimal();
                            BidSize = csv[2].ToDecimal();
                            AskPrice = csv[3].ToDecimal();
                            AskSize = csv[4].ToDecimal();
                            Value = (BidPrice + AskPrice) / 2;
                        }
                        break;
                    }
                    case SecurityType.Future:
                    case SecurityType.Option:
                    {
                        var csv = line.ToCsv(7);
                        TickType = config.TickType;
                        Time = date.Date.AddMilliseconds(csv[0].ToInt64())
                            .ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
                        Symbol = config.Symbol;

                        if (TickType == TickType.Trade)
                        {
                            Value = csv[1].ToDecimal()/scaleFactor;
                            Quantity = csv[2].ToDecimal();
                            Exchange = csv[3];
                            SaleCondition = csv[4];
                            Suspicious = csv[5] == "1";
                        }
                        else if (TickType == TickType.OpenInterest)
                        {
                            Value = csv[1].ToDecimal();
                        }
                        else
                        {
                            if (csv[1].Length != 0)
                            {
                                BidPrice = csv[1].ToDecimal()/scaleFactor;
                                BidSize = csv[2].ToDecimal();
                            }
                            if (csv[3].Length != 0)
                            {
                                AskPrice = csv[3].ToDecimal()/scaleFactor;
                                AskSize = csv[4].ToDecimal();
                            }
                            Exchange = csv[5];
                            Suspicious = csv[6] == "1";

                            if (BidPrice != 0)
                            {
                                if (AskPrice != 0)
                                {
                                    Value = (BidPrice + AskPrice)/2m;
                                }
                                else
                                {
                                    Value = BidPrice;
                                }
                            }
                            else
                            {
                                Value = AskPrice;
                            }
                        }

                        break;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Tick implementation of reader method: read a line of data from the source and convert it to a tick object.
        /// </summary>
        /// <param name="config">Subscription configuration object for algorithm</param>
        /// <param name="line">Line from the datafeed source</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>New Initialized tick</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // currently ticks don't come through the reader function
                return new Tick();
            }

            return new Tick(config, line, date);
        }

        /// <summary>
        /// Get source for tick data feed - not used with QuantConnect data sources implementation.
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source request if source spread across multiple files</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>String source location of the file to be opened with a stream</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                // this data type is streamed in live mode
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.Streaming);
            }

            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
            if (config.SecurityType == SecurityType.Option ||
                config.SecurityType == SecurityType.Future)
            {
                source += "#" + LeanData.GenerateZipEntryName(config.Symbol, date, config.Resolution, config.TickType);
            }
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Update the tick price information - not used.
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price</param>
        /// <param name="askPrice">Current asking price</param>
        /// <param name="volume">Volume of this trade</param>
        /// <param name="bidSize">The size of the current bid, if available</param>
        /// <param name="askSize">The size of the current ask, if available</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume, decimal bidSize, decimal askSize)
        {
            Value = lastTrade;
            BidPrice = bidPrice;
            AskPrice = askPrice;
            BidSize = bidSize;
            AskSize = askSize;
            Quantity = Convert.ToDecimal(volume);
        }

        /// <summary>
        /// Check if tick contains valid data (either a trade, or a bid or ask)
        /// </summary>
        public bool IsValid()
        {
            return (TickType == TickType.Trade && LastPrice > 0.0m && Quantity > 0) ||
                   (TickType == TickType.Quote && AskPrice > 0.0m && AskSize > 0) ||
                   (TickType == TickType.Quote && BidPrice > 0.0m && BidSize > 0) ||
                   (TickType == TickType.OpenInterest && Value > 0);
        }

        /// <summary>
        /// Clone implementation for tick class:
        /// </summary>
        /// <returns>New tick object clone of the current class values.</returns>
        public override BaseData Clone()
        {
            return new Tick(this);
        }

        /// <summary>
        /// Formats a string with the symbol and value.
        /// </summary>
        /// <returns>string - a string formatted as SPY: 167.753</returns>
        public override string ToString()
        {
            switch (TickType)
            {
                case TickType.Trade:
                    return $"{Symbol}: Price: {Price} Quantity: {Quantity}";

                case TickType.Quote:
                    return $"{Symbol}: Bid: {BidSize}@{BidPrice} Ask: {AskSize}@{AskPrice}";

                case TickType.OpenInterest:
                    return $"{Symbol}: OpenInterest: {Value}";

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static decimal GetScaleFactor(SecurityType securityType)
        {
            return securityType == SecurityType.Equity || securityType == SecurityType.Option ? 10000m : 1;
        }
    }
}