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
            var directory = Path.Combine(dataDirectory, securityType.ToString().ToLower(), market.ToLower(), resolution.ToString().ToLower());
            if (resolution != Resolution.Daily && resolution != Resolution.Hour)
            {
                directory = Path.Combine(directory, symbol.ToLower());
            }

            return Path.Combine(directory, GenerateZipFileName(symbol, securityType, date, resolution));
        }

        /// <summary>
        /// Creates the entry name for a QC zip data file
        /// </summary>
        public static string GenerateZipEntryName(string symbol, SecurityType securityType, DateTime date, Resolution resolution, TickType dataType = TickType.Trade)
        {
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

            return string.Format("{0}_{1}_{2}_{3}.csv", date.ToString(DateFormat.EightCharacter), symbol, resolution.ToString().ToLower(), dataType.ToString().ToLower());
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
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static int Scale(decimal value)
        {
            return Convert.ToInt32(value * 10000);
        }
    }
}
