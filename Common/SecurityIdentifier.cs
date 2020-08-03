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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using ProtoBuf;
using QuantConnect.Configuration;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Defines a unique identifier for securities
    /// </summary>
    /// <remarks>
    /// The SecurityIdentifier contains information about a specific security.
    /// This includes the symbol and other data specific to the SecurityType.
    /// The symbol is limited to 12 characters
    /// </remarks>
    [JsonConverter(typeof(SecurityIdentifierJsonConverter))]
    [ProtoContract(SkipConstructor = true)]
    public class SecurityIdentifier : IEquatable<SecurityIdentifier>
    {
        #region Empty, DefaultDate Fields

        private static readonly ConcurrentDictionary<string, SecurityIdentifier> SecurityIdentifierCache
            = new ConcurrentDictionary<string, SecurityIdentifier>();
        private static readonly string MapFileProviderTypeName = Config.Get("map-file-provider", "LocalDiskMapFileProvider");
        private static readonly char[] InvalidCharacters = {'|', ' '};
        private static readonly Lazy<IMapFileProvider> MapFileProvider = new Lazy<IMapFileProvider>(
            () => Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(MapFileProviderTypeName)
        );

        /// <summary>
        /// Gets an instance of <see cref="SecurityIdentifier"/> that is empty, that is, one with no symbol specified
        /// </summary>
        public static readonly SecurityIdentifier Empty = new SecurityIdentifier(string.Empty, 0);

        /// <summary>
        /// Gets an instance of <see cref="SecurityIdentifier"/> that is explicitly no symbol
        /// </summary>
        public static readonly SecurityIdentifier None = new SecurityIdentifier("NONE", 0);

        /// <summary>
        /// Gets the date to be used when it does not apply.
        /// </summary>
        public static readonly DateTime DefaultDate = DateTime.FromOADate(0);

        /// <summary>
        /// Gets the set of invalids symbol characters
        /// </summary>
        public static readonly HashSet<char> InvalidSymbolCharacters = new HashSet<char>(InvalidCharacters);

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

        #endregion

        #region Member variables

        [ProtoMember(1)]
        private string _symbol;
        [ProtoMember(2)]
        private ulong _properties;
        [ProtoMember(3)]
        private SecurityIdentifier _underlying;
        private bool _hashCodeSet;
        private int _hashCode;
        private decimal? _strikePrice;
        private OptionStyle? _optionStyle;
        private OptionRight? _optionRight;
        private DateTime? _date;
        private string _stringRep;
        private string _market;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether or not this <see cref="SecurityIdentifier"/> is a derivative,
        /// that is, it has a valid <see cref="Underlying"/> property
        /// </summary>
        public bool HasUnderlying
        {
            get { return _underlying != null; }
        }

        /// <summary>
        /// Gets the underlying security identifier for this security identifier. When there is
        /// no underlying, this property will return a value of <see cref="Empty"/>.
        /// </summary>
        public SecurityIdentifier Underlying
        {
            get
            {
                if (_underlying == null)
                {
                    throw new InvalidOperationException("No underlying specified for this identifier. Check that HasUnderlying is true before accessing the Underlying property.");
                }
                return _underlying;
            }
        }

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
                try
                {
                    return _date.Value;
                }
                catch (InvalidOperationException)
                {
                    switch (SecurityType)
                    {
                        case SecurityType.Base:
                        case SecurityType.Equity:
                        case SecurityType.Option:
                        case SecurityType.Future:
                            var oadate = ExtractFromProperties(DaysOffset, DaysWidth);
                            _date = DateTime.FromOADate(oadate);
                            return _date.Value;
                        default:
                            throw new InvalidOperationException("Date is only defined for SecurityType.Equity, SecurityType.Option, SecurityType.Future, and SecurityType.Base");
                    }
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
            get { return _symbol; }
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
                if (_market == null)
                {
                    var marketCode = ExtractFromProperties(MarketOffset, MarketWidth);
                    var market = QuantConnect.Market.Decode((int)marketCode);
                    // if we couldn't find it, send back the numeric representation
                    _market = market ?? marketCode.ToStringInvariant();
                }
                return _market;
            }
        }

        /// <summary>
        /// Gets the security type component of this security identifier.
        /// </summary>
        [ProtoMember(4)]
        public SecurityType SecurityType { get; }

        /// <summary>
        /// Gets the option strike price. This only applies to SecurityType.Option
        /// and will thrown anexception if accessed otherwse.
        /// </summary>
        public decimal StrikePrice
        {
            get
            {
                try
                {
                    // will throw 'InvalidOperationException' if not set
                    return _strikePrice.Value;
                }
                catch (InvalidOperationException)
                {
                    if (SecurityType != SecurityType.Option)
                    {
                        throw new InvalidOperationException("OptionType is only defined for SecurityType.Option");
                    }

                    // performance: lets calculate strike price once
                    var scale = ExtractFromProperties(StrikeScaleOffset, StrikeScaleWidth);
                    var unscaled = ExtractFromProperties(StrikeOffset, StrikeWidth);
                    var pow = Math.Pow(10, (int)scale - StrikeDefaultScale);
                    _strikePrice = unscaled * (decimal)pow;

                    return _strikePrice.Value;
                }
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
                try
                {
                    // will throw 'InvalidOperationException' if not set
                    return _optionRight.Value;
                }
                catch (InvalidOperationException)
                {
                    if (SecurityType != SecurityType.Option)
                    {
                        throw new InvalidOperationException("OptionRight is only defined for SecurityType.Option");
                    }
                    _optionRight = (OptionRight)ExtractFromProperties(PutCallOffset, PutCallWidth);
                    return _optionRight.Value;
                }
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
                try
                {
                    // will throw 'InvalidOperationException' if not set
                    return _optionStyle.Value;
                }
                catch (InvalidOperationException)
                {
                    if (SecurityType != SecurityType.Option)
                    {
                        throw new InvalidOperationException("OptionStyle is only defined for SecurityType.Option");
                    }

                    _optionStyle = (OptionStyle)(ExtractFromProperties(OptionStyleOffset, OptionStyleWidth));
                    return _optionStyle.Value;
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityIdentifier"/> class
        /// </summary>
        /// <param name="symbol">The base36 string encoded as a long using alpha [0-9A-Z]</param>
        /// <param name="properties">Other data defining properties of the symbol including market,
        /// security type, listing or expiry date, strike/call/put/style for options, ect...</param>
        public SecurityIdentifier(string symbol, ulong properties)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol), "SecurityIdentifier requires a non-null string 'symbol'");
            }
            if (symbol.IndexOfAny(InvalidCharacters) != -1)
            {
                throw new ArgumentException("symbol must not contain the characters '|' or ' '.", nameof(symbol));
            }
            _symbol = symbol;
            _properties = properties;
            _underlying = null;
            _strikePrice = null;
            _optionStyle = null;
            _optionRight = null;
            _date = null;
            SecurityType = (SecurityType)ExtractFromProperties(SecurityTypeOffset, SecurityTypeWidth, properties);
            if (!SecurityType.IsValid())
            {
                throw new ArgumentException($"The provided properties do not match with a valid {nameof(SecurityType)}", "properties");
            }
            _hashCode = unchecked (symbol.GetHashCode() * 397) ^ properties.GetHashCode();
            _hashCodeSet = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityIdentifier"/> class
        /// </summary>
        /// <param name="symbol">The base36 string encoded as a long using alpha [0-9A-Z]</param>
        /// <param name="properties">Other data defining properties of the symbol including market,
        /// security type, listing or expiry date, strike/call/put/style for options, ect...</param>
        /// <param name="underlying">Specifies a <see cref="SecurityIdentifier"/> that represents the underlying security</param>
        public SecurityIdentifier(string symbol, ulong properties, SecurityIdentifier underlying)
            : this(symbol, properties)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol), "SecurityIdentifier requires a non-null string 'symbol'");
            }
            _symbol = symbol;
            _properties = properties;
            // performance: directly call Equals(SecurityIdentifier other), shortcuts Equals(object other)
            if (!underlying.Equals(Empty))
            {
                _underlying = underlying;
            }
        }

        #endregion

        #region AddMarket, GetMarketCode, and Generate

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for an option
        /// </summary>
        /// <param name="expiry">The date the option expires</param>
        /// <param name="underlying">The underlying security's symbol</param>
        /// <param name="market">The market</param>
        /// <param name="strike">The strike price</param>
        /// <param name="optionRight">The option type, call or put</param>
        /// <param name="optionStyle">The option style, American or European</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified option security</returns>
        public static SecurityIdentifier GenerateOption(DateTime expiry,
            SecurityIdentifier underlying,
            string market,
            decimal strike,
            OptionRight optionRight,
            OptionStyle optionStyle)
        {
            return Generate(expiry, underlying.Symbol, SecurityType.Option, market, strike, optionRight, optionStyle, underlying);
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a future
        /// </summary>
        /// <param name="expiry">The date the future expires</param>
        /// <param name="symbol">The security's symbol</param>
        /// <param name="market">The market</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified futures security</returns>
        public static SecurityIdentifier GenerateFuture(DateTime expiry,
            string symbol,
            string market)
        {
            return Generate(expiry, symbol, SecurityType.Future, market);
        }

        /// <summary>
        /// Helper overload that will search the mapfiles to resolve the first date. This implementation
        /// uses the configured <see cref="IMapFileProvider"/> via the <see cref="Composer.Instance"/>
        /// </summary>
        /// <param name="symbol">The symbol as it is known today</param>
        /// <param name="market">The market</param>
        /// <param name="mapSymbol">Specifies if symbol should be mapped using map file provider</param>
        /// <param name="mapFileProvider">Specifies the IMapFileProvider to use for resolving symbols, specify null to load from Composer</param>
        /// <param name="mappingResolveDate">The date to use to resolve the map file. Default value is <see cref="DateTime.Today"/></param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified symbol today</returns>
        public static SecurityIdentifier GenerateEquity(string symbol, string market, bool mapSymbol = true, IMapFileProvider mapFileProvider = null, DateTime? mappingResolveDate = null)
        {
            var firstDate = DefaultDate;
            if (mapSymbol)
            {
                var firstTickerDate = GetFirstTickerAndDate(mapFileProvider ?? MapFileProvider.Value, symbol, market, mappingResolveDate: mappingResolveDate);
                firstDate = firstTickerDate.Item2;
                symbol = firstTickerDate.Item1;
            }

            return GenerateEquity(firstDate, symbol, market);
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for an equity
        /// </summary>
        /// <param name="date">The first date this security traded (in LEAN this is the first date in the map_file</param>
        /// <param name="symbol">The ticker symbol this security traded under on the <paramref name="date"/></param>
        /// <param name="market">The security's market</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified equity security</returns>
        public static SecurityIdentifier GenerateEquity(DateTime date, string symbol, string market)
        {
            return Generate(date, symbol, SecurityType.Equity, market);
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a <see cref="ConstituentsUniverseData"/>.
        /// Note that the symbol ticker is case sensitive here.
        /// </summary>
        /// <param name="symbol">The ticker to use for this constituent identifier</param>
        /// <param name="securityType">The security type of this constituent universe</param>
        /// <param name="market">The security's market</param>
        /// <remarks>This method is special in the sense that it does not force the Symbol to be upper
        /// which is required to determine the source file of the constituent
        /// <see cref="ConstituentsUniverseData.GetSource(Data.SubscriptionDataConfig,DateTime,bool)"/></remarks>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified constituent universe</returns>
        public static SecurityIdentifier GenerateConstituentIdentifier(string symbol, SecurityType securityType, string market)
        {
            return Generate(DefaultDate, symbol, securityType, market, forceSymbolToUpper: false);
        }

        /// <summary>
        /// Generates the <see cref="Symbol"/> property for <see cref="QuantConnect.SecurityType.Base"/> security identifiers
        /// </summary>
        /// <param name="dataType">The base data custom data type if namespacing is required, null otherwise</param>
        /// <param name="symbol">The ticker symbol</param>
        /// <returns>The value used for the security identifier's <see cref="Symbol"/></returns>
        public static string GenerateBaseSymbol(Type dataType, string symbol)
        {
            if (dataType == null)
            {
                return symbol;
            }

            return $"{symbol.ToUpperInvariant()}.{dataType.Name}";
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a custom security with the option of providing the first date
        /// </summary>
        /// <param name="dataType">The custom data type</param>
        /// <param name="symbol">The ticker symbol of this security</param>
        /// <param name="market">The security's market</param>
        /// <param name="mapSymbol">Whether or not we should map this symbol</param>
        /// <param name="date">First date that the security traded on</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified base security</returns>
        public static SecurityIdentifier GenerateBase(Type dataType, string symbol, string market, bool mapSymbol = false, DateTime? date = null)
        {
            var firstDate = date ?? DefaultDate;

            if (mapSymbol)
            {
                var firstTickerDate = GetFirstTickerAndDate(MapFileProvider.Value, symbol, market);
                firstDate = firstTickerDate.Item2;
                symbol = firstTickerDate.Item1;
            }

            return Generate(
                firstDate,
                GenerateBaseSymbol(dataType, symbol),
                SecurityType.Base,
                market,
                forceSymbolToUpper: false
            );
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a forex pair
        /// </summary>
        /// <param name="symbol">The currency pair in the format similar to: 'EURUSD'</param>
        /// <param name="market">The security's market</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified forex pair</returns>
        public static SecurityIdentifier GenerateForex(string symbol, string market)
        {
            return Generate(DefaultDate, symbol, SecurityType.Forex, market);
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a Crypto pair
        /// </summary>
        /// <param name="symbol">The currency pair in the format similar to: 'EURUSD'</param>
        /// <param name="market">The security's market</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified Crypto pair</returns>
        public static SecurityIdentifier GenerateCrypto(string symbol, string market)
        {
            return Generate(DefaultDate, symbol, SecurityType.Crypto, market);
        }

        /// <summary>
        /// Generates a new <see cref="SecurityIdentifier"/> for a CFD security
        /// </summary>
        /// <param name="symbol">The CFD contract symbol</param>
        /// <param name="market">The security's market</param>
        /// <returns>A new <see cref="SecurityIdentifier"/> representing the specified CFD security</returns>
        public static SecurityIdentifier GenerateCfd(string symbol, string market)
        {
            return Generate(DefaultDate, symbol, SecurityType.Cfd, market);
        }

        /// <summary>
        /// Generic generate method. This method should be used carefully as some parameters are not required and
        /// some parameters mean different things for different security types
        /// </summary>
        private static SecurityIdentifier Generate(DateTime date,
            string symbol,
            SecurityType securityType,
            string market,
            decimal strike = 0,
            OptionRight optionRight = 0,
            OptionStyle optionStyle = 0,
            SecurityIdentifier underlying = null,
            bool forceSymbolToUpper = true)
        {
            if ((ulong)securityType >= SecurityTypeWidth || securityType < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(securityType), "securityType must be between 0 and 99");
            }
            if ((int)optionRight > 1 || optionRight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(optionRight), "optionType must be either 0 or 1");
            }

            // normalize input strings
            market = market.ToLowerInvariant();
            symbol = forceSymbolToUpper ? symbol.LazyToUpper() : symbol;

            var marketIdentifier = QuantConnect.Market.Encode(market);
            if (!marketIdentifier.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(market), "The specified market wasn't found in the  markets lookup. " +
                    $"Requested: {market}. You can add markets by calling QuantConnect.Market.AddMarket(string,ushort)"
                );
            }

            var days = (ulong)date.ToOADate() * DaysOffset;
            var marketCode = (ulong)marketIdentifier * MarketOffset;

            ulong strikeScale;
            var strk = NormalizeStrike(strike, out strikeScale) * StrikeOffset;
            strikeScale *= StrikeScaleOffset;
            var style = (ulong)optionStyle * OptionStyleOffset;
            var putcall = (ulong)optionRight * PutCallOffset;

            var otherData = putcall + days + style + strk + strikeScale + marketCode + (ulong)securityType;

            var result = new SecurityIdentifier(symbol, otherData, underlying ?? Empty);

            // we already have these so lets set them
            switch (securityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                case SecurityType.Future:
                    result._date = date;
                    break;
                case SecurityType.Option:
                    result._date = date;
                    result._strikePrice = strike;
                    result._optionRight = optionRight;
                    result._optionStyle = optionStyle;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Resolves the first ticker/date of the security represented by <paramref name="tickerToday"/>
        /// </summary>
        /// <param name="mapFileProvider">The IMapFileProvider instance used for resolving map files</param>
        /// <param name="tickerToday">The security's ticker as it trades today</param>
        /// <param name="market">The market the security exists in</param>
        /// <param name="mappingResolveDate">The date to use to resolve the map file. Default value is <see cref="DateTime.Today"/></param>
        /// <returns>The security's first ticker/date if mapping data available, otherwise, the provided ticker and DefaultDate are returned</returns>
        private static Tuple<string, DateTime> GetFirstTickerAndDate(IMapFileProvider mapFileProvider, string tickerToday, string market, DateTime? mappingResolveDate = null)
        {
            var resolver = mapFileProvider.Get(market);
            var mapFile = resolver.ResolveMapFile(tickerToday, mappingResolveDate ?? DateTime.Today);

            // if we have mapping data, use the first ticker/date from there, otherwise use provided ticker and DefaultDate
            return mapFile.Any()
                ? Tuple.Create(mapFile.FirstTicker, mapFile.FirstDate)
                : Tuple.Create(tickerToday, DefaultDate);
        }

        /// <summary>
        /// Converts an upper case alpha numeric string into a long
        /// </summary>
        private static ulong DecodeBase36(string symbol)
        {
            var result = 0ul;
            var baseValue = 1ul;
            for (var i = symbol.Length - 1; i > -1; i--)
            {
                var c = symbol[i];

                // assumes alpha numeric upper case only strings
                var value = (uint)(c <= 57
                    ? c - '0'
                    : c - 'A' + 10);

                result += baseValue * value;
                baseValue *= 36;
            }

            return result;
        }

        /// <summary>
        /// Converts a long to an uppercase alpha numeric string
        /// </summary>
        private static string EncodeBase36(ulong data)
        {
            var stack = new Stack<char>(15);
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

        /// <summary>
        /// The strike is normalized into deci-cents and then a scale factor
        /// is also saved to bring it back to un-normalized
        /// </summary>
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
                throw new ArgumentException(Invariant($"The specified strike price\'s precision is too high: {str}"));
            }

            return (ulong)strike;
        }

        /// <summary>
        /// Accurately performs the integer exponentiation
        /// </summary>
        private static ulong Pow(uint x, int pow)
        {
            // don't use Math.Pow(double, double) due to precision issues
            return (ulong)BigInteger.Pow(x, pow);
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
        /// Attempts to parse the specified <see paramref="value"/> as a <see cref="SecurityIdentifier"/>.
        /// </summary>
        /// <param name="value">The string value to be parsed</param>
        /// <param name="identifier">The result of parsing, when this function returns true, <paramref name="identifier"/>
        /// was properly created and reflects the input string, when this function returns false <paramref name="identifier"/>
        /// will equal default(SecurityIdentifier)</param>
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
            if (!TryParseProperties(value, out exception, out identifier))
            {
                return false;
            }

            return true;
        }

        private static readonly char[] SplitSpace = {' '};

        /// <summary>
        /// Parses the string into its component ulong pieces
        /// </summary>
        private static bool TryParseProperties(string value, out Exception exception, out SecurityIdentifier identifier)
        {
            exception = null;

            if (string.IsNullOrWhiteSpace(value) || value == " 0")
            {
                identifier = Empty;
                return true;
            }

            // for performance, we first verify if we already have parsed this SecurityIdentifier
            if (SecurityIdentifierCache.TryGetValue(value, out identifier))
            {
                return true;
            }
            // after calling TryGetValue because if it failed it will set identifier to default
            identifier = Empty;

            try
            {
                var sids = value.Split('|');
                for (var i = sids.Length - 1; i > -1; i--)
                {
                    var current = sids[i];
                    var parts = current.Split(SplitSpace, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        exception = new FormatException("The string must be splittable on space into two parts.");
                        return false;
                    }

                    var symbol = parts[0];
                    var otherData = parts[1];
                    var props = DecodeBase36(otherData);

                    // toss the previous in as the underlying, if Empty, ignored by ctor
                    identifier = new SecurityIdentifier(symbol, props, identifier);
                }
            }
            catch (Exception error)
            {
                exception = error;
                Log.Error($"SecurityIdentifier.TryParseProperties(): Error parsing SecurityIdentifier: '{value}', Exception: {exception}");
                return false;
            }

            SecurityIdentifierCache.TryAdd(value, identifier);
            return true;
        }

        /// <summary>
        /// Extracts the embedded value from _otherData
        /// </summary>
        private ulong ExtractFromProperties(ulong offset, ulong width)
        {
            return ExtractFromProperties(offset, width, _properties);
        }

        /// <summary>
        /// Extracts the embedded value from _otherData
        /// </summary>
        /// <remarks>Static so it can be used in <see cref="_lazySecurityType"/> initialization</remarks>
        private static ulong ExtractFromProperties(ulong offset, ulong width, ulong properties)
        {
            return (properties / offset) % width;
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
            return ReferenceEquals(this, other) || _properties == other._properties
                && _symbol == other._symbol
                && _underlying == other._underlying;
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
            if (!_hashCodeSet)
            {
                _hashCode = unchecked(_symbol.GetHashCode() * 397) ^ _properties.GetHashCode();
                _hashCodeSet = true;
            }
            return _hashCode;
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
            if (_stringRep == null)
            {
                var props = EncodeBase36(_properties);
                props = props.Length == 0 ? "0" : props;
                _stringRep = HasUnderlying ? $"{_symbol} {props}|{_underlying}" : $"{_symbol} {props}";
            }
            return _stringRep;
        }

        #endregion
    }
}
