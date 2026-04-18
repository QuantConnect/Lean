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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for generating lean data file content
    /// </summary>
    public static class LeanData
    {
        private static readonly HashSet<Type> _strictDailyEndTimesDataTypes = new()
        {
            // the underlying could yield auxiliary data which we don't want to change
            typeof(TradeBar), typeof(QuoteBar), typeof(BaseDataCollection), typeof(OpenInterest)
        };

        /// <summary>
        /// The different <see cref="SecurityType"/> used for data paths
        /// </summary>
        /// <remarks>This includes 'alternative'</remarks>
        public static HashSet<string> SecurityTypeAsDataPath => Enum.GetNames(typeof(SecurityType))
            .Select(x => x.ToLowerInvariant()).Union(new[] { "alternative" }).ToHashSet();

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
        /// Helper method that will parse a given data line in search of an associated date time
        /// </summary>
        public static DateTime ParseTime(string line, DateTime date, Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Tick:
                case Resolution.Second:
                case Resolution.Minute:
                    var index = line.IndexOf(',', StringComparison.InvariantCulture);
                    return date.AddTicks(Convert.ToInt64(10000 * decimal.Parse(line.AsSpan(0, index))));
                case Resolution.Hour:
                case Resolution.Daily:
                    return DateTime.ParseExact(line.AsSpan(0, DateFormat.TwelveCharacter.Length), DateFormat.TwelveCharacter, CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }
        }

        /// <summary>
        /// Converts the specified base data instance into a lean data file csv line
        /// </summary>
        public static string GenerateLine(IBaseData data, SecurityType securityType, Resolution resolution)
        {
            var milliseconds = data.Time.TimeOfDay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            var longTime = data.Time.ToStringInvariant(DateFormat.TwelveCharacter);

            switch (securityType)
            {
                case SecurityType.Equity:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds, Scale(tick.LastPrice), tick.Quantity, tick.ExchangeCode, tick.SaleCondition, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds, Scale(tick.BidPrice), tick.BidSize, Scale(tick.AskPrice), tick.AskSize, tick.ExchangeCode, tick.SaleCondition, tick.Suspicious ? "1" : "0");
                            }
                            break;
                        case Resolution.Minute:
                        case Resolution.Second:
                            var tradeBar = data as TradeBar;
                            if (tradeBar != null)
                            {
                                return ToCsv(milliseconds, Scale(tradeBar.Open), Scale(tradeBar.High), Scale(tradeBar.Low), Scale(tradeBar.Close), tradeBar.Volume);
                            }
                            var quoteBar = data as QuoteBar;
                            if (quoteBar != null)
                            {
                                return ToCsv(milliseconds,
                                    ToScaledCsv(quoteBar.Bid), quoteBar.LastBidSize,
                                    ToScaledCsv(quoteBar.Ask), quoteBar.LastAskSize);
                            }
                            break;

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime, Scale(bigTradeBar.Open), Scale(bigTradeBar.High), Scale(bigTradeBar.Low), Scale(bigTradeBar.Close), bigTradeBar.Volume);
                            }
                            var bigQuoteBar = data as QuoteBar;
                            if (bigQuoteBar != null)
                            {
                                return ToCsv(longTime,
                                    ToScaledCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToScaledCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            break;
                    }
                    break;

                case SecurityType.Crypto:
                case SecurityType.CryptoFuture:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = data as Tick;
                            if (tick == null)
                            {
                                throw new ArgumentException($"{securityType} tick could not be created", nameof(data));
                            }
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds, tick.LastPrice, tick.Quantity, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds, tick.BidPrice, tick.BidSize, tick.AskPrice, tick.AskSize, tick.Suspicious ? "1" : "0");
                            }
                            throw new ArgumentException($"{securityType} tick could not be created");
                        case Resolution.Second:
                        case Resolution.Minute:
                            var quoteBar = data as QuoteBar;
                            if (quoteBar != null)
                            {
                                return ToCsv(milliseconds,
                                    ToNonScaledCsv(quoteBar.Bid), quoteBar.LastBidSize,
                                    ToNonScaledCsv(quoteBar.Ask), quoteBar.LastAskSize);
                            }
                            var tradeBar = data as TradeBar;
                            if (tradeBar != null)
                            {
                                return ToCsv(milliseconds, tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume);
                            }
                            throw new ArgumentException($"{securityType} minute/second bar could not be created", nameof(data));

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigQuoteBar = data as QuoteBar;
                            if (bigQuoteBar != null)
                            {
                                return ToCsv(longTime,
                                    ToNonScaledCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToNonScaledCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime,
                                             bigTradeBar.Open,
                                             bigTradeBar.High,
                                             bigTradeBar.Low,
                                             bigTradeBar.Close,
                                             bigTradeBar.Volume);
                            }
                            throw new ArgumentException($"{securityType} hour/daily bar could not be created", nameof(data));
                    }
                    break;
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = data as Tick;
                            if (tick == null)
                            {
                                throw new ArgumentException("Expected data of type 'Tick'", nameof(data));
                            }
                            return ToCsv(milliseconds, tick.BidPrice, tick.AskPrice);

                        case Resolution.Second:
                        case Resolution.Minute:
                            var bar = data as QuoteBar;
                            if (bar == null)
                            {
                                throw new ArgumentException("Expected data of type 'QuoteBar'", nameof(data));
                            }
                            return ToCsv(milliseconds,
                                ToNonScaledCsv(bar.Bid), bar.LastBidSize,
                                ToNonScaledCsv(bar.Ask), bar.LastAskSize);

                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = data as QuoteBar;
                            if (bigBar == null)
                            {
                                throw new ArgumentException("Expected data of type 'QuoteBar'", nameof(data));
                            }
                            return ToCsv(longTime,
                                ToNonScaledCsv(bigBar.Bid), bigBar.LastBidSize,
                                ToNonScaledCsv(bigBar.Ask), bigBar.LastAskSize);
                    }
                    break;

                case SecurityType.Index:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            return ToCsv(milliseconds, tick.LastPrice, tick.Quantity, string.Empty, string.Empty, "0");
                        case Resolution.Second:
                        case Resolution.Minute:
                            var bar = data as TradeBar;
                            if (bar == null)
                            {
                                throw new ArgumentException("Expected data of type 'TradeBar'", nameof(data));
                            }
                            return ToCsv(milliseconds, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigTradeBar = data as TradeBar;
                            return ToCsv(longTime, bigTradeBar.Open, bigTradeBar.High, bigTradeBar.Low, bigTradeBar.Close, bigTradeBar.Volume);
                    }
                    break;

                case SecurityType.Option:
                case SecurityType.IndexOption:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds,
                                    Scale(tick.LastPrice), tick.Quantity, tick.ExchangeCode, tick.SaleCondition, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds,
                                    Scale(tick.BidPrice), tick.BidSize, Scale(tick.AskPrice), tick.AskSize, tick.ExchangeCode, tick.Suspicious ? "1" : "0");
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
                                    ToScaledCsv(quoteBar.Bid), quoteBar.LastBidSize,
                                    ToScaledCsv(quoteBar.Ask), quoteBar.LastAskSize);
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
                                    ToScaledCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToScaledCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime, ToScaledCsv(bigTradeBar), bigTradeBar.Volume);
                            }
                            var bigOpenInterest = data as OpenInterest;
                            if (bigOpenInterest != null)
                            {
                                return ToCsv(longTime, bigOpenInterest.Value);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
                    }
                    break;

                case SecurityType.FutureOption:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds,
                                    tick.LastPrice, tick.Quantity, tick.ExchangeCode, tick.SaleCondition, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds,
                                    tick.BidPrice, tick.BidSize, tick.AskPrice, tick.AskSize, tick.ExchangeCode, tick.Suspicious ? "1" : "0");
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
                                    ToNonScaledCsv(quoteBar.Bid), quoteBar.LastBidSize,
                                    ToNonScaledCsv(quoteBar.Ask), quoteBar.LastAskSize);
                            }
                            var tradeBar = data as TradeBar;
                            if (tradeBar != null)
                            {
                                return ToCsv(milliseconds,
                                    tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume);
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
                                    ToNonScaledCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToNonScaledCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime, ToNonScaledCsv(bigTradeBar), bigTradeBar.Volume);
                            }
                            var bigOpenInterest = data as OpenInterest;
                            if (bigOpenInterest != null)
                            {
                                return ToCsv(longTime, bigOpenInterest.Value);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
                    }
                    break;

                case SecurityType.Future:
                    switch (resolution)
                    {
                        case Resolution.Tick:
                            var tick = (Tick)data;
                            if (tick.TickType == TickType.Trade)
                            {
                                return ToCsv(milliseconds,
                                             tick.LastPrice, tick.Quantity, tick.ExchangeCode, tick.SaleCondition, tick.Suspicious ? "1" : "0");
                            }
                            if (tick.TickType == TickType.Quote)
                            {
                                return ToCsv(milliseconds,
                                             tick.BidPrice, tick.BidSize, tick.AskPrice, tick.AskSize, tick.ExchangeCode, tick.Suspicious ? "1" : "0");
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
                                    ToNonScaledCsv(quoteBar.Bid), quoteBar.LastBidSize,
                                    ToNonScaledCsv(quoteBar.Ask), quoteBar.LastAskSize);
                            }
                            var tradeBar = data as TradeBar;
                            if (tradeBar != null)
                            {
                                return ToCsv(milliseconds,
                                             tradeBar.Open, tradeBar.High, tradeBar.Low, tradeBar.Close, tradeBar.Volume);
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
                                    ToNonScaledCsv(bigQuoteBar.Bid), bigQuoteBar.LastBidSize,
                                    ToNonScaledCsv(bigQuoteBar.Ask), bigQuoteBar.LastAskSize);
                            }
                            var bigTradeBar = data as TradeBar;
                            if (bigTradeBar != null)
                            {
                                return ToCsv(longTime, ToNonScaledCsv(bigTradeBar), bigTradeBar.Volume);
                            }
                            var bigOpenInterest = data as OpenInterest;
                            if (bigOpenInterest != null)
                            {
                                return ToCsv(longTime, bigOpenInterest.Value);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(securityType), securityType, null);
            }

            throw new NotImplementedException(Invariant(
                $"LeanData.GenerateLine has not yet been implemented for security type: {securityType} at resolution: {resolution}"
            ));
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
        /// Determines if the Type is a 'common' type used throughout lean
        /// This method is helpful in creating <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <param name="baseDataType">The Type to check</param>
        /// <returns>A bool indicating whether the type is of type <see cref="TradeBar"/>
        ///  <see cref="QuoteBar"/> or <see cref="OpenInterest"/></returns>
        public static bool IsCommonLeanDataType(Type baseDataType)
        {
            if (baseDataType == typeof(Tick) ||
                baseDataType == typeof(TradeBar) ||
                baseDataType == typeof(QuoteBar) ||
                baseDataType == typeof(OpenInterest))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper method to determine if a configuration set is valid
        /// </summary>
        public static bool IsValidConfiguration(SecurityType securityType, Resolution resolution, TickType tickType)
        {
            if (securityType == SecurityType.Equity && (resolution == Resolution.Daily || resolution == Resolution.Hour))
            {
                return tickType != TickType.Quote;
            }
            return true;
        }

        /// <summary>
        /// Generates the full zip file path rooted in the <paramref name="dataDirectory"/>
        /// </summary>
        public static string GenerateZipFilePath(string dataDirectory, Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            // we could call 'GenerateRelativeZipFilePath' but we don't to avoid an extra string & path combine we are doing to drop right away
            return Path.Combine(dataDirectory, GenerateRelativeZipFileDirectory(symbol, resolution), GenerateZipFileName(symbol, date, resolution, tickType));
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
            var securityType = symbol.SecurityType.SecurityTypeToLower();

            var market = symbol.ID.Market.ToLowerInvariant();
            var res = resolution.ResolutionToLower();
            var directory = Path.Combine(securityType, market, res);
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Index:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                    return !isHourOrDaily ? Path.Combine(directory, symbol.Value.ToLowerInvariant()) : directory;

                case SecurityType.IndexOption:
                    // For index options, we use the canonical option ticker since it can differ from the underlying's ticker.
                    return !isHourOrDaily ? Path.Combine(directory, symbol.ID.Symbol.ToLowerInvariant()) : directory;

                case SecurityType.Option:
                    // options uses the underlying symbol for pathing.
                    return !isHourOrDaily ? Path.Combine(directory, symbol.Underlying.Value.ToLowerInvariant()) : directory;

                case SecurityType.FutureOption:
                    // For futures options, we use the canonical option ticker plus the underlying's expiry
                    // since it can differ from the underlying's ticker. We differ from normal futures
                    // because the option chain can be extraordinarily large compared to equity option chains.
                    var futureOptionPath = Path.Combine(symbol.ID.Symbol, symbol.Underlying.ID.Date.ToStringInvariant(DateFormat.EightCharacter))
                        .ToLowerInvariant();

                    return Path.Combine(directory, futureOptionPath);

                case SecurityType.Future:
                case SecurityType.CryptoFuture:
                    return !isHourOrDaily ? Path.Combine(directory, symbol.ID.Symbol.ToLowerInvariant()) : directory;

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
                                        symbol.Value.ToLowerInvariant() + ".csv");
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
            var directory = Path.Combine(securityType.SecurityTypeToLower(), market.ToLowerInvariant(), resolution.ResolutionToLower());
            if (resolution != Resolution.Daily && resolution != Resolution.Hour)
            {
                directory = Path.Combine(directory, symbol.ToLowerInvariant());
            }

            return Path.Combine(directory, GenerateZipFileName(symbol, securityType, date, resolution));
        }

        /// <summary>
        /// Generates the relative directory to the universe files for the specified symbol
        /// </summary>
        public static string GenerateRelativeUniversesDirectory(Symbol symbol)
        {
            var path = Path.Combine(symbol.SecurityType.SecurityTypeToLower(), symbol.ID.Market, "universes");
            switch (symbol.SecurityType)
            {
                case SecurityType.Option:
                    path = Path.Combine(path, symbol.Underlying.Value.ToLowerInvariant());
                    break;

                case SecurityType.IndexOption:
                    path = Path.Combine(path, symbol.ID.Symbol.ToLowerInvariant());
                    break;

                case SecurityType.FutureOption:
                    path = Path.Combine(path,
                        symbol.Underlying.ID.Symbol.ToLowerInvariant(),
                        symbol.Underlying.ID.Date.ToStringInvariant(DateFormat.EightCharacter));
                    break;

                case SecurityType.Future:
                    path = Path.Combine(path, symbol.ID.Symbol.ToLowerInvariant());
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unsupported security type {symbol.SecurityType}");
            }

            return path;
        }

        /// <summary>
        /// Generates the directory to the universe files for the specified symbol
        /// </summary>
        public static string GenerateUniversesDirectory(string dataDirectory, Symbol symbol)
        {
            return Path.Combine(dataDirectory, GenerateRelativeUniversesDirectory(symbol));
        }

        /// <summary>
        /// Generate's the zip entry name to hold the specified data.
        /// </summary>
        public static string GenerateZipEntryName(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            var formattedDate = date.ToStringInvariant(DateFormat.EightCharacter);
            var isHourOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Index:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                case SecurityType.Crypto:
                    if (resolution == Resolution.Tick && symbol.SecurityType == SecurityType.Equity)
                    {
                        return Invariant($"{formattedDate}_{symbol.Value.ToLowerInvariant()}_{tickType}_{resolution}.csv");
                    }

                    if (isHourOrDaily)
                    {
                        return $"{symbol.Value.ToLowerInvariant()}.csv";
                    }

                    return Invariant($"{formattedDate}_{symbol.Value.ToLowerInvariant()}_{resolution.ResolutionToLower()}_{tickType.TickTypeToLower()}.csv");

                case SecurityType.Option:
                    var optionPath = symbol.Underlying.Value.ToLowerInvariant();

                    if (isHourOrDaily)
                    {
                        return string.Join("_",
                            optionPath,
                            tickType.TickTypeToLower(),
                            symbol.ID.OptionStyle.OptionStyleToLower(),
                            symbol.ID.OptionRight.OptionRightToLower(),
                            Scale(symbol.ID.StrikePrice),
                            symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter)
                            ) + ".csv";
                    }

                    return string.Join("_",
                        formattedDate,
                        optionPath,
                        resolution.ResolutionToLower(),
                        tickType.TickTypeToLower(),
                        symbol.ID.OptionStyle.OptionStyleToLower(),
                        symbol.ID.OptionRight.OptionRightToLower(),
                        Scale(symbol.ID.StrikePrice),
                        symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter)
                        ) + ".csv";

                case SecurityType.IndexOption:
                case SecurityType.FutureOption:
                    // We want the future/index option ticker as the lookup name inside the ZIP file
                    var optionTickerBasedPath = symbol.ID.Symbol.ToLowerInvariant();

                    if (isHourOrDaily)
                    {
                        return string.Join("_",
                            optionTickerBasedPath,
                            tickType.TickTypeToLower(),
                            symbol.ID.OptionStyle.OptionStyleToLower(),
                            symbol.ID.OptionRight.OptionRightToLower(),
                            Scale(symbol.ID.StrikePrice),
                            symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter)
                            ) + ".csv";
                    }

                    return string.Join("_",
                        formattedDate,
                        optionTickerBasedPath,
                        resolution.ResolutionToLower(),
                        tickType.TickTypeToLower(),
                        symbol.ID.OptionStyle.OptionStyleToLower(),
                        symbol.ID.OptionRight.OptionRightToLower(),
                        Scale(symbol.ID.StrikePrice),
                        symbol.ID.Date.ToStringInvariant(DateFormat.EightCharacter)
                        ) + ".csv";

                case SecurityType.Future:
                case SecurityType.CryptoFuture:
                    if (symbol.HasUnderlying)
                    {
                        symbol = symbol.Underlying;
                    }

                    string expirationTag;
                    var expiryDate = symbol.ID.Date;
                    if (expiryDate != SecurityIdentifier.DefaultDate)
                    {
                        var monthsToAdd = FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(symbol.ID.Symbol, expiryDate.Date);
                        var contractYearMonth = expiryDate.AddMonths(monthsToAdd).ToStringInvariant(DateFormat.YearMonth);

                        expirationTag = $"{contractYearMonth}_{expiryDate.ToStringInvariant(DateFormat.EightCharacter)}";
                    }
                    else
                    {
                        expirationTag = "perp";
                    }

                    if (isHourOrDaily)
                    {
                        return string.Join("_",
                            symbol.ID.Symbol.ToLowerInvariant(),
                            tickType.TickTypeToLower(),
                            expirationTag
                            ) + ".csv";
                    }

                    return string.Join("_",
                        formattedDate,
                        symbol.ID.Symbol.ToLowerInvariant(),
                        resolution.ResolutionToLower(),
                        tickType.TickTypeToLower(),
                        expirationTag
                        ) + ".csv";

                case SecurityType.Commodity:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Generates the zip file name for the specified date of data.
        /// </summary>
        public static string GenerateZipFileName(Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            var tickTypeString = tickType.TickTypeToLower();
            var formattedDate = date.ToStringInvariant(DateFormat.EightCharacter);
            var isHourOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Base:
                case SecurityType.Index:
                case SecurityType.Equity:
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    if (isHourOrDaily)
                    {
                        return $"{symbol.Value.ToLowerInvariant()}.zip";
                    }

                    return $"{formattedDate}_{tickTypeString}.zip";
                case SecurityType.Crypto:
                    if (isHourOrDaily)
                    {
                        return $"{symbol.Value.ToLowerInvariant()}_{tickTypeString}.zip";
                    }

                    return $"{formattedDate}_{tickTypeString}.zip";
                case SecurityType.Option:
                    if (isHourOrDaily)
                    {
                        // see TryParsePath: he knows tick type position is 3
                        var optionPath = symbol.Underlying.Value.ToLowerInvariant();
                        return $"{optionPath}_{date.Year}_{tickTypeString}_{symbol.ID.OptionStyle.OptionStyleToLower()}.zip";
                    }

                    return $"{formattedDate}_{tickTypeString}_{symbol.ID.OptionStyle.OptionStyleToLower()}.zip";

                case SecurityType.IndexOption:
                case SecurityType.FutureOption:
                    if (isHourOrDaily)
                    {
                        // see TryParsePath: he knows tick type position is 3
                        var optionTickerBasedPath = symbol.ID.Symbol.ToLowerInvariant();
                        return $"{optionTickerBasedPath}_{date.Year}_{tickTypeString}_{symbol.ID.OptionStyle.OptionStyleToLower()}.zip";
                    }

                    return $"{formattedDate}_{tickTypeString}_{symbol.ID.OptionStyle.OptionStyleToLower()}.zip";

                case SecurityType.Future:
                case SecurityType.CryptoFuture:
                    if (isHourOrDaily)
                    {
                        return $"{symbol.ID.Symbol.ToLowerInvariant()}_{tickTypeString}.zip";
                    }

                    return $"{formattedDate}_{tickTypeString}.zip";

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
                return $"{symbol.ToLowerInvariant()}.zip";
            }

            var zipFileName = date.ToStringInvariant(DateFormat.EightCharacter);

            if (tickType == null)
            {
                if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd)
                {
                    tickType = TickType.Quote;
                }
                else
                {
                    tickType = TickType.Trade;
                }
            }

            var suffix = Invariant($"_{tickType.Value.TickTypeToLower()}.zip");
            return zipFileName + suffix;
        }

        /// <summary>
        /// Gets the tick type most commonly associated with the specified security type
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>The most common tick type for the specified security type</returns>
        public static TickType GetCommonTickType(SecurityType securityType)
        {
            if (securityType == SecurityType.Forex || securityType == SecurityType.Cfd || securityType == SecurityType.Crypto)
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
                case SecurityType.FutureOption:
                case SecurityType.IndexOption:
                    if (isHourlyOrDaily)
                    {
                        var style = parts[2].ParseOptionStyle();
                        var right = parts[3].ParseOptionRight();
                        var strike = Parse.Decimal(parts[4]) / 10000m;
                        var expiry = Parse.DateTimeExact(parts[5], DateFormat.EightCharacter);
                        return Symbol.CreateOption(symbol.Underlying, symbol.ID.Symbol, symbol.ID.Market, style, right, strike, expiry);
                    }
                    else
                    {
                        var style = parts[4].ParseOptionStyle();
                        var right = parts[5].ParseOptionRight();
                        var strike = Parse.Decimal(parts[6]) / 10000m;
                        var expiry = DateTime.ParseExact(parts[7], DateFormat.EightCharacter, CultureInfo.InvariantCulture);
                        return Symbol.CreateOption(symbol.Underlying, symbol.ID.Symbol, symbol.ID.Market, style, right, strike, expiry);
                    }

                case SecurityType.Future:
                    if (isHourlyOrDaily)
                    {
                        var expiryYearMonth = Parse.DateTimeExact(parts[2], DateFormat.YearMonth);
                        var futureExpiryFunc = FuturesExpiryFunctions.FuturesExpiryFunction(symbol);
                        var futureExpiry = futureExpiryFunc(expiryYearMonth);
                        return Symbol.CreateFuture(parts[0], symbol.ID.Market, futureExpiry);
                    }
                    else
                    {
                        var expiryYearMonth = Parse.DateTimeExact(parts[4], DateFormat.YearMonth);
                        var futureExpiryFunc = FuturesExpiryFunctions.FuturesExpiryFunction(symbol);
                        var futureExpiry = futureExpiryFunc(expiryYearMonth);
                        return Symbol.CreateFuture(parts[1], symbol.ID.Market, futureExpiry);
                    }

                default:
                    throw new NotImplementedException(Invariant(
                        $"ReadSymbolFromZipEntry is not implemented for {symbol.ID.SecurityType} {symbol.ID.Market} {resolution}"
                    ));
            }
        }

        /// <summary>
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static long Scale(decimal value)
        {
            return (long)(value * 10000);
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
                    args[i] = ((decimal)value).Normalize();
                }
            }

            var argsFormatted = args.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture));
            return string.Join(",", argsFormatted);
        }

        /// <summary>
        /// Creates a scaled csv line for the bar, if null fills in empty strings
        /// </summary>
        private static string ToScaledCsv(IBar bar)
        {
            if (bar == null)
            {
                return ToCsv(string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return ToCsv(Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close));
        }


        /// <summary>
        /// Creates a non scaled csv line for the bar, if null fills in empty strings
        /// </summary>
        private static string ToNonScaledCsv(IBar bar)
        {
            if (bar == null)
            {
                return ToCsv(string.Empty, string.Empty, string.Empty, string.Empty);
            }

            return ToCsv(bar.Open, bar.High, bar.Low, bar.Close);
        }

        /// <summary>
        /// Get the <see cref="TickType"/> for common Lean data types.
        /// If not a Lean common data type, return a TickType of Trade.
        /// </summary>
        /// <param name="type">A Type used to determine the TickType</param>
        /// <param name="securityType">The SecurityType used to determine the TickType</param>
        /// <returns>A TickType corresponding to the type</returns>
        public static TickType GetCommonTickTypeForCommonDataTypes(Type type, SecurityType securityType)
        {
            if (type == typeof(TradeBar))
            {
                return TickType.Trade;
            }
            if (type == typeof(QuoteBar))
            {
                return TickType.Quote;
            }
            if (type == typeof(OpenInterest))
            {
                return TickType.OpenInterest;
            }
            if (type.IsAssignableTo(typeof(BaseChainUniverseData)))
            {
                return TickType.Quote;
            }
            if (type == typeof(Tick))
            {
                return GetCommonTickType(securityType);
            }

            return TickType.Trade;
        }

        /// <summary>
        /// Matches a data path security type with the <see cref="SecurityType"/>
        /// </summary>
        /// <remarks>This includes 'alternative'</remarks>
        /// <param name="securityType">The data path security type</param>
        /// <returns>The matching security type for the given data path</returns>
        public static SecurityType ParseDataSecurityType(string securityType)
        {
            if (securityType.Equals("alternative", StringComparison.InvariantCultureIgnoreCase))
            {
                return SecurityType.Base;
            }
            return (SecurityType)Enum.Parse(typeof(SecurityType), securityType, true);
        }

        /// <summary>
        /// Parses file name into a <see cref="Security"/> and DateTime
        /// </summary>
        /// <param name="fileName">File name to be parsed</param>
        /// <param name="securityType">The securityType as parsed from the fileName</param>
        /// <param name="market">The market as parsed from the fileName</param>
        public static bool TryParseSecurityType(string fileName, out SecurityType securityType, out string market)
        {
            securityType = SecurityType.Base;
            market = string.Empty;

            try
            {
                var info = SplitDataPath(fileName);

                // find the securityType and parse it
                var typeString = info.Find(x => SecurityTypeAsDataPath.Contains(x.ToLowerInvariant()));
                securityType = ParseDataSecurityType(typeString);

                var existingMarkets = Market.SupportedMarkets();
                var foundMarket = info.Find(x => existingMarkets.Contains(x.ToLowerInvariant()));
                if (foundMarket != null)
                {
                    market = foundMarket;
                }
            }
            catch (Exception e)
            {
                Log.Error($"LeanData.TryParsePath(): Error encountered while parsing the path {fileName}. Error: {e.GetBaseException()}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses file name into a <see cref="Security"/> and DateTime
        /// </summary>
        /// <param name="filePath">File path to be parsed</param>
        /// <param name="symbol">The symbol as parsed from the fileName</param>
        /// <param name="date">Date of data in the file path. Only returned if the resolution is lower than Hourly</param>
        /// <param name="resolution">The resolution of the symbol as parsed from the filePath</param>
        /// <param name="tickType">The tick type</param>
        /// <param name="dataType">The data type</param>
        public static bool TryParsePath(string filePath, out Symbol symbol, out DateTime date,
            out Resolution resolution, out TickType tickType, out Type dataType)
        {
            symbol = default;
            tickType = default;
            dataType = default;
            date = default;
            resolution = default;

            try
            {
                if (!TryParsePath(filePath, out symbol, out date, out resolution, out var isUniverses))
                {
                    return false;
                }

                tickType = GetCommonTickType(symbol.SecurityType);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.Contains('_', StringComparison.InvariantCulture))
                {
                    // example: 20140606_openinterest_american.zip
                    var tickTypePosition = 1;
                    if (resolution >= Resolution.Hour && symbol.SecurityType.IsOption())
                    {
                        // daily and hourly have the year too, example: aapl_2014_openinterest_american.zip
                        // see GenerateZipFileName he's creating these paths
                        tickTypePosition = 2;
                    }
                    tickType = (TickType)Enum.Parse(typeof(TickType), fileName.Split('_')[tickTypePosition], true);
                }

                dataType = isUniverses ? typeof(OptionUniverse) : GetDataType(resolution, tickType);
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug($"LeanData.TryParsePath(): Error encountered while parsing the path {filePath}. Error: {ex.GetBaseException()}");
            }
            return false;
        }

        /// <summary>
        /// Parses file name into a <see cref="Security"/> and DateTime
        /// </summary>
        /// <param name="fileName">File name to be parsed</param>
        /// <param name="symbol">The symbol as parsed from the fileName</param>
        /// <param name="date">Date of data in the file path. Only returned if the resolution is lower than Hourly</param>
        /// <param name="resolution">The resolution of the symbol as parsed from the filePath</param>
        public static bool TryParsePath(string fileName, out Symbol symbol, out DateTime date, out Resolution resolution)
        {
            return TryParsePath(fileName, out symbol, out date, out resolution, out _);
        }

        /// <summary>
        /// Parses file name into a <see cref="Security"/> and DateTime
        /// </summary>
        /// <param name="fileName">File name to be parsed</param>
        /// <param name="symbol">The symbol as parsed from the fileName</param>
        /// <param name="date">Date of data in the file path. Only returned if the resolution is lower than Hourly</param>
        /// <param name="resolution">The resolution of the symbol as parsed from the filePath</param>
        /// <param name="isUniverses">Outputs whether the file path represents a universe data file.</param>
        public static bool TryParsePath(string fileName, out Symbol symbol, out DateTime date, out Resolution resolution, out bool isUniverses)
        {
            symbol = null;
            resolution = Resolution.Daily;
            date = default(DateTime);
            isUniverses = default;

            try
            {
                var info = SplitDataPath(fileName);

                // find where the useful part of the path starts - i.e. the securityType
                var startIndex = info.FindIndex(x => SecurityTypeAsDataPath.Contains(x.ToLowerInvariant()));

                if (startIndex == -1)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"LeanData.TryParsePath(): Failed to parse '{fileName}' unexpected SecurityType");
                    }
                    // SPDB & MHDB folders
                    return false;
                }
                var securityType = ParseDataSecurityType(info[startIndex]);

                var market = Market.USA;
                string ticker;

                if (!Enum.TryParse(info[startIndex + 2], true, out resolution))
                {
                    resolution = Resolution.Daily;
                    isUniverses = info[startIndex + 2].Equals("universes", StringComparison.InvariantCultureIgnoreCase);
                    if (securityType != SecurityType.Base)
                    {
                        if (!isUniverses)
                        {
                            if (Log.DebuggingEnabled)
                            {
                                Log.Debug($"LeanData.TryParsePath(): Failed to parse '{fileName}' unexpected Resolution");
                            }
                            // only acept a failure to parse resolution if we are facing a universes path
                            return false;
                        }

                        (symbol, date) = ParseUniversePath(info, securityType);
                        return true;
                    }
                }

                if (securityType == SecurityType.Base)
                {
                    // the last part of the path is the file name
                    var fileNameNoPath = info[info.Count - 1].Split('_').First();

                    if (!DateTime.TryParseExact(fileNameNoPath,
                        DateFormat.EightCharacter,
                        DateTimeFormatInfo.InvariantInfo,
                        DateTimeStyles.None,
                        out date))
                    {
                        // if parsing the date failed we assume filename is ticker
                        ticker = fileNameNoPath;
                    }
                    else
                    {
                        // ticker must be the previous part of the path
                        ticker = info[info.Count - 2];
                    }
                }
                else
                {
                    // Gather components used to create the security
                    market = info[startIndex + 1];
                    var components = info[startIndex + 3].Split('_');

                    // Remove the ticktype from the ticker (Only exists in Crypto and Future data but causes no issues)
                    ticker = components[0];

                    if (resolution < Resolution.Hour)
                    {
                        // Future options are special and have the following format Market/Resolution/Ticker/FutureExpiry/Date
                        var dateIndex = securityType == SecurityType.FutureOption ? startIndex + 5 : startIndex + 4;
                        date = Parse.DateTimeExact(info[dateIndex].Substring(0, 8), DateFormat.EightCharacter);
                    }
                    // If resolution is Daily or Hour for options and index options, we can only get the year from the path
                    else if (securityType == SecurityType.Option || securityType == SecurityType.IndexOption)
                    {
                        var year = int.Parse(components[1], CultureInfo.InvariantCulture);
                        date = new DateTime(year, 01, 01);
                    }
                }

                if (securityType == SecurityType.FutureOption)
                {
                    // Future options have underlying FutureExpiry date as the parent dir for the zips, we need this for our underlying
                    symbol = CreateSymbol(ticker, securityType, market, null, Parse.DateTimeExact(info[startIndex + 4].Substring(0, 8), DateFormat.EightCharacter));
                }
                else
                {
                    symbol = CreateSymbol(ticker, securityType, market, null, date);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"LeanData.TryParsePath(): Error encountered while parsing the path {fileName}. Error: {ex.GetBaseException()}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the universe file path and extracts the corresponding symbol and file date.
        /// </summary>
        /// <param name="filePathParts">
        /// A list of strings representing the file path segments. The expected structure is:
        /// <para>General format: ["data", SecurityType, Market, "universes", ...]</para>
        /// <para>Examples:</para>
        /// <list type="bullet">
        /// <item><description>Equity: <c>data/equity/usa/universes/etf/spy/20201130.csv</c></description></item>
        /// <item><description>Option: <c>data/option/usa/universes/aapl/20241112.csv</c></description></item>
        /// <item><description>Future: <c>data/future/cme/universes/es/20130710.csv</c></description></item>
        /// <item><description>Future Option: <c>data/futureoption/cme/universes/20120401/20111230.csv</c></description></item>
        /// </list>
        /// </param>
        /// <param name="securityType">The type of security for which the symbol is being created.</param>
        /// <returns>A tuple containing the parsed <see cref="Symbol"/> and the universe processing file date.</returns>
        /// <exception cref="ArgumentException">Thrown if the file path does not contain 'universes'.</exception>
        /// <exception cref="NotSupportedException">Thrown if the security type is not supported.</exception>
        private static (Symbol symbol, DateTime processingDate) ParseUniversePath(IReadOnlyList<string> filePathParts, SecurityType securityType)
        {
            if (!filePathParts.Contains("universes", StringComparer.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"LeanData.{nameof(ParseUniversePath)}:The file path must contain a 'universes' part, but it was not found.");
            }

            var symbol = default(Symbol);
            var market = filePathParts[2];
            var ticker = filePathParts[^2];
            var universeFileDate = DateTime.ParseExact(filePathParts[^1], DateFormat.EightCharacter, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None);
            switch (securityType)
            {
                case SecurityType.Equity:
                    securityType = SecurityType.Base;
                    var dataType = filePathParts.Contains("etf", StringComparer.InvariantCultureIgnoreCase) ? typeof(ETFConstituentUniverse) : default;
                    symbol = CreateSymbol(ticker, securityType, market, dataType, universeFileDate);
                    break;
                case SecurityType.Option:
                    symbol = CreateSymbol(ticker, securityType, market, null, universeFileDate);
                    break;
                case SecurityType.IndexOption:
                    symbol = CreateSymbol(ticker, securityType, market, null, default);
                    break;
                case SecurityType.FutureOption:
                    symbol = CreateSymbol(filePathParts[^3], securityType, market, null, Parse.DateTimeExact(filePathParts[^2], DateFormat.EightCharacter));
                    break;
                case SecurityType.Future:
                    var mapUnderlyingTicker = OptionSymbol.MapToUnderlying(ticker, securityType);
                    symbol = Symbol.CreateFuture(mapUnderlyingTicker, market, universeFileDate);
                    break;
                default:
                    throw new NotSupportedException($"LeanData.{nameof(ParseUniversePath)}:The security type '{securityType}' is not supported for data universe files.");
            }

            return (symbol, universeFileDate);
        }

        /// <summary>
        /// Creates a new Symbol based on parsed data path information.
        /// </summary>
        /// <param name="ticker">The parsed ticker symbol.</param>
        /// <param name="securityType">The parsed type of security.</param>
        /// <param name="market">The parsed market or exchange.</param>
        /// <param name="dataType">Optional type used for generating the base data SID (applicable only for SecurityType.Base).</param>
        /// <param name="mappingResolveDate">The date used in path parsing to create the correct symbol.</param>
        /// <returns>A unique security identifier.</returns>
        /// <example>
        /// <code>
        /// path: equity/usa/minute/spwr/20071223_trade.zip
        /// ticker: spwr
        /// securityType: equity
        /// market: usa
        /// mappingResolveDate: 2007/12/23
        /// </code>
        /// </example>
        private static Symbol CreateSymbol(string ticker, SecurityType securityType, string market, Type dataType, DateTime mappingResolveDate = default)
        {
            if (mappingResolveDate != default && (securityType == SecurityType.Equity || securityType == SecurityType.Option))
            {
                var symbol = new Symbol(SecurityIdentifier.GenerateEquity(ticker, market, mappingResolveDate: mappingResolveDate), ticker);
                return securityType == SecurityType.Option ? Symbol.CreateCanonicalOption(symbol) : symbol;
            }
            else if (securityType == SecurityType.FutureOption)
            {
                var underlyingTicker = OptionSymbol.MapToUnderlying(ticker, securityType);
                // Create our underlying future and then the Canonical option for this future
                var underlyingFuture = Symbol.CreateFuture(underlyingTicker, market, mappingResolveDate);
                return Symbol.CreateCanonicalOption(underlyingFuture);
            }
            else if (securityType == SecurityType.IndexOption)
            {
                var underlyingTicker = OptionSymbol.MapToUnderlying(ticker, securityType);
                // Create our underlying index and then the Canonical option
                var underlyingIndex = Symbol.Create(underlyingTicker, SecurityType.Index, market);
                return Symbol.CreateCanonicalOption(underlyingIndex, ticker, market, null);
            }
            else
            {
                return Symbol.Create(ticker, securityType, market, baseDataType: dataType);
            }
        }

        private static List<string> SplitDataPath(string fileName)
        {
            var pathSeparators = new[] { '/', '\\' };

            // Removes file extension
            fileName = fileName.Replace(fileName.GetExtension(), string.Empty);

            // remove any relative file path
            while (fileName.First() == '.' || pathSeparators.Any(x => x == fileName.First()))
            {
                fileName = fileName.Remove(0, 1);
            }

            // split path into components
            return fileName.Split(pathSeparators, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Aggregates a list of second/minute bars at the requested resolution
        /// </summary>
        /// <param name="bars">List of <see cref="TradeBar"/>s</param>
        /// <param name="symbol">Symbol of all tradeBars</param>
        /// <param name="resolution">Desired resolution for new <see cref="TradeBar"/>s</param>
        /// <returns>List of aggregated <see cref="TradeBar"/>s</returns>
        public static IEnumerable<TradeBar> AggregateTradeBars(IEnumerable<TradeBar> bars, Symbol symbol, TimeSpan resolution)
        {
            return Aggregate(new TradeBarConsolidator(resolution), bars, symbol);
        }

        /// <summary>
        /// Aggregates a list of second/minute bars at the requested resolution
        /// </summary>
        /// <param name="bars">List of <see cref="QuoteBar"/>s</param>
        /// <param name="symbol">Symbol of all QuoteBars</param>
        /// <param name="resolution">Desired resolution for new <see cref="QuoteBar"/>s</param>
        /// <returns>List of aggregated <see cref="QuoteBar"/>s</returns>
        public static IEnumerable<QuoteBar> AggregateQuoteBars(IEnumerable<QuoteBar> bars, Symbol symbol, TimeSpan resolution)
        {
            return Aggregate(new QuoteBarConsolidator(resolution), bars, symbol);
        }

        /// <summary>
        /// Aggregates a list of ticks at the requested resolution
        /// </summary>
        /// <param name="ticks">List of quote ticks</param>
        /// <param name="symbol">Symbol of all ticks</param>
        /// <param name="resolution">Desired resolution for new <see cref="QuoteBar"/>s</param>
        /// <returns>List of aggregated <see cref="QuoteBar"/>s</returns>
        public static IEnumerable<QuoteBar> AggregateTicks(IEnumerable<Tick> ticks, Symbol symbol, TimeSpan resolution)
        {
            return Aggregate(new TickQuoteBarConsolidator(resolution), ticks, symbol);
        }

        /// <summary>
        /// Aggregates a list of ticks at the requested resolution
        /// </summary>
        /// <param name="ticks">List of trade ticks</param>
        /// <param name="symbol">Symbol of all ticks</param>
        /// <param name="resolution">Desired resolution for new <see cref="TradeBar"/>s</param>
        /// <returns>List of aggregated <see cref="TradeBar"/>s</returns>
        public static IEnumerable<TradeBar> AggregateTicksToTradeBars(IEnumerable<Tick> ticks, Symbol symbol, TimeSpan resolution)
        {
            return Aggregate(new TickConsolidator(resolution), ticks, symbol);
        }

        /// <summary>
        /// Helper method to return the start time and period of a bar the given point time should be part of
        /// </summary>
        /// <param name="exchangeTimeZoneDate">The point in time we want to get the bar information about</param>
        /// <param name="exchange">The associated security exchange</param>
        /// <param name="extendedMarketHours">True if extended market hours should be taken into consideration</param>
        /// <returns>The calendar information that holds a start time and a period</returns>
        public static CalendarInfo GetDailyCalendar(DateTime exchangeTimeZoneDate, SecurityExchange exchange, bool extendedMarketHours)
        {
            return GetDailyCalendar(exchangeTimeZoneDate, exchange.Hours, extendedMarketHours);
        }

        /// <summary>
        /// Helper method to return the start time and period of a bar the given point time should be part of
        /// </summary>
        /// <param name="exchangeTimeZoneDate">The point in time we want to get the bar information about</param>
        /// <param name="exchangeHours">The associated exchange hours</param>
        /// <param name="extendedMarketHours">True if extended market hours should be taken into consideration</param>
        /// <returns>The calendar information that holds a start time and a period</returns>
        public static CalendarInfo GetDailyCalendar(DateTime exchangeTimeZoneDate, SecurityExchangeHours exchangeHours, bool extendedMarketHours)
        {
            var startTime = exchangeHours.GetFirstDailyMarketOpen(exchangeTimeZoneDate, extendedMarketHours);
            var endTime = exchangeHours.GetLastDailyMarketClose(startTime, extendedMarketHours);
            var period = endTime - startTime;
            return new CalendarInfo(startTime, period);
        }

        /// <summary>
        /// Helper method to get the next daily end time, taking into account strict end times if appropriate
        /// </summary>
        public static DateTime GetNextDailyEndTime(Symbol symbol, DateTime exchangeTimeZoneDate, SecurityExchangeHours exchangeHours)
        {
            var nextMidnight = exchangeTimeZoneDate.Date.AddDays(1);
            if (!UseStrictEndTime(true, symbol, Time.OneDay, exchangeHours))
            {
                return nextMidnight;
            }

            var nextMarketClose = exchangeHours.GetLastDailyMarketClose(exchangeTimeZoneDate, extendedMarketHours: false);
            if (nextMarketClose > nextMidnight)
            {
                // if exchangeTimeZoneDate is after the previous close, the next close might be tomorrow
                if (!exchangeHours.IsOpen(exchangeTimeZoneDate, extendedMarketHours: false))
                {
                    return nextMarketClose;
                }
                return nextMidnight;
            }
            return nextMarketClose;
        }

        /// <summary>
        /// Helper method that defines the types of options that should use scale factor
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OptionUseScaleFactor(Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Option || symbol.SecurityType == SecurityType.IndexOption;
        }

        /// <summary>
        /// Helper method to determine if we should use strict end time
        /// </summary>
        /// <param name="symbol">The associated symbol</param>
        /// <param name="increment">The datas time increment</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UseStrictEndTime(bool dailyStrictEndTimeEnabled, Symbol symbol, TimeSpan increment, SecurityExchangeHours exchangeHours)
        {
            if (exchangeHours.IsMarketAlwaysOpen
                || increment <= Time.OneHour
                || symbol.SecurityType == SecurityType.Cfd && symbol.ID.Market == Market.Oanda
                || symbol.SecurityType == SecurityType.Forex
                || symbol.SecurityType == SecurityType.Base)
            {
                return false;
            }
            return dailyStrictEndTimeEnabled;
        }

        /// <summary>
        /// Helper method to determine if we should use strict end time
        /// </summary>
        public static bool UseDailyStrictEndTimes(IAlgorithmSettings settings, BaseDataRequest request, Symbol symbol, TimeSpan increment,
            SecurityExchangeHours exchangeHours = null)
        {
            return UseDailyStrictEndTimes(settings, request.DataType, symbol, increment, exchangeHours ?? request.ExchangeHours);
        }

        /// <summary>
        /// Helper method to determine if we should use strict end time
        /// </summary>
        public static bool UseDailyStrictEndTimes(IAlgorithmSettings settings, Type dataType, Symbol symbol, TimeSpan increment, SecurityExchangeHours exchangeHours)
        {
            return UseDailyStrictEndTimes(settings.DailyPreciseEndTime, dataType, symbol, increment, exchangeHours);
        }

        /// <summary>
        /// Helper method to determine if we should use strict end time
        /// </summary>
        public static bool UseDailyStrictEndTimes(bool dailyStrictEndTimeEnabled, Type dataType, Symbol symbol, TimeSpan increment, SecurityExchangeHours exchangeHours)
        {
            return UseDailyStrictEndTimes(dataType) && UseStrictEndTime(dailyStrictEndTimeEnabled, symbol, increment, exchangeHours);
        }

        /// <summary>
        /// True if this data type should use strict daily end times
        /// </summary>
        public static bool UseDailyStrictEndTimes(Type dataType)
        {
            return dataType != null && _strictDailyEndTimesDataTypes.Contains(dataType);
        }

        /// <summary>
        /// Helper method that if appropiate, will set the Time and EndTime of the given data point to it's daily strict times
        /// </summary>
        /// <param name="baseData">The target data point</param>
        /// <param name="exchange">The associated exchange hours</param>
        /// <remarks>This method is used to set daily times on pre existing data, assuming it does not cover extended market hours</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetStrictEndTimes(IBaseData baseData, SecurityExchangeHours exchange)
        {
            if (baseData == null)
            {
                return false;
            }

            var dataType = baseData.GetType();
            if (!UseDailyStrictEndTimes(dataType))
            {
                return false;
            }

            var dailyCalendar = GetDailyCalendar(baseData.EndTime, exchange, extendedMarketHours: false);
            if (dailyCalendar.End < baseData.Time)
            {
                // this data point we were given is probably from extended market hours which we don't support for daily backtesting data
                return false;
            }
            baseData.Time = dailyCalendar.Start;
            baseData.EndTime = dailyCalendar.End;
            return true;
        }

        /// <summary>
        /// Helper to separate filename and entry from a given key for DataProviders
        /// </summary>
        /// <param name="key">The key to parse</param>
        /// <param name="fileName">File name extracted</param>
        /// <param name="entryName">Entry name extracted</param>
        public static void ParseKey(string key, out string fileName, out string entryName)
        {
            // Default scenario, no entryName included in key
            entryName = null; // default to all entries
            fileName = key;

            if (key == null)
            {
                return;
            }

            // Try extracting an entry name; Anything after a # sign
            var hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = key.Substring(hashIndex + 1);
                fileName = key.Substring(0, hashIndex);
            }
        }

        /// <summary>
        /// Helper method to determine if the specified data type supports extended market hours
        /// </summary>
        /// <param name="dataType">The data type</param>
        /// <returns>Whether the specified data type supports extended market hours</returns>
        public static bool SupportsExtendedMarketHours(Type dataType)
        {
            return !dataType.IsAssignableTo(typeof(BaseChainUniverseData));
        }

        /// <summary>
        /// Helper method to aggregate ticks or bars into lower frequency resolutions
        /// </summary>
        /// <typeparam name="T">Output type</typeparam>
        /// <typeparam name="K">Input type</typeparam>
        /// <param name="consolidator">The consolidator to use</param>
        /// <param name="dataPoints">The data point source</param>
        /// <param name="symbol">The symbol to output</param>
        private static IEnumerable<T> Aggregate<T, K>(PeriodCountConsolidatorBase<K, T> consolidator, IEnumerable<K> dataPoints, Symbol symbol)
            where T : BaseData
            where K : BaseData
        {
            IBaseData lastAggregated = null;
            var getConsolidatedBar = () =>
            {
                if (lastAggregated != consolidator.Consolidated && consolidator.Consolidated != null)
                {
                    // if there's a new aggregated bar we set the symbol & return it
                    lastAggregated = consolidator.Consolidated;
                    lastAggregated.Symbol = symbol;
                    return lastAggregated;
                }
                return null;
            };

            foreach (var dataPoint in dataPoints)
            {
                consolidator.Update(dataPoint);
                var consolidated = getConsolidatedBar();
                if (consolidated != null)
                {
                    yield return (T)consolidated;
                }
            }

            // flush any partial bar
            consolidator.Scan(Time.EndOfTime);
            var lastConsolidated = getConsolidatedBar();
            if (lastConsolidated != null)
            {
                yield return (T)lastConsolidated;
            }

            // cleanup
            consolidator.DisposeSafely();
        }
    }
}
