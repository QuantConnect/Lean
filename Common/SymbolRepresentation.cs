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
using System.Linq;
using QuantConnect.Securities.Future;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Public static helper class that does parsing/generation of symbol representations (options, futures)
    /// </summary>
    public static class SymbolRepresentation
    {
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
            public int ExpirationYearShort { get; set;  }

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

            if (!_futuresMonthCodeLookup.ContainsKey(expirationMonthString))
            {
                return null;
            }

            var expirationMonth = _futuresMonthCodeLookup[expirationMonthString];

            return new FutureTickerProperties
            {
                Underlying = underlyingString,
                ExpirationYearShort = expirationYearShort,
                ExpirationMonth = expirationMonth,
                ExpirationDay = expirationDay
            };
        }

        /// <summary>
        /// Returns future symbol ticker from underlying and expiration date. Function can generate tickers of two formats: one and two digits year.
        /// Format [Ticker][2 digit day code][1 char month code][2/1 digit year code], more information at http://help.tradestation.com/09_01/tradestationhelp/symbology/futures_symbology.htm
        /// </summary>
        /// <param name="underlying">String underlying</param>
        /// <param name="expiration">Expiration date</param>
        /// <param name="doubleDigitsYear">True if year should represented by two digits; False - one digit</param>
        /// <returns></returns>
        public static string GenerateFutureTicker(string underlying, DateTime expiration, bool doubleDigitsYear = true)
        {
            var year = doubleDigitsYear ? expiration.Year % 100 : expiration.Year % 10;
            var month = expiration.Month;

            // These futures expire in the month before the contract month
            if (FuturesExpiryUtilityFunctions.ExpiresInPreviousMonth(underlying))
            {
                if (month < 12)
                {
                    month++;
                }
                else
                {
                    month = 1;
                    year++;
                }
            }

            return $"{underlying}{expiration.Day:00}{_futuresMonthLookup[month]}{year}";
        }

        /// <summary>
        /// Returns option symbol ticker in accordance with OSI symbology
        /// More information can be found at http://www.optionsclearing.com/components/docs/initiatives/symbology/symbology_initiative_v1_8.pdf
        /// </summary>
        /// <param name="symbol">Symbol object to create OSI ticker from</param>
        /// <returns>The OSI ticker representation</returns>
        public static string GenerateOptionTickerOSI(Symbol symbol)
        {
            if (symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException(Invariant($"{nameof(GenerateOptionTickerOSI)} returns symbol to be an option, received {symbol.SecurityType}."));
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
            return Invariant($"{underlying,-6}{expiration.ToStringInvariant(DateFormat.SixCharacter)}{right.ToString()[0]}{(strikePrice * 1000m):00000000}");
        }

        /// <summary>
        /// Parses the specified OSI options ticker into a Symbol object
        /// </summary>
        /// <param name="ticker">The OSI compliant option ticker string</param>
        /// <returns>Symbol object for the specified OSI option ticker string</returns>
        public static Symbol ParseOptionTickerOSI(string ticker)
        {
            var underlying = ticker.Substring(0, 6).Trim();
            var expiration = DateTime.ParseExact(ticker.Substring(6, 6), DateFormat.SixCharacter, null);
            OptionRight right;
            if (ticker[12] == 'C') right = OptionRight.Call;
            else if (ticker[12] == 'P') right = OptionRight.Put;
            else throw new FormatException($"Expected 12th character to be 'C' or 'P' for OptionRight: {ticker}");
            var strike = Parse.Decimal(ticker.Substring(13, 8)) / 1000m;
            var underlyingSid = SecurityIdentifier.GenerateEquity(underlying, Market.USA);
            var sid = SecurityIdentifier.GenerateOption(expiration, underlyingSid, Market.USA, strike, right, OptionStyle.American);
            return new Symbol(sid, ticker, new Symbol(underlyingSid, underlying));
        }

        /// <summary>
        /// Function returns option contract parameters (underlying name, expiration date, strike, right) from IQFeed option ticker
        /// Symbology details: http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&amp;displayaction=support%C2%A7ion=guide&amp;web=iqfeed&amp;guide=options&amp;web=IQFeed&amp;type=stock
        /// </summary>
        /// <param name="ticker">IQFeed option ticker</param>
        /// <returns>Results containing 1) underlying name, 2) option right, 3) option strike 4) expiration date</returns>
        public static OptionTickerProperties ParseOptionTickerIQFeed(string ticker)
        {
            // This table describes IQFeed option symbology
            var symbology = new Dictionary<string, Tuple<int, OptionRight>>
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

            var letterRange = symbology.Keys
                            .Select(x => x[0])
                            .ToArray();
            var optionTypeDelimiter = ticker.LastIndexOfAny(letterRange);
            var strikePriceString = ticker.Substring(optionTypeDelimiter + 1, ticker.Length - optionTypeDelimiter - 1);

            var lookupResult = symbology[ticker[optionTypeDelimiter].ToStringInvariant()];
            var month = lookupResult.Item1;
            var optionRight = lookupResult.Item2;

            var dayString = ticker.Substring(optionTypeDelimiter - 2, 2);
            var yearString = ticker.Substring(optionTypeDelimiter - 4, 2);
            var underlying = ticker.Substring(0, optionTypeDelimiter - 4);

            // if we cannot parse strike price, we ignore this contract, but log the information.
            decimal strikePrice;
            if (!Decimal.TryParse(strikePriceString, out strikePrice))
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


        private static IReadOnlyDictionary<string, int> _futuresMonthCodeLookup = new Dictionary<string, int>
                        {
                            { "F", 1 },
                            { "G", 2 },
                            { "H", 3 },
                            { "J", 4 },
                            { "K", 5 },
                            { "M", 6 },
                            { "N", 7 },
                            { "Q", 8 },
                            { "U", 9 },
                            { "V", 10 },
                            { "X", 11 },
                            { "Z", 12 }
                        };

        private static IReadOnlyDictionary<int, string> _futuresMonthLookup = _futuresMonthCodeLookup.ToDictionary(kv => kv.Value, kv => kv.Key);
    }
}
