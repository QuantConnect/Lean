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
using ProtoBuf;
using System.IO;
using System.Threading;
using QuantConnect.Util;
using System.Globalization;
using QuantConnect.Logging;
using static QuantConnect.StringExtensions;
using QuantConnect.Python;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// TradeBar class for second and minute resolution data:
    /// An OHLC implementation of the QuantConnect BaseData class with parameters for candles.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class TradeBar : BaseData, IBaseDataBar
    {
        // scale factor used in QC equity/forex data files
        private const decimal _scaleFactor = 1 / 10000m;

        private int _initialized;
        private decimal _open;
        private decimal _high;
        private decimal _low;

        /// <summary>
        /// Volume:
        /// </summary>
        [ProtoMember(101)]
        public virtual decimal Volume { get; set; }

        /// <summary>
        /// Opening price of the bar: Defined as the price at the start of the time period.
        /// </summary>
        [ProtoMember(102)]
        public virtual decimal Open
        {
            get { return _open; }
            set
            {
                Initialize(value);
                _open = value;
            }
        }

        /// <summary>
        /// High price of the TradeBar during the time period.
        /// </summary>
        [ProtoMember(103)]
        public virtual decimal High
        {
            get { return _high; }
            set
            {
                Initialize(value);
                _high = value;
            }
        }

        /// <summary>
        /// Low price of the TradeBar during the time period.
        /// </summary>
        [ProtoMember(104)]
        public virtual decimal Low
        {
            get { return _low; }
            set
            {
                Initialize(value);
                _low = value;
            }
        }

        /// <summary>
        /// Closing price of the TradeBar. Defined as the price at Start Time + TimeSpan.
        /// </summary>
        [ProtoMember(105)]
        public virtual decimal Close
        {
            get { return Value; }
            set
            {
                Initialize(value);
                Value = value;
            }
        }

        /// <summary>
        /// The closing time of this bar, computed via the Time and Period
        /// </summary>
        [PandasIgnore]
        public override DateTime EndTime
        {
            get { return Time + Period; }
            set { Period = value - Time; }
        }

        /// <summary>
        /// The period of this trade bar, (second, minute, daily, ect...)
        /// </summary>
        [ProtoMember(106)]
        [PandasIgnore]
        public virtual TimeSpan Period { get; set; }

        //In Base Class: Alias of Closing:
        //public decimal Price;

        //Symbol of Asset.
        //In Base Class: public Symbol Symbol;

        //In Base Class: DateTime Of this TradeBar
        //public DateTime Time;

        /// <summary>
        /// Default initializer to setup an empty tradebar.
        /// </summary>
        public TradeBar()
        {
            Symbol = Symbol.Empty;
            DataType = MarketDataType.TradeBar;
            Period = QuantConnect.Time.OneMinute;
        }

        /// <summary>
        /// Cloner constructor for implementing fill forward.
        /// Return a new instance with the same values as this original.
        /// </summary>
        /// <param name="original">Original tradebar object we seek to clone</param>
        public TradeBar(TradeBar original)
        {
            DataType = MarketDataType.TradeBar;
            Time = new DateTime(original.Time.Ticks);
            Symbol = original.Symbol;
            Value = original.Close;
            Open = original.Open;
            High = original.High;
            Low = original.Low;
            Close = original.Close;
            Volume = original.Volume;
            Period = original.Period;
            _initialized = 1;
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
        /// <param name="period">The period of this bar, specify null for default of 1 minute</param>
        public TradeBar(DateTime time, Symbol symbol, decimal open, decimal high, decimal low, decimal close, decimal volume, TimeSpan? period = null)
        {
            Time = time;
            Symbol = symbol;
            Value = close;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Period = period ?? QuantConnect.Time.OneMinute;
            DataType = MarketDataType.TradeBar;
            _initialized = 1;
        }

        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it line by line into the engine.
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
                return new TradeBar();
            }

            try
            {
                switch (config.SecurityType)
                {
                    //Equity File Data Format:
                    case SecurityType.Equity:
                        return ParseEquity(config, line, date);

                    //FOREX has a different data file format:
                    case SecurityType.Forex:
                        return ParseForex(config, line, date);

                    case SecurityType.Crypto:
                    case SecurityType.CryptoFuture:
                        return ParseCrypto(config, line, date);

                    case SecurityType.Cfd:
                        return ParseCfd(config, line, date);

                    case SecurityType.Index:
                        return ParseIndex(config, line, date);

                    case SecurityType.Option:
                    case SecurityType.FutureOption:
                    case SecurityType.IndexOption:
                        return ParseOption(config, line, date);

                    case SecurityType.Future:
                        return ParseFuture(config, line, date);

                }
            }
            catch (Exception err)
            {
                Log.Error(Invariant($"TradeBar.Reader(): Error parsing line: '{line}', Symbol: {config.Symbol.Value}, SecurityType: ") +
                    Invariant($"{config.SecurityType}, Resolution: {config.Resolution}, Date: {date:yyyy-MM-dd}, Message: {err}")
                );
            }

            // if we couldn't parse it above return a default instance
            return new TradeBar { Symbol = config.Symbol, Period = config.Increment };
        }

        /// <summary>
        /// TradeBar Reader: Fetch the data from the QC storage and feed it directly from the stream into the engine.
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="stream">The file data stream</param>
        /// <param name="date">Date of this reader request</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Enumerable iterator for returning each line of the required data.</returns>
        [StubsIgnore]
        public override BaseData Reader(SubscriptionDataConfig config, StreamReader stream, DateTime date, bool isLiveMode)
        {
            //Handle end of file:
            if (stream == null || stream.EndOfStream)
            {
                return null;
            }
            if (isLiveMode)
            {
                return new TradeBar();
            }

            try
            {
                switch (config.SecurityType)
                {
                    //Equity File Data Format:
                    case SecurityType.Equity:
                        return ParseEquity(config, stream, date);

                    //FOREX has a different data file format:
                    case SecurityType.Forex:
                        return ParseForex(config, stream, date);

                    case SecurityType.Crypto:
                    case SecurityType.CryptoFuture:
                        return ParseCrypto(config, stream, date);

                    case SecurityType.Index:
                        return ParseIndex(config, stream, date);

                    case SecurityType.Cfd:
                        return ParseCfd(config, stream, date);

                    case SecurityType.Option:
                    case SecurityType.FutureOption:
                    case SecurityType.IndexOption:
                        return ParseOption(config, stream, date);

                    case SecurityType.Future:
                        return ParseFuture(config, stream, date);

                }
            }
            catch (Exception err)
            {
                Log.Error(Invariant($"TradeBar.Reader(): Error parsing stream, Symbol: {config.Symbol.Value}, SecurityType: ") +
                          Invariant($"{config.SecurityType}, Resolution: {config.Resolution}, Date: {date:yyyy-MM-dd}, Message: {err}")
                );
            }

            // we need to consume a line anyway, to advance the stream
            stream.ReadLine();

            // if we couldn't parse it above return a default instance
            return new TradeBar { Symbol = config.Symbol, Period = config.Increment };
        }

        /// <summary>
        /// Parses the trade bar data line assuming QC data formats
        /// </summary>
        public static TradeBar Parse(SubscriptionDataConfig config, string line, DateTime baseDate)
        {
            switch (config.SecurityType)
            {
                case SecurityType.Equity:
                    return ParseEquity(config, line, baseDate);

                case SecurityType.Forex:
                case SecurityType.Crypto:
                case SecurityType.CryptoFuture:
                    return ParseForex(config, line, baseDate);

                case SecurityType.Cfd:
                    return ParseCfd(config, line, baseDate);
            }

            return null;
        }

        /// <summary>
        /// Parses equity trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static T ParseEquity<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };

            ParseEquity(tradeBar, config, line, date);

            return tradeBar;
        }

        /// <summary>
        /// Parses equity trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static TradeBar ParseEquity(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            var tradeBar = new TradeBar
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };
            StreamParseScale(config, streamReader, date, useScaleFactor: true, tradeBar, true);

            return tradeBar;
        }

        private static void ParseEquity(TradeBar tradeBar, SubscriptionDataConfig config, string line, DateTime date)
        {
            LineParseScale(config, line, date, useScaleFactor: true, tradeBar, hasVolume: true);
        }

        /// <summary>
        /// Parses equity trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">Date of this reader request</param>
        /// <returns></returns>
        public static TradeBar ParseEquity(SubscriptionDataConfig config, string line, DateTime date)
        {
            var tradeBar = new TradeBar
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };
            ParseEquity(tradeBar, config, line, date);
            return tradeBar;
        }

        /// <summary>
        /// Parses forex trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseForex<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };
            LineParseNoScale(config, line, date, tradeBar, hasVolume: false);

            return tradeBar;
        }

        /// <summary>
        /// Parses crypto trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        public static T ParseCrypto<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Symbol = config.Symbol,
                Period = config.Increment
            };
            LineParseNoScale(config, line, date, tradeBar);

            return tradeBar;
        }

        /// <summary>
        /// Parses crypto trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        public static TradeBar ParseCrypto(SubscriptionDataConfig config, string line, DateTime date)
        {
            return LineParseNoScale(config, line, date);
        }

        /// <summary>
        /// Parses crypto trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        public static TradeBar ParseCrypto(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            return StreamParseNoScale(config, streamReader, date);
        }

        /// <summary>
        /// Parses forex trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseForex(SubscriptionDataConfig config, string line, DateTime date)
        {
            return LineParseNoScale(config, line, date, hasVolume: false);
        }

        /// <summary>
        /// Parses forex trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseForex(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            return StreamParseNoScale(config, streamReader, date, hasVolume: false);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseCfd<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            // CFD has the same data format as Forex
            return ParseForex<T>(config, line, date);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseCfd(SubscriptionDataConfig config, string line, DateTime date)
        {
            // CFD has the same data format as Forex
            return ParseForex(config, line, date);
        }

        /// <summary>
        /// Parses CFD trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseCfd(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            // CFD has the same data format as Forex
            return ParseForex(config, streamReader, date);
        }

        /// <summary>
        /// Parses Option trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseOption<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };
            LineParseScale(config, line, date, useScaleFactor: LeanData.OptionUseScaleFactor(config.Symbol), tradeBar, hasVolume: true);

            return tradeBar;
        }

        /// <summary>
        /// Parses Option trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseOption<T>(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };
            StreamParseScale(config, streamReader, date, useScaleFactor: LeanData.OptionUseScaleFactor(config.Symbol), tradeBar, true);

            return tradeBar;
        }

        /// <summary>
        /// Parses Future trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseFuture<T>(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };
            StreamParseNoScale(config, streamReader, date, tradeBar);

            return tradeBar;
        }

        /// <summary>
        /// Parses Future trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <typeparam name="T">The requested output type, must derive from TradeBar</typeparam>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static T ParseFuture<T>(SubscriptionDataConfig config, string line, DateTime date)
            where T : TradeBar, new()
        {
            var tradeBar = new T
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };
            LineParseNoScale(config, line, date, tradeBar);

            return tradeBar;
        }

        /// <summary>
        /// Parse an index bar from the LEAN disk format
        /// </summary>
        public static TradeBar ParseIndex(SubscriptionDataConfig config, string line, DateTime date)
        {
            return LineParseNoScale(config, line, date);
        }

        /// <summary>
        /// Parse an index bar from the LEAN disk format
        /// </summary>
        private static TradeBar LineParseNoScale(SubscriptionDataConfig config, string line, DateTime date, TradeBar bar = null, bool hasVolume = true)
        {
            var tradeBar = bar ?? new TradeBar
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            var csv = line.ToCsv(hasVolume ? 6 : 5);
            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                tradeBar.Time = DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                tradeBar.Time = date.Date.AddMilliseconds(csv[0].ToInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            tradeBar.Open = csv[1].ToDecimal();
            tradeBar.High = csv[2].ToDecimal();
            tradeBar.Low = csv[3].ToDecimal();
            tradeBar.Close = csv[4].ToDecimal();
            if (hasVolume)
            {
                tradeBar.Volume = csv[5].ToDecimal();
            }
            return tradeBar;
        }

        /// <summary>
        /// Parse an index bar from the LEAN disk format
        /// </summary>
        private static TradeBar StreamParseNoScale(SubscriptionDataConfig config, StreamReader streamReader, DateTime date, TradeBar bar = null, bool hasVolume = true)
        {
            var tradeBar = bar ?? new TradeBar
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            if (config.Resolution == Resolution.Daily || config.Resolution == Resolution.Hour)
            {
                // hourly and daily have different time format, and can use slow, robust c# parser.
                tradeBar.Time = streamReader.GetDateTime().ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            else
            {
                // Using custom "ToDecimal" conversion for speed on high resolution data.
                tradeBar.Time = date.Date.AddMilliseconds(streamReader.GetInt32()).ConvertTo(config.DataTimeZone, config.ExchangeTimeZone);
            }
            tradeBar.Open = streamReader.GetDecimal();
            tradeBar.High = streamReader.GetDecimal();
            tradeBar.Low = streamReader.GetDecimal();
            tradeBar.Close = streamReader.GetDecimal();
            if (hasVolume)
            {
                tradeBar.Volume = streamReader.GetDecimal();
            }
            return tradeBar;
        }

        private static TradeBar LineParseScale(SubscriptionDataConfig config, string line, DateTime date, bool useScaleFactor, TradeBar bar = null, bool hasVolume = true)
        {
            var tradeBar = bar ?? new TradeBar
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            LineParseNoScale(config, line, date, tradeBar, hasVolume);
            if (useScaleFactor)
            {
                tradeBar.Open *= _scaleFactor;
                tradeBar.High *= _scaleFactor;
                tradeBar.Low *= _scaleFactor;
                tradeBar.Close *= _scaleFactor;
            }

            return tradeBar;
        }

        private static TradeBar StreamParseScale(SubscriptionDataConfig config, StreamReader streamReader, DateTime date, bool useScaleFactor, TradeBar bar = null, bool hasVolume = true)
        {
            var tradeBar = bar ?? new TradeBar
            {
                Period = config.Increment,
                Symbol = config.Symbol
            };

            StreamParseNoScale(config, streamReader, date, tradeBar, hasVolume);
            if (useScaleFactor)
            {
                tradeBar.Open *= _scaleFactor;
                tradeBar.High *= _scaleFactor;
                tradeBar.Low *= _scaleFactor;
                tradeBar.Close *= _scaleFactor;
            }

            return tradeBar;
        }

        /// <summary>
        /// Parse an index bar from the LEAN disk format
        /// </summary>
        public static TradeBar ParseIndex(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            return StreamParseNoScale(config, streamReader, date);
        }

        /// <summary>
        /// Parses Option trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseOption(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseOption<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Parses Option trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseOption(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            return ParseOption<TradeBar>(config, streamReader, date);
        }

        /// <summary>
        /// Parses Future trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="line">Line from the data file requested</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseFuture(SubscriptionDataConfig config, string line, DateTime date)
        {
            return ParseFuture<TradeBar>(config, line, date);
        }

        /// <summary>
        /// Parses Future trade bar data into the specified tradebar type, useful for custom types with OHLCV data deriving from TradeBar
        /// </summary>
        /// <param name="config">Symbols, Resolution, DataType, </param>
        /// <param name="streamReader">The data stream of the requested file</param>
        /// <param name="date">The base data used to compute the time of the bar since the line specifies a milliseconds since midnight</param>
        /// <returns></returns>
        public static TradeBar ParseFuture(SubscriptionDataConfig config, StreamReader streamReader, DateTime date)
        {
            return ParseFuture<TradeBar>(config, streamReader, date);
        }

        /// <summary>
        /// Update the tradebar - build the bar from this pricing information:
        /// </summary>
        /// <param name="lastTrade">This trade price</param>
        /// <param name="bidPrice">Current bid price (not used) </param>
        /// <param name="askPrice">Current asking price (not used) </param>
        /// <param name="volume">Volume of this trade</param>
        /// <param name="bidSize">The size of the current bid, if available</param>
        /// <param name="askSize">The size of the current ask, if available</param>
        public override void Update(decimal lastTrade, decimal bidPrice, decimal askPrice, decimal volume, decimal bidSize, decimal askSize)
        {
            Initialize(lastTrade);
            if (lastTrade > High) High = lastTrade;
            if (lastTrade < Low) Low = lastTrade;
            //Volume is the total summed volume of trades in this bar:
            Volume += volume;
            //Always set the closing price;
            Close = lastTrade;
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
                // this data type is streamed in live mode
                return new SubscriptionDataSource(string.Empty, SubscriptionTransportMedium.Streaming);
            }

            var source = LeanData.GenerateZipFilePath(Globals.DataFolder, config.Symbol, date, config.Resolution, config.TickType);
            if (config.SecurityType == SecurityType.Future || config.SecurityType.IsOption())
            {
                source += "#" + LeanData.GenerateZipEntryName(config.Symbol, date, config.Resolution, config.TickType);
            }
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <param name="fillForward">True if this is a fill forward clone</param>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone(bool fillForward)
        {
            var clone = base.Clone(fillForward);

            if (fillForward)
            {
                // zero volume out, since it would skew calculations in volume-based indicators
                ((TradeBar)clone).Volume = 0;
            }

            return clone;
        }

        /// <summary>
        /// Return a new instance clone of this object
        /// </summary>
        public override BaseData Clone()
        {
            return (BaseData)MemberwiseClone();
        }

        /// <summary>
        /// Formats a string with the symbol and value.
        /// </summary>
        /// <returns>string - a string formatted as SPY: 167.753</returns>
        public override string ToString()
        {
            return $"{Symbol}: " +
                   $"O: {Open.SmartRounding()} " +
                   $"H: {High.SmartRounding()} " +
                   $"L: {Low.SmartRounding()} " +
                   $"C: {Close.SmartRounding()} " +
                   $"V: {Volume.SmartRounding()}";
        }

        /// <summary>
        /// Initializes this bar with a first data point
        /// </summary>
        /// <param name="value">The seed value for this bar</param>
        private void Initialize(decimal value)
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
            {
                _open = value;
                _low = value;
                _high = value;
            }
        }
    }
}
