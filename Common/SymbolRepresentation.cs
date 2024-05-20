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
using System.Linq;
using QuantConnect.Logging;
using System.Globalization;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Public static helper class that does parsing/generation of symbol representations (options, futures)
    /// </summary>
    public static class SymbolRepresentation
    {
        private static DateTime TodayUtc = DateTime.UtcNow;

        /// <summary>
        /// Class contains future ticker properties returned by ParseFutureTicker()
        /// </summary>
        public class FutureTickerProperties
        {
            /// <summary>
            /// Underlying name
            /// </summary>
            public string Underlying { get; set;  }

            /// <summary>
            /// Short expiration year
            /// </summary>
            public int ExpirationYearShort { get; set; }

            /// <summary>
            /// Short expiration year digits
            /// </summary>
            public int ExpirationYearShortLength { get; set; }

            /// <summary>
            /// Expiration month
            /// </summary>
            public int ExpirationMonth { get; set; }

            /// <summary>
            /// Expiration day
            /// </summary>
            public int ExpirationDay { get; set; }
        }

        /// <summary>
        /// Class contains option ticker properties returned by ParseOptionTickerIQFeed()
        /// </summary>
        public class OptionTickerProperties
        {
            /// <summary>
            /// Underlying name
            /// </summary>
            public string Underlying { get; set; }

            /// <summary>
            /// Option right
            /// </summary>
            public OptionRight OptionRight { get; set; }

            /// <summary>
            /// Option strike
            /// </summary>
            public decimal OptionStrike { get; set; }

            /// <summary>
            /// Expiration date
            /// </summary>
            public DateTime ExpirationDate { get; set; }
        }


        /// <summary>
        /// Function returns underlying name, expiration year, expiration month, expiration day for the future contract ticker. Function detects if
        /// the format used is either 1 or 2 digits year, and if day code is present (will default to 1rst day of month). Returns null, if parsing failed.
        /// Format [Ticker][2 digit day code OPTIONAL][1 char month code][2/1 digit year code]
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns>Results containing 1) underlying name, 2) short expiration year, 3) expiration month</returns>
        public static FutureTickerProperties ParseFutureTicker(string ticker)
        {
            var doubleDigitYear = char.IsDigit(ticker.Substring(ticker.Length - 2, 1)[0]);
            var doubleDigitOffset = doubleDigitYear ? 1 : 0;

            var expirationDayOffset = 0;
            var expirationDay = 1;
            if (ticker.Length > 4 + doubleDigitOffset)
            {
                var potentialExpirationDay = ticker.Substring(ticker.Length - 4 - doubleDigitOffset, 2);
                var containsExpirationDay = char.IsDigit(potentialExpirationDay[0]) && char.IsDigit(potentialExpirationDay[1]);
                expirationDayOffset = containsExpirationDay ? 2 : 0;
                if (containsExpirationDay && !int.TryParse(potentialExpirationDay, out expirationDay))
                {
                    return null;
                }
            }

            var expirationYearString = ticker.Substring(ticker.Length - 1 - doubleDigitOffset, 1 + doubleDigitOffset);
            var expirationMonthString = ticker.Substring(ticker.Length - 2 - doubleDigitOffset, 1);
            var underlyingString = ticker.Substring(0, ticker.Length - 2 - doubleDigitOffset - expirationDayOffset);

            int expirationYearShort;

            if (!int.TryParse(expirationYearString, out expirationYearShort))
            {
                return null;
            }

            if (!FuturesMonthCodeLookup.ContainsKey(expirationMonthString))
            {
                return null;
            }

            var expirationMonth = FuturesMonthCodeLookup[expirationMonthString];

            return new FutureTickerProperties
            {
                Underlying = underlyingString,
                ExpirationYearShort = expirationYearShort,
                ExpirationYearShortLength = expirationYearString.Length,
                ExpirationMonth = expirationMonth,
                ExpirationDay = expirationDay
            };
        }

        /// <summary>
        /// Helper method to parse and generate a future symbol from a given user friendly representation
        /// </summary>
        /// <param name="ticker">The future ticker, for example 'ESZ1'</param>
        /// <param name="futureYear">Clarifies the year for the current future</param>
        /// <returns>The future symbol or null if failed</returns>
        public static Symbol ParseFutureSymbol(string ticker, int? futureYear = null)
        {
            var parsed = ParseFutureTicker(ticker);
            if (parsed == null)
            {
                return null;
            }

            var underlying = parsed.Underlying;
            var expirationMonth = parsed.ExpirationMonth;
            var expirationYear = GetExpirationYear(futureYear, parsed);

            if (!SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(underlying, SecurityType.Future, out var market))
            {
                Log.Debug($@"SymbolRepresentation.ParseFutureSymbol(): {
                    Messages.SymbolRepresentation.FailedToGetMarketForTickerAndUnderlying(ticker, underlying)}");
                return null;
            }

            var expiryFunc = FuturesExpiryFunctions.FuturesExpiryFunction(Symbol.Create(underlying, SecurityType.Future, market));
            var expiryDate = expiryFunc(new DateTime(expirationYear, expirationMonth, 1));

            return Symbol.CreateFuture(underlying, market, expiryDate);
        }

        /// <summary>
        /// Creates a future option Symbol from the provided ticker
        /// </summary>
        /// <param name="ticker">The future option ticker, for example 'ESZ0 P3590'</param>
        /// <param name="strikeScale">Optional the future option strike scale factor</param>
        public static Symbol ParseFutureOptionSymbol(string ticker, int strikeScale = 1)
        {
            var split = ticker.Split(' ');
            if (split.Length != 2)
            {
                return null;
            }

            var parsed = ParseFutureTicker(split[0]);
            if (parsed == null)
            {
                return null;
            }
            ticker = parsed.Underlying;

            OptionRight right;
            if (split[1][0] == 'P' || split[1][0] == 'p')
            {
                right = OptionRight.Put;
            }
            else if (split[1][0] == 'C' || split[1][0] == 'c')
            {
                right = OptionRight.Call;
            }
            else
            {
                return null;
            }
            var strike = split[1].Substring(1);

            if (parsed.ExpirationYearShort < 10)
            {
                parsed.ExpirationYearShort += 20;
            }
            var expirationYearParsed = 2000 + parsed.ExpirationYearShort;

            var expirationDate = new DateTime(expirationYearParsed, parsed.ExpirationMonth, 1);

            var strikePrice = decimal.Parse(strike, NumberStyles.Any, CultureInfo.InvariantCulture);
            var futureTicker = FuturesOptionsSymbolMappings.MapFromOption(ticker);

            if (!SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(futureTicker, SecurityType.Future, out var market))
            {
                Log.Debug($"SymbolRepresentation.ParseFutureOptionSymbol(): {Messages.SymbolRepresentation.NoMarketFound(futureTicker)}");
                return null;
            }

            var canonicalFuture = Symbol.Create(futureTicker, SecurityType.Future, market);
            var futureExpiry = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalFuture)(expirationDate);
            var future = Symbol.CreateFuture(futureTicker, market, futureExpiry);

            var futureOptionExpiry = FuturesOptionsExpiryFunctions.GetFutureOptionExpiryFromFutureExpiry(future);

            return Symbol.CreateOption(future,
                market,
                OptionStyle.American,
                right,
                strikePrice / strikeScale,
                futureOptionExpiry);
        }

        /// <summary>
        /// Returns future symbol ticker from underlying and expiration date. Function can generate tickers of two formats: one and two digits year.
        /// Format [Ticker][2 digit day code][1 char month code][2/1 digit year code], more information at http://help.tradestation.com/09_01/tradestationhelp/symbology/futures_symbology.htm
        /// </summary>
        /// <param name="underlying">String underlying</param>
        /// <param name="expiration">Expiration date</param>
        /// <param name="doubleDigitsYear">True if year should represented by two digits; False - one digit</param>
        /// <param name="includeExpirationDate">True if expiration date should be included</param>
        /// <returns>The user friendly future ticker</returns>
        public static string GenerateFutureTicker(string underlying, DateTime expiration, bool doubleDigitsYear = true, bool includeExpirationDate = true)
        {
            var year = doubleDigitsYear ? expiration.Year % 100 : expiration.Year % 10;
            var month = expiration.Month;

            var contractMonthDelta = FuturesExpiryUtilityFunctions.GetDeltaBetweenContractMonthAndContractExpiry(underlying, expiration.Date);
            if (contractMonthDelta < 0)
            {
                // For futures that have an expiry after the contract month.
                // This is for dairy contracts, which can and do expire after the contract month.
                var expirationMonth = expiration.AddDays(-(expiration.Day - 1))
                    .AddMonths(contractMonthDelta);

                month = expirationMonth.Month;
                year = doubleDigitsYear ? expirationMonth.Year % 100 : expirationMonth.Year % 10;
            }
            else {
                // These futures expire in the month before or in the contract month
                month += contractMonthDelta;

                // Get the month back into the allowable range, allowing for a wrap
                // Below is a little algorithm for wrapping numbers with a certain bounds.
                // In this case, were dealing with months, wrapping to years once we get to January
                // As modulo works for [0, x), it's best to subtract 1 (as months are [1, 12] to convert to [0, 11]),
                // do the modulo/integer division, then add 1 back on to get into the correct range again
                month--;
                year += month / 12;
                month %= 12;
                month++;
            }

            var expirationDay = includeExpirationDate ? $"{expiration.Day:00}" : string.Empty;

            return $"{underlying}{expirationDay}{FuturesMonthLookup[month]}{year}";
        }

        /// <summary>
        /// Returns option symbol ticker in accordance with OSI symbology
        /// More information can be found at http://www.optionsclearing.com/components/docs/initiatives/symbology/symbology_initiative_v1_8.pdf
        /// </summary>
        /// <param name="symbol">Symbol object to create OSI ticker from</param>
        /// <returns>The OSI ticker representation</returns>
        public static string GenerateOptionTickerOSI(this Symbol symbol)
        {
            if (!symbol.SecurityType.IsOption())
            {
                throw new ArgumentException(
                    Messages.SymbolRepresentation.UnexpectedSecurityTypeForMethod(nameof(GenerateOptionTickerOSI), symbol.SecurityType));
            }

            return GenerateOptionTickerOSI(symbol.Underlying.Value, symbol.ID.OptionRight, symbol.ID.StrikePrice, symbol.ID.Date);
        }

        /// <summary>
        /// Returns option symbol ticker in accordance with OSI symbology
        /// More information can be found at http://www.optionsclearing.com/components/docs/initiatives/symbology/symbology_initiative_v1_8.pdf
        /// </summary>
        /// <param name="underlying">Underlying string</param>
        /// <param name="right">Option right</param>
        /// <param name="strikePrice">Option strike</param>
        /// <param name="expiration">Option expiration date</param>
        /// <returns>The OSI ticker representation</returns>
        public static string GenerateOptionTickerOSI(string underlying, OptionRight right, decimal strikePrice, DateTime expiration)
        {
            if (underlying.Length > 5) underlying += " ";
            return Invariant($"{underlying,-6}{expiration.ToStringInvariant(DateFormat.SixCharacter)}{right.ToStringPerformance()[0]}{(strikePrice * 1000m):00000000}");
        }

        /// <summary>
        /// Parses the specified OSI options ticker into a Symbol object
        /// </summary>
        /// <param name="ticker">The OSI compliant option ticker string</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The associated market</param>
        /// <returns>Symbol object for the specified OSI option ticker string</returns>
        public static Symbol ParseOptionTickerOSI(string ticker, SecurityType securityType = SecurityType.Option, string market = Market.USA)
        {
            return ParseOptionTickerOSI(ticker, securityType, OptionStyle.American, market);
        }

        /// <summary>
        /// Parses the specified OSI options ticker into a Symbol object
        /// </summary>
        /// <param name="ticker">The OSI compliant option ticker string</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The associated market</param>
        /// <param name="optionStyle">The option style</param>
        /// <returns>Symbol object for the specified OSI option ticker string</returns>
        public static Symbol ParseOptionTickerOSI(string ticker, SecurityType securityType, OptionStyle optionStyle, string market)
        {
            var optionTicker = ticker.Substring(0, 6).Trim();
            var expiration = DateTime.ParseExact(ticker.Substring(6, 6), DateFormat.SixCharacter, null);
            OptionRight right;
            if (ticker[12] == 'C' || ticker[12] == 'c')
            {
                right = OptionRight.Call;
            }
            else if (ticker[12] == 'P' || ticker[12] == 'p')
            {
                right = OptionRight.Put;
            }
            else
            {
                throw new FormatException(Messages.SymbolRepresentation.UnexpectedOptionRightFormatForParseOptionTickerOSI(ticker));
            }
            var strike = Parse.Decimal(ticker.Substring(13, 8)) / 1000m;
            SecurityIdentifier underlyingSid;
            if (securityType == SecurityType.Option)
            {
                underlyingSid = SecurityIdentifier.GenerateEquity(optionTicker, market);
                // let it fallback to it's default handling, which include mapping
                optionTicker = null;
            }
            else if(securityType == SecurityType.IndexOption)
            {
                underlyingSid = SecurityIdentifier.GenerateIndex(OptionSymbol.MapToUnderlying(optionTicker, securityType), market);
            }
            else
            {
                throw new NotImplementedException($"ParseOptionTickerOSI(): {Messages.SymbolRepresentation.SecurityTypeNotImplemented(securityType)}");
            }
            var sid = SecurityIdentifier.GenerateOption(expiration, underlyingSid, optionTicker, market, strike, right, optionStyle);
            return new Symbol(sid, ticker, new Symbol(underlyingSid, underlyingSid.Symbol));
        }

        /// <summary>
        /// Function returns option ticker from IQFeed option ticker
        /// For example CSCO1220V19 Cisco October Put at 19.00 Expiring on 10/20/12
        /// Symbology details: http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&amp;displayaction=support%C2%A7ion=guide&amp;web=iqfeed&amp;guide=options&amp;web=IQFeed&amp;type=stock
        /// </summary>
        /// <param name="symbol">THe option symbol</param>
        /// <returns>The option ticker</returns>
        public static string GenerateOptionTicker(Symbol symbol)
        {
            var letter = _optionSymbology.Where(x => x.Value.Item2 == symbol.ID.OptionRight && x.Value.Item1 == symbol.ID.Date.Month).Select(x => x.Key).Single();
            var twoYearDigit = symbol.ID.Date.ToString("yy");
            return $"{SecurityIdentifier.Ticker(symbol.Underlying, symbol.ID.Date)}{twoYearDigit}{symbol.ID.Date.Day:00}{letter}{symbol.ID.StrikePrice.ToStringInvariant()}";
        }

        /// <summary>
        /// Function returns option contract parameters (underlying name, expiration date, strike, right) from IQFeed option ticker
        /// Symbology details: http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&amp;displayaction=support%C2%A7ion=guide&amp;web=iqfeed&amp;guide=options&amp;web=IQFeed&amp;type=stock
        /// </summary>
        /// <param name="ticker">IQFeed option ticker</param>
        /// <returns>Results containing 1) underlying name, 2) option right, 3) option strike 4) expiration date</returns>
        public static OptionTickerProperties ParseOptionTickerIQFeed(string ticker)
        {
            var letterRange = _optionSymbology.Keys
                            .Select(x => x[0])
                            .ToArray();
            var optionTypeDelimiter = ticker.LastIndexOfAny(letterRange);
            var strikePriceString = ticker.Substring(optionTypeDelimiter + 1, ticker.Length - optionTypeDelimiter - 1);

            var lookupResult = _optionSymbology[ticker[optionTypeDelimiter].ToStringInvariant()];
            var month = lookupResult.Item1;
            var optionRight = lookupResult.Item2;

            var dayString = ticker.Substring(optionTypeDelimiter - 2, 2);
            var yearString = ticker.Substring(optionTypeDelimiter - 4, 2);
            var underlying = ticker.Substring(0, optionTypeDelimiter - 4);

            // if we cannot parse strike price, we ignore this contract, but log the information.
            Decimal strikePrice;
            if (!Decimal.TryParse(strikePriceString, NumberStyles.Any, CultureInfo.InvariantCulture, out strikePrice))
            {
                return null;
            }

            int day;

            if (!int.TryParse(dayString, out day))
            {
                return null;
            }

            int year;

            if (!int.TryParse(yearString, out year))
            {
                return null;
            }

            var expirationDate = new DateTime(2000 + year, month, day);

            return new OptionTickerProperties
            {
                Underlying = underlying,
                OptionRight = optionRight,
                OptionStrike = strikePrice,
                ExpirationDate = expirationDate
            };
        }


        // This table describes IQFeed option symbology
        private static Dictionary<string, Tuple<int, OptionRight>> _optionSymbology = new Dictionary<string, Tuple<int, OptionRight>>
                        {
                            { "A", Tuple.Create(1, OptionRight.Call) }, { "M", Tuple.Create(1, OptionRight.Put) },
                            { "B", Tuple.Create(2, OptionRight.Call) }, { "N", Tuple.Create(2, OptionRight.Put) },
                            { "C", Tuple.Create(3, OptionRight.Call) }, { "O", Tuple.Create(3, OptionRight.Put) },
                            { "D", Tuple.Create(4, OptionRight.Call) }, { "P", Tuple.Create(4, OptionRight.Put) },
                            { "E", Tuple.Create(5, OptionRight.Call) }, { "Q", Tuple.Create(5, OptionRight.Put) },
                            { "F", Tuple.Create(6, OptionRight.Call) }, { "R", Tuple.Create(6, OptionRight.Put) },
                            { "G", Tuple.Create(7, OptionRight.Call) }, { "S", Tuple.Create(7, OptionRight.Put) },
                            { "H", Tuple.Create(8, OptionRight.Call) }, { "T", Tuple.Create(8, OptionRight.Put) },
                            { "I", Tuple.Create(9, OptionRight.Call) }, { "U", Tuple.Create(9, OptionRight.Put) },
                            { "J", Tuple.Create(10, OptionRight.Call) }, { "V", Tuple.Create(10, OptionRight.Put) },
                            { "K", Tuple.Create(11, OptionRight.Call) }, { "W", Tuple.Create(11, OptionRight.Put) },
                            { "L", Tuple.Create(12, OptionRight.Call) }, { "X", Tuple.Create(12, OptionRight.Put) },

                        };


        /// <summary>
        /// Provides a lookup dictionary for mapping futures month codes to their corresponding numeric values.
        /// </summary>
        public static IReadOnlyDictionary<string, int> FuturesMonthCodeLookup { get; } = new Dictionary<string, int>
        {
            { "F", 1 }, // January
            { "G", 2 }, // February
            { "H", 3 }, // March
            { "J", 4 }, // April
            { "K", 5 }, // May
            { "M", 6 }, // June
            { "N", 7 }, // July
            { "Q", 8 }, // August
            { "U", 9 }, // September
            { "V", 10 }, // October
            { "X", 11 }, // November
            { "Z", 12 } // December
        };

        /// <summary>
        /// Provides a lookup dictionary for mapping numeric values to their corresponding futures month codes.
        /// </summary>
        public static IReadOnlyDictionary<int, string> FuturesMonthLookup { get; } = FuturesMonthCodeLookup.ToDictionary(kv => kv.Value, kv => kv.Key);

        /// <summary>
        /// Get the expiration year from short year (two-digit integer).
        /// Examples: NQZ23 and NQZ3 for Dec 2023
        /// </summary>
        /// <param name="futureYear">Clarifies the year for the current future</param>
        /// <param name="shortYear">Year in 2 digits format (23 represents 2023)</param>
        /// <returns>Tickers from live trading may not provide the four-digit year.</returns>
        private static int GetExpirationYear(int? futureYear, FutureTickerProperties parsed)
        {
            if(futureYear.HasValue)
            {
                var referenceYear = 1900 + parsed.ExpirationYearShort;
                while(referenceYear < futureYear.Value)
                {
                    referenceYear += 10;
                }

                return referenceYear;
            }

            var currentYear = DateTime.UtcNow.Year;
            if (parsed.ExpirationYearShortLength > 1)
            {
                // we are given a double digit year
                return 2000 + parsed.ExpirationYearShort;
            }

            var baseYear = ((int)Math.Round(currentYear / 10.0)) * 10 + parsed.ExpirationYearShort;
            while (baseYear < currentYear)
            {
                baseYear += 10;
            }
            return baseYear;
        }
    }
}
