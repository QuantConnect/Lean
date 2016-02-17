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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for generating lean data file content
    /// </summary>
    public static class LeanData
    {
        /// <summary>
        /// Converts the specified base data instance into a lean data file csv line
        /// </summary>
        public static string GenerateLine(IBaseData data, SecurityType securityType, Resolution resolution)
        {
            var line = string.Empty;
            var format = "{0},{1},{2},{3},{4},{5}";
            var milliseconds = data.Time.TimeOfDay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            var longTime = data.Time.ToString(DateFormat.TwelveCharacter);

            switch (securityType)
            {
                case SecurityType.Equity:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = data as Tick;
                            if (tick != null)
                            {
                                line = string.Format(format, milliseconds, Scale(tick.LastPrice), tick.Quantity, tick.Exchange, tick.SaleCondition, tick.Suspicious);
                            }
                            break;

                        case Resolution.Minute:
                        case Resolution.Second:
                            var bar = data as TradeBar;
                            if (bar != null)
                            {
                                line = string.Format(format, milliseconds, Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close), bar.Volume);
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = data as TradeBar;
                            if (bigBar != null)
                            {
                                line = string.Format(format, longTime, Scale(bigBar.Open), Scale(bigBar.High), Scale(bigBar.Low), Scale(bigBar.Close), bigBar.Volume);
                            }
                            break;
                    }
                    break;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var fxTick = data as Tick;
                            if (fxTick != null)
                            {
                                line = string.Format("{0},{1},{2}", milliseconds, fxTick.BidPrice, fxTick.AskPrice);
                            }
                            break;

                        case Resolution.Second:
                        case Resolution.Minute:
                            var fxBar = data as TradeBar;
                            if (fxBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", milliseconds, fxBar.Open, fxBar.High, fxBar.Low, fxBar.Close);
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var dailyBar = data as TradeBar;
                            if (dailyBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", longTime, dailyBar.Open, dailyBar.High, dailyBar.Low, dailyBar.Close);
                            }
                            break;
                    }
                    break;
            }

            return line;
        }

        /// <summary>
        /// Generates the full zip file path rooted in the <paramref name="dataDirectory"/>
        /// </summary>
        public static string GenerateZipFilePath(string dataDirectory, Symbol symbol, DateTime date, Resolution resolution)
        {
            return GenerateZipFilePath(dataDirectory, symbol.Value, symbol.ID.SecurityType, symbol.ID.Market, date, resolution);
        }

        /// <summary>
        /// Generates the full zip file path rooted in the <paramref name="dataDirectory"/>
        /// </summary>
        public static string GenerateZipFilePath(string dataDirectory, string symbol, SecurityType securityType, string market, DateTime date, Resolution resolution)
        {
            return Path.Combine(dataDirectory, GenerateRelativeZipFilePath(symbol, securityType, market, date, resolution));
        }

        /// <summary>
        /// Generates the relative zip file path rooted in the /Data directory
        /// </summary>
        public static string GenerateRelativeZipFilePath(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
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
                    directory = !isHourOrDaily ? Path.Combine(directory, symbol.Value.ToLower()) : directory;
                    break;

                case SecurityType.Option:
                    // options uses the underlying symbol for pathing
                    directory = !isHourOrDaily ? Path.Combine(directory, symbol.ID.Symbol.ToLower()) : directory;
                    break;

                case SecurityType.Commodity:
                case SecurityType.Future:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Path.Combine(directory, GenerateZipFileName(symbol, date, resolution, tickType));
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
                        return string.Format("{0}_{1}_{2}_{3}_{4:yyyyMMdd}_{5}.csv", 
                            symbol.ID.Symbol.ToLower(), 
                            symbol.ID.OptionStyle.ToLower(), 
                            symbol.ID.OptionRight.ToLower(), 
                            symbol.ID.StrikePrice*10000, // in deci-cents
                            symbol.ID.Date, 
                            tickType.ToLower()
                            );
                    }

                    return string.Format("{0}_{1}_{2}_{3}_{4}_{5:yyyyMMdd}_{6}_{7}.csv", 
                        formattedDate, 
                        symbol.ID.Symbol.ToLower(), 
                        symbol.ID.OptionStyle.ToLower(), 
                        symbol.ID.OptionRight.ToLower(), 
                        symbol.ID.StrikePrice*10000, // in deci-cents
                        symbol.ID.Date, 
                        resolution.ToLower(), 
                        tickType.ToLower()
                        );

                case SecurityType.Commodity:
                case SecurityType.Future:
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
                        return string.Format("{0}_{1}.zip", 
                            symbol.ID.Symbol.ToLower(), // underlying
                            tickTypeString
                            );
                    }

                    return string.Format("{0}_{1}.zip", 
                        formattedDate, 
                        tickTypeString
                        );

                case SecurityType.Commodity:
                case SecurityType.Future:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Creates the zip file name for a QC zip data file
        /// </summary>
        public static string GenerateZipFileName(string symbol, SecurityType securityType, DateTime date, Resolution resolution)
        {
            if (resolution == Resolution.Hour || resolution == Resolution.Daily)
            {
                return symbol.ToLower() + ".zip";
            }

            var zipFileName = date.ToString(DateFormat.EightCharacter);
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
            {
                return zipFileName + "_quote.zip";
            }
            return zipFileName + "_trade.zip";
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
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static int Scale(decimal value)
        {
            return Convert.ToInt32(value*10000);
        }
    }
}
