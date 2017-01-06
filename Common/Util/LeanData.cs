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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for generating lean data file content
    /// </summary>
    public static class LeanData
    {
        /// <summary>
        /// Converts the specified base data instance into a lean data file csv line.
        /// This method takes into account the fake that base data instances typically
        /// are time stamped in the exchange time zone, but need to be written to disk
        /// in the data time zone.
        /// </summary>
        public static string GenerateLine(IBaseData data, Resolution resolution, DateTimeZone exchangeTimeZone, DateTimeZone dataTimeZone)
        {
            var clone = data.Clone();
            clone.Time = data.Time.ConvertTo(exchangeTimeZone, dataTimeZone);
            return GenerateLine(clone, clone.Symbol.ID.SecurityType, resolution);
        }

        /// <summary>
        /// Converts the specified base data instance into a lean data file csv line
        /// </summary>
        public static string GenerateLine(IBaseData data, SecurityType securityType, Resolution resolution)
        {
            var milliseconds = data.Time.TimeOfDay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            var longTime = data.Time.ToString(DateFormat.TwelveCharacter);

            switch (securityType)
            {
                case SecurityType.Equity:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick) data;
                            return ToCsv(milliseconds, Scale(tick.LastPrice), tick.Quantity, tick.Exchange, tick.SaleCondition, tick.Suspicious ? "1" : "0");

                        case Resolution.Minute:
                        case Resolution.Second:
                            var bar = (TradeBar) data;
                            return ToCsv(milliseconds, Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close), bar.Volume);

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = (TradeBar) data;
                            return ToCsv(longTime, Scale(bigBar.Open), Scale(bigBar.High), Scale(bigBar.Low), Scale(bigBar.Close), bigBar.Volume);
                    }
                    break;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick) data;
                            return ToCsv(milliseconds, tick.BidPrice, tick.AskPrice);

                        case Resolution.Second:
                        case Resolution.Minute:
                            var bar = (TradeBar) data;
                            return ToCsv(milliseconds, bar.Open, bar.High, bar.Low, bar.Close);

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = (TradeBar) data;
                            return ToCsv(longTime, bigBar.Open, bigBar.High, bigBar.Low, bigBar.Close);
                    }
                    break;

                case SecurityType.Option:
                case SecurityType.Future:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds,
                                    Scale(tick.LastPrice), tick.Quantity, tick.Exchange, tick.SaleCondition, tick.Suspicious ? "1": "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds,
                                    Scale(tick.BidPrice), tick.BidSize, Scale(tick.AskPrice), tick.AskSize, tick.Exchange, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.OpenInterest)
                            {
                                return ToCsv(milliseconds, tick.Value);
                            }
                            break;

                        case Resolution.Second:
                        case Resolution.Minute:
                            // option and future data can be quote or trade bars
                            var quoteBar = data as QuoteBar;
                            if (quoteBar != null)
                            {
                                return ToCsv(milliseconds,
                                    ToCsv(quoteBar.Bid), quoteBar.LastBidSize, 
                                    ToCsv(quoteBar.Ask), quoteBar.LastAskSize);
                            }
                            var tradeBar = data as TradeBar;
                            if (tradeBar != null)
                            {
                                return ToCsv(milliseconds,
                                    Scale(tradeBar.Open), Scale(tradeBar.High), Scale(tradeBar.Low), Scale(tradeBar.Close), tradeBar.Volume);
                            }
                            var openInterest = data as OpenInterest;
                            if (openInterest != null)
                            {
                                return ToCsv(milliseconds, openInterest.Value);
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            // option and future data can be quote or trade bars
                            var bigQuoteBar = data as QuoteBar;
                            if (bigQuoteBar != null)
                            {
                                return ToCsv(longTime,
                                    ToCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime,
                                    ToCsv(bigTradeBar), bigTradeBar.Volume);
                            }
                            var bigOpenInterest = data as OpenInterest;
                            if (bigOpenInterest != null)
                            {
                                return ToCsv(milliseconds, bigOpenInterest.Value);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("resolution", resolution, null);
                    }
                    break;
            }

            throw new NotImplementedException("LeanData.GenerateLine has not yet been implemented for security type: " + securityType + " at resolution: " + resolution);
        }

        /// <summary>
        /// Gets the data type required for the specified combination of resolution and tick type
        /// </summary>
        /// <param name="resolution">The resolution, if Tick, the Type returned is always Tick</param>
        /// <param name="tickType">The <see cref="TickType"/> that primarily dictates the type returned</param>
        /// <returns>The Type used to create a subscription</returns>
        public static Type GetDataType(Resolution resolution, TickType tickType)
        {
            if (resolution == Resolution.Tick) return typeof(Tick);
            if (tickType == TickType.OpenInterest) return typeof(OpenInterest);
            if (tickType == TickType.Quote) return typeof(QuoteBar);
            return typeof(TradeBar);
        }

        /// <summary>
        /// Generates the full zip file path rooted in the <paramref name="dataDirectory"/>
        /// </summary>
        public static string GenerateZipFilePath(string dataDirectory, Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            return Path.Combine(dataDirectory, GenerateRelativeZipFilePath(symbol, date, resolution, tickType));
        }

        /// <summary>
        /// Generates the full zip file path rooted in the <paramref name="dataDirectory"/>
        /// </summary>
        public static string GenerateZipFilePath(string dataDirectory, string symbol, SecurityType securityType, string market, DateTime date, Resolution resolution)
        {
            return Path.Combine(dataDirectory, GenerateRelativeZipFilePath(symbol, securityType, market, date, resolution));
        }

        /// <summary>
        /// Generates the relative zip directory for the specified symbol/resolution
        /// </summary>
        public static string GenerateRelativeZipFileDirectory(Symbol symbol, Resolution resolution)
        {
            var isHourOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;
            var securityType = symbol.ID.SecurityType.ToLower();
            var market = symbol.ID.Market.ToLower();
            var res = resolution.ToLower();
            var directory = Path.Combine(securityType, market, res);
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return !isHourOrDaily ? Path.Combine(directory, symbol.Value.ToLower()) : directory;

                case SecurityType.Future:
                case SecurityType.Option:
                    // options uses the underlying symbol for pathing
                    return !isHourOrDaily ? Path.Combine(directory, symbol.Underlying.Value.ToLower()) : directory;

                case SecurityType.Commodity:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Generates relative factor file paths for equities
        /// </summary>
        public static string GenerateRelativeFactorFilePath(Symbol symbol)
        {
            return Path.Combine(Globals.DataFolder,
                                        "equity",
                                        symbol.ID.Market,
                                        "factor_files",
                                        symbol.Value.ToLower() + ".csv");
        }
        /// <summary>
        /// Generates the relative zip file path rooted in the /Data directory
        /// </summary>
        public static string GenerateRelativeZipFilePath(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            return Path.Combine(GenerateRelativeZipFileDirectory(symbol, resolution), GenerateZipFileName(symbol, date, resolution, tickType));
        }

        /// <summary>
        /// Generates the relative zip file path rooted in the /Data directory
        /// </summary>
        public static string GenerateRelativeZipFilePath(string symbol, SecurityType securityType, string market, DateTime date, Resolution resolution)
        {
            var directory = Path.Combine(securityType.ToLower(), market.ToLower(), resolution.ToLower());
            if (resolution != Resolution.Daily && resolution != Resolution.Hour)
            {
                directory = Path.Combine(directory, symbol.ToLower());
            }

            return Path.Combine(directory, GenerateZipFileName(symbol, securityType, date, resolution));
        }

        /// <summary>
        /// Generate's the zip entry name to hold the specified data.
        /// </summary>
        public static string GenerateZipEntryName(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            var formattedDate = date.ToString(DateFormat.EightCharacter);
            var isHourOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    if (isHourOrDaily)
                    {
                        return string.Format("{0}.csv", 
                            symbol.Value.ToLower()
                            );
                    }

                    return string.Format("{0}_{1}_{2}_{3}.csv", 
                        formattedDate, 
                        symbol.Value.ToLower(), 
                        resolution.ToLower(), 
                        tickType.ToLower()
                        );

                case SecurityType.Option:
                    if (isHourOrDaily)
                    {
                        return string.Join("_",
                            symbol.Underlying.Value.ToLower(), // underlying
                            tickType.ToLower(),
                            symbol.ID.OptionStyle.ToLower(),
                            symbol.ID.OptionRight.ToLower(),
                            Scale(symbol.ID.StrikePrice),
                            symbol.ID.Date.ToString(DateFormat.EightCharacter)
                            ) + ".csv";
                    }

                    return string.Join("_",
                        formattedDate,
                        symbol.Underlying.Value.ToLower(), // underlying
                        resolution.ToLower(),
                        tickType.ToLower(),
                        symbol.ID.OptionStyle.ToLower(),
                        symbol.ID.OptionRight.ToLower(),
                        Scale(symbol.ID.StrikePrice),
                        symbol.ID.Date.ToString(DateFormat.EightCharacter)
                        ) + ".csv";

                case SecurityType.Future:
                    if (isHourOrDaily)
                    {
                        return string.Join("_",
                            symbol.Underlying.Value.ToLower(), // underlying
                            tickType.ToLower(),
                            symbol.ID.Date.ToString(DateFormat.YearMonth)
                            ) + ".csv";
                    }

                    return string.Join("_",
                        formattedDate,
                        symbol.Underlying.Value.ToLower(), // underlying
                        resolution.ToLower(),
                        tickType.ToLower(),
                        symbol.ID.Date.ToString(DateFormat.YearMonth)
                        ) + ".csv";

                case SecurityType.Commodity:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Creates the entry name for a QC zip data file
        /// </summary>
        public static string GenerateZipEntryName(string symbol, SecurityType securityType, DateTime date, Resolution resolution, TickType dataType = TickType.Trade)
        {
            if (securityType != SecurityType.Base && securityType != SecurityType.Equity && securityType != SecurityType.Forex && securityType != SecurityType.Cfd)
            {
                throw new NotImplementedException("This method only implements base, equity, forex and cfd security type.");
            }

            symbol = symbol.ToLower();

            if (resolution == Resolution.Hour || resolution == Resolution.Daily)
            {
                return symbol + ".csv";
            }

            //All fx is quote data.
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
            {
                dataType = TickType.Quote;
            }

            return string.Format("{0}_{1}_{2}_{3}.csv", date.ToString(DateFormat.EightCharacter), symbol, resolution.ToLower(), dataType.ToLower());
        }

        /// <summary>
        /// Generates the zip file name for the specified date of data.
        /// </summary>
        public static string GenerateZipFileName(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            var tickTypeString = tickType.ToLower();
            var formattedDate = date.ToString(DateFormat.EightCharacter);
            var isHourOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    if (isHourOrDaily)
                    {
                        return string.Format("{0}.zip", 
                            symbol.Value.ToLower()
                            );
                    }

                    return string.Format("{0}_{1}.zip", 
                        formattedDate, 
                        tickTypeString
                        );

                case SecurityType.Option:
                    if (isHourOrDaily)
                    {
                        return string.Format("{0}_{1}_{2}.zip", 
                            symbol.Underlying.Value.ToLower(), // underlying
                            tickTypeString,
                            symbol.ID.OptionStyle.ToLower()
                            );
                    }

                    return string.Format("{0}_{1}_{2}.zip", 
                        formattedDate, 
                        tickTypeString,
                        symbol.ID.OptionStyle.ToLower()
                        );

                case SecurityType.Future:
                    if (isHourOrDaily)
                    {
                        return string.Format("{0}_{1}.zip",
                            symbol.Underlying.Value.ToLower(), // underlying
                            tickTypeString);
                    }

                    return string.Format("{0}_{1}.zip",
                        formattedDate,
                        tickTypeString);

                case SecurityType.Commodity:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Creates the zip file name for a QC zip data file
        /// </summary>
        public static string GenerateZipFileName(string symbol, SecurityType securityType, DateTime date, Resolution resolution, TickType? tickType = null)
        {
            if (resolution == Resolution.Hour || resolution == Resolution.Daily)
            {
                return symbol.ToLower() + ".zip";
            }

            var zipFileName = date.ToString(DateFormat.EightCharacter);
            tickType = tickType ?? (securityType == SecurityType.Forex || securityType == SecurityType.Cfd ? TickType.Quote : TickType.Trade);
            var suffix = string.Format("_{0}.zip", tickType.Value.ToLower());
            return zipFileName + suffix;
        }

        /// <summary>
        /// Gets the tick type most commonly associated with the specified security type
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>The most common tick type for the specified security type</returns>
        public static TickType GetCommonTickType(SecurityType securityType)
        {
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
            {
                return TickType.Quote;
            }
            return TickType.Trade;
        }

        /// <summary>
        /// Creates a symbol from the specified zip entry name
        /// </summary>
        /// <param name="symbol">The root symbol of the output symbol</param>
        /// <param name="resolution">The resolution of the data source producing the zip entry name</param>
        /// <param name="zipEntryName">The zip entry name to be parsed</param>
        /// <returns>A new symbol representing the zip entry name</returns>
        public static Symbol ReadSymbolFromZipEntry(Symbol symbol, Resolution resolution, string zipEntryName)
        {
            var isHourlyOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;
            var parts = zipEntryName.Replace(".csv", string.Empty).Split('_');
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Option:
                    if (isHourlyOrDaily)
                    {
                        var style = (OptionStyle)Enum.Parse(typeof(OptionStyle), parts[2], true);
                        var right = (OptionRight)Enum.Parse(typeof(OptionRight), parts[3], true);
                        var strike = decimal.Parse(parts[4]) / 10000m;
                        var expiry = DateTime.ParseExact(parts[5], DateFormat.EightCharacter, null);
                        return Symbol.CreateOption(symbol.Underlying, Market.USA, style, right, strike, expiry);
                    }
                    else
                    {
                        var style = (OptionStyle)Enum.Parse(typeof(OptionStyle), parts[4], true);
                        var right = (OptionRight)Enum.Parse(typeof(OptionRight), parts[5], true);
                        var strike = decimal.Parse(parts[6]) / 10000m;
                        var expiry = DateTime.ParseExact(parts[7], DateFormat.EightCharacter, null);
                        return Symbol.CreateOption(symbol.Underlying, Market.USA, style, right, strike, expiry);
                    }
                    break;

                case SecurityType.Future:
                    var expiryYearMonth = DateTime.ParseExact(parts[4], DateFormat.YearMonth, null);
                    expiryYearMonth = new DateTime(expiryYearMonth.Year, expiryYearMonth.Month, DateTime.DaysInMonth(expiryYearMonth.Year, expiryYearMonth.Month));
                    return Symbol.CreateFuture(parts[1], Market.USA, expiryYearMonth);

                default:
                    throw new NotImplementedException("ReadSymbolFromZipEntry is not implemented for " + symbol.ID.SecurityType + " " + resolution);
            }
        }

        /// <summary>
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static long Scale(decimal value)
        {
            return (long)(value*10000);
        }

        /// <summary>
        /// Create a csv line from the specified arguments
        /// </summary>
        private static string ToCsv(params object[] args)
        {
            // use culture neutral formatting for decimals
            for (var i = 0; i < args.Length; i++)
            {
                var value = args[i];
                if (value is decimal)
                {
                    args[i] = ((decimal) value).ToString(CultureInfo.InvariantCulture);
                }
            }

            return string.Join(",", args);
        }

        /// <summary>
        /// Creates a csv line for the bar, if null fills in empty strings
        /// </summary>
        private static string ToCsv(IBar bar)
        {
            if (bar == null)
            {
                return ToCsv(string.Empty, string.Empty, string.Empty, string.Empty);
            }
            return ToCsv(Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close));
        }
    }
}
