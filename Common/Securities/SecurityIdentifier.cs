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
using System.Numerics;
using System.Text.RegularExpressions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Defines a unique identifier for securities
    /// </summary>
    /// <remarks>
    /// The SecurityIdentifier contains information about a specific security.
    /// This includes the symbol and other data specific to the SecurityType.
    /// The symbol is limited to 12 characters
    /// </remarks>
    public struct SecurityIdentifier : IEquatable<SecurityIdentifier>
    {
        #region Empty, Invalid

        /// <summary>
        /// Gets an instance of <see cref="SecurityIdentifier"/> that is empty, that is, one with no symbol specified
        /// </summary>
        public static readonly SecurityIdentifier Empty = new SecurityIdentifier(0, 0);

        /// <summary>
        /// Gets an instance of <see cref="SecurityIdentifier"/> that is invalid. The symbol will have a value of "3W5E11264SGSF"
        /// which is a base36 encoded form of <see cref="ulong.MaxValue"/>
        /// </summary>
        public static readonly SecurityIdentifier Invalid = new SecurityIdentifier(ulong.MaxValue, ulong.MaxValue);

        #endregion

        #region Scales, Widths and Market Maps

        // these values define the structure of the 'otherData'
        // the constant width fields are used via modulus, so the width is the number of zeros specified,
        // {put/call:1}{oa-date:5}{style:1}{strike:6}{strike-scale:2}{market:3}{security-type:2}

        private const ulong SecurityTypeWidth = 100;
        private const ulong SecurityTypeOffset = 1;

        private const ulong MarketWidth = 1000;
        private const ulong MarketOffset = SecurityTypeOffset * SecurityTypeWidth;

        private const int StrikeDefaultScale = 4;
        private static readonly ulong StrikeDefaultScaleExpanded = Pow(10, StrikeDefaultScale);

        private const ulong StrikeScaleWidth = 100;
        private const ulong StrikeScaleOffset = MarketOffset * MarketWidth;

        private const ulong StrikeWidth = 1000000;
        private const ulong StrikeOffset = StrikeScaleOffset * StrikeScaleWidth;

        private const ulong OptionStyleWidth = 10;
        private const ulong OptionStyleOffset = StrikeOffset * StrikeWidth;

        private const ulong DaysWidth = 100000;
        private const ulong DaysOffset = OptionStyleOffset * OptionStyleWidth;

        private const ulong PutCallOffset = DaysOffset * DaysWidth;
        private const ulong PutCallWidth = 10;

        private static readonly Dictionary<string, ulong> Markets = new Dictionary<string, ulong>
        {
            {"empty", 0},
            {"usa", 1},
            {"fxcm", 2},
            {"oanda", 3}
        };

        private static readonly Dictionary<ulong, string> ReverseMarkets = new Dictionary<ulong, string>
        {
            {0, "empty"},
            {1, "usa"},
            {2, "fxcm"},
            {3, "oanda"}
        };

        #endregion

        #region Member variables

        private readonly ulong _symbolData;
        private readonly ulong _otherData;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the date component of this identifier. For equities this
        /// is the first date the security traded. Technically speaking,
        /// in LEAN, this is the first date mentioned in the map_files.
        /// For options this is the expiry date. For futures this is the
        /// settlement date. For forex and cfds this property will throw an
        /// exception as the field is not specified.
        /// </summary>
        public DateTime Date
        {
            get
            {
                var stype = SecurityType;
                switch (stype)
                {
                    case SecurityType.Equity:
                    case SecurityType.Option:
                    case SecurityType.Future:
                        var oadate = ExtractFromOtherData(DaysOffset, DaysWidth);
                        return DateTime.FromOADate(oadate);
                    default:
                        throw new InvalidOperationException("Date is only defined for SecurityType.Equity, SecurityType.Option and SecurityType.Future");
                }
            }
        }

        /// <summary>
        /// Gets the original symbol used to generate this security identifier.
        /// For equities, by convention this is the first ticker symbol for which
        /// the security traded
        /// </summary>
        public string Symbol
        {
            get { return EncodeBase36(_symbolData); }
        }

        /// <summary>
        /// Gets the market component of this security identifier. If located in the
        /// internal mappings, the full string is returned. If the value is unknown,
        /// the integer value is returned as a string.
        /// </summary>
        public string Market
        {
            get
            {
                string market;
                var marketIndex = ExtractFromOtherData(MarketOffset, MarketWidth);
                if (ReverseMarkets.TryGetValue(marketIndex, out market))
                {
                    return market;
                }

                // if we couldn't find it, send back the numeric representation
                return marketIndex.ToString();
            }
        }

        /// <summary>
        /// Gets the security type component of this security identifier.
        /// </summary>
        public SecurityType SecurityType
        {
            get { return (SecurityType)ExtractFromOtherData(SecurityTypeOffset, SecurityTypeWidth); }
        }

        /// <summary>
        /// Gets the option strike price. This only applies to SecurityType.Option
        /// and will thrown anexception if accessed otherwse.
        /// </summary>
        public decimal StrikePrice
        {
            get
            {
                if (SecurityType != SecurityType.Option)
                {
                    throw new InvalidOperationException("OptionType is only defined for SecurityType.Option");
                }
                var scale = ExtractFromOtherData(StrikeScaleOffset, StrikeScaleWidth);
                var unscaled = ExtractFromOtherData(StrikeOffset, StrikeWidth);
                var pow = Math.Pow(10, (int)scale - StrikeDefaultScale);
                return unscaled * (decimal)pow;
            }
        }

        /// <summary>
        /// Gets the option type component of this security identifier. This
        /// only applies to SecurityType.Open and will throw an exception if
        /// accessed otherwise.
        /// </summary>
        public OptionRight OptionRight
        {
            get
            {
                if (SecurityType != SecurityType.Option)
                {
                    throw new InvalidOperationException("OptionRight is only defined for SecurityType.Option");
                }
                return (OptionRight)ExtractFromOtherData(PutCallOffset, PutCallWidth);
            }
        }

        /// <summary>
        /// Gets the option style component of this security identifier. This
        /// only applies to SecurityType.Open and will throw an exception if
        /// accessed otherwise.
        /// </summary>
        public OptionStyle OptionStyle
        {
            get
            {
                if (SecurityType != SecurityType.Option)
                {
                    throw new InvalidOperationException("OptionStyle is only defined for SecurityType.Option");
                }
                return (OptionStyle)(ExtractFromOtherData(OptionStyleOffset, OptionStyleWidth));
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityIdentifier"/> class
        /// </summary>
        /// <param name="symbolData">The base36 string encoded as a long using alpha [0-9A-Z]</param>
        /// <param name="otherData">Other data defining properties of the symbol including market,
        /// security type, listing or expiry date, strike/call/put/style for options, ect...</param>
        public SecurityIdentifier(ulong symbolData, ulong otherData)
        {
            _symbolData = symbolData;
            _otherData = otherData;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityIdentifier"/> class
        /// </summary>
        /// <param name="securityIdentifierString">The 40 digit number that represents a security identifier</param>
        public SecurityIdentifier(string securityIdentifierString)
        {
            Exception exception;
            if (!TryParseComponents(securityIdentifierString, out exception, out _otherData, out _symbolData))
            {
                throw exception;
            }
        }

        #endregion

        #region Generate and AddMarket

        /// <summary>
        /// Adds the specified market to the map of available markets with the specified identifier.
        /// </summary>
        /// <param name="market">The market string to add</param>
        /// <param name="identifier">The identifier for the market, this value must be positive and less than 100</param>
        public static void AddMarket(string market, ushort identifier)
        {
            if (identifier >= MarketWidth)
            {
                throw new ArgumentOutOfRangeException("identifier", "The market identifier is limited to values less than 100.");
            }

            market = market.ToLower();

            ulong marketIdentifier;
            if (Markets.TryGetValue(market, out marketIdentifier) && identifier != marketIdentifier)
            {
                throw new ArgumentException("Attempted to add an already added market with a different identifier. Market: " + market);
            }

            string existingMarket;
            if (ReverseMarkets.TryGetValue(identifier, out existingMarket))
            {
                throw new ArgumentException("Attempted to add a market identifier that is already in use. New Market: " + market + " Existing Market: " + existingMarket);
            }

            // update our maps
            Markets[market] = identifier;
            ReverseMarkets[identifier] = market;
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for an option
        /// </summary>
        /// <param name="expiry">The date the option expires</param>
        /// <param name="underlying">The underlying security's symbol</param>
        /// <param name="market">The market</param>
        /// <param name="strike">The strike price</param>
        /// <param name="optionRight">The option type, call or put</param>
        /// <param name="optionStyle">The option style, American or European</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified data</returns>
        public static SecurityIdentifier GenerateOption(DateTime expiry,
            string underlying,
            string market,
            decimal strike,
            OptionRight optionRight,
            OptionStyle optionStyle)
        {
            return Generate(expiry, underlying, SecurityType.Option, market, strike, optionRight, optionStyle);
        }

        public static SecurityIdentifier GenerateEquity(DateTime date, string symbol, string market)
        {
            return Generate(date, symbol, SecurityType.Equity, market);
        }

        public static SecurityIdentifier GenerateBase(string symbol, string market)
        {
            return Generate(DateTime.FromOADate(0), symbol, SecurityType.Base, market);
        }

        public static SecurityIdentifier GenerateForex(string symbol, string market)
        {
            return Generate(DateTime.FromOADate(0), symbol, SecurityType.Forex, market);
        }

        private static SecurityIdentifier Generate(DateTime date,
            string symbol,
            SecurityType securityType,
            string market,
            decimal strike = 0,
            OptionRight optionRight = 0,
            OptionStyle optionStyle = 0)
        {
            if ((ulong)securityType >= SecurityTypeWidth || securityType < 0)
            {
                throw new ArgumentOutOfRangeException("securityType", "securityType must be between 0 and 99");
            }
            if ((int)optionRight > 1 || optionRight < 0)
            {
                throw new ArgumentOutOfRangeException("optionRight", "optionType must be either 0 or 1");
            }

            // normalize input strings
            market = market.ToLower();
            symbol = symbol.ToUpper();
            // remove all non alpha numeric characters
            symbol = Regex.Replace(symbol, @"[^A-Z0-9]+", "");

            ulong marketIdentifier;
            if (!Markets.TryGetValue(market, out marketIdentifier))
            {
                throw new ArgumentOutOfRangeException("market", "The specified market wasn't found in the lookup. Requested: " + market);
            }

            var days = ((ulong)date.ToOADate()) * DaysOffset;
            marketIdentifier = marketIdentifier * MarketOffset;
            ulong strikeScale;
            var strk = NormalizeStrike(strike, out strikeScale) * StrikeOffset;
            strikeScale *= StrikeScaleOffset;
            var style = ((ulong)optionStyle) * OptionStyleOffset;
            var putcall = (ulong)(optionRight) * PutCallOffset;

            if (symbol.Length > 12)
            {
                throw new ArgumentException("Symbol lengths are limited to 12 characters for encoding.");
            }

            var sid = DecodeBase36(symbol);
            var otherData = putcall + days + style + strk + strikeScale + marketIdentifier + (ulong)securityType;

            return new SecurityIdentifier(sid, otherData);
        }

        /// <summary>
        /// Converts an upper case alpha numeric string into a long
        /// </summary>
        public static ulong DecodeBase36(string symbol)
        {
            int pos = 0;
            ulong result = 0;
            for (int i = symbol.Length - 1; i > -1; i--)
            {
                var c = symbol[i];

                // assumes alpha numeric upper case only strings
                var value = (uint)(c <= 57
                    ? c - '0'
                    : c - 'A' + 10);

                result += value * Pow(36, pos++);
            }
            return result;
        }

        public static string EncodeBase36(ulong data)
        {
            var stack = new Stack<char>();
            while (data != 0)
            {
                var value = data % 36;
                var c = value < 10
                    ? (char)(value + '0')
                    : (char)(value - 10 + 'A');

                stack.Push(c);
                data /= 36;
            }
            return new string(stack.ToArray());
        }

        private static ulong NormalizeStrike(decimal strike, out ulong scale)
        {
            var str = strike;

            if (strike == 0)
            {
                scale = 0;
                return 0;
            }

            // convert strike to default scaling, this keeps the scale always positive
            strike *= StrikeDefaultScaleExpanded;

            scale = 0;
            while (strike % 10 == 0)
            {
                strike /= 10;
                scale++;
            }

            if (strike >= 1000000)
            {
                throw new ArgumentException("The specified strike price's precision is too high: " + str);
            }

            return (ulong)strike;
        }

        private static ulong Pow(uint x, int pow)
        {
            return (ulong)BigInteger.Pow(x, pow);
            //http://stackoverflow.com/a/383596/1582922
            ulong result = 1;
            while (pow != 0)
            {
                if ((pow & 1) == 1)
                    result *= x;
                x *= x;
                pow >>= 1;
            }
            return result;
        }

        #endregion

        #region Parsing routines

        /// <summary>
        /// Parses the specified string into a <see cref="SecurityIdentifier"/>
        /// The string must be a 40 digit number. The first 20 digits must be parseable
        /// to a 64 bit unsigned integer and contain ancillary data about the security.
        /// The second 20 digits must also be parseable as a 64 bit unsigned integer and
        /// contain the symbol encoded from base36, this provides for 12 alpha numeric case
        /// insensitive characters.
        /// </summary>
        /// <param name="value">The string value to be parsed</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> instance if the <paramref name="value"/> is able to be parsed.</returns>
        /// <exception cref="FormatException">This exception is thrown if the string's length is not exactly 40 characters, or
        /// if the components are unable to be parsed as 64 bit unsigned integers</exception>
        public static SecurityIdentifier Parse(string value)
        {
            Exception exception;
            SecurityIdentifier identifier;
            if (!TryParse(value, out identifier, out exception))
            {
                throw exception;
            }

            return identifier;
        }

        /// <summary>
        /// Attempts to parse the specified <see cref="value"/> as a <see cref="SecurityIdentifier"/>.
        /// </summary>
        /// <param name="value">The string value to be parsed</param>
        /// <param name="identifier">The result of parsing, when this function returns true, <paramref name="identifier"/>
        /// was properly created and reflects the input string, when this function returns false <paramref name="identifier"/>
        /// will equal <see cref="Invalid"/></param>
        /// <returns>True on success, otherwise false</returns>
        public static bool TryParse(string value, out SecurityIdentifier identifier)
        {
            Exception exception;
            return TryParse(value, out identifier, out exception);
        }

        /// <summary>
        /// Helper method impl to be used by parse and tryparse
        /// </summary>
        private static bool TryParse(string value, out SecurityIdentifier identifier, out Exception exception)
        {
            ulong od;
            ulong sd;
            identifier = Invalid;
            if (!TryParseComponents(value, out exception, out od, out sd))
            {
                return false;
            }

            identifier = new SecurityIdentifier(sd, od);
            return true;
        }

        /// <summary>
        /// Parses the string into its component ulong pieces
        /// </summary>
        private static bool TryParseComponents(string value, out Exception exception, out ulong od, out ulong sd)
        {
            od = sd = ulong.MaxValue;
            exception = null;

            if (value.StartsWith(Invalid.Symbol))
            {
                sd = Invalid._symbolData;
                od = Invalid._otherData;
                return true;
            }

            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new FormatException("The string must be splittable on space into two parts.");
            }

            var symbolData = parts[0];
            var otherData = parts[1];

            sd = DecodeBase36(symbolData);
            od = DecodeBase36(otherData);
            return true;
        }

        /// <summary>
        /// Extracts the embedded value from _otherData
        /// </summary>
        private ulong ExtractFromOtherData(ulong offset, ulong width)
        {
            return (_otherData / offset) % width;
        }

        #endregion

        #region Equality members and ToString

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(SecurityIdentifier other)
        {
            return _symbolData == other._symbolData && _otherData == other._otherData;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;
            return Equals((SecurityIdentifier)obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked { return (_symbolData.GetHashCode()*397) ^ _otherData.GetHashCode(); }
        }

        /// <summary>
        /// Override equals operator
        /// </summary>
        public static bool operator ==(SecurityIdentifier left, SecurityIdentifier right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Override not equals operator
        /// </summary>
        public static bool operator !=(SecurityIdentifier left, SecurityIdentifier right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var od = EncodeBase36(_otherData);
            var sd = EncodeBase36(_symbolData);
            return sd + ' ' + od;
        }

        #endregion
    }
}
