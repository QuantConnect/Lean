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
 *
*/

using System;
using ProtoBuf;
using Python.Runtime;
using Newtonsoft.Json;
using QuantConnect.Securities;
using QuantConnect.Python;

namespace QuantConnect
{
    /// <summary>
    /// Represents a unique security identifier. This is made of two components,
    /// the unique SID and the Value. The value is the current ticker symbol while
    /// the SID is constant over the life of a security
    /// </summary>
    [JsonConverter(typeof(SymbolJsonConverter))]
    [ProtoContract(SkipConstructor = true)]
    [PandasNonExpandable]
    public sealed class Symbol : IEquatable<Symbol>, IComparable
    {
        private static readonly Lazy<SecurityDefinitionSymbolResolver> _securityDefinitionSymbolResolver = new (() => SecurityDefinitionSymbolResolver.GetInstance());

        private Symbol _canonical;
        // for performance we register how we compare with empty
        private bool? _isEmpty;

        /// <summary>
        /// Represents an unassigned symbol. This is intended to be used as an
        /// uninitialized, default value
        /// </summary>
        public static readonly Symbol Empty = new Symbol(SecurityIdentifier.Empty, string.Empty);

        /// <summary>
        /// Represents no symbol. This is intended to be used when no symbol is explicitly intended
        /// </summary>
        public static readonly Symbol None = new Symbol(SecurityIdentifier.None, "NONE");

        /// <summary>
        /// Provides a convenience method for creating a Symbol for most security types.
        /// This method currently does not support Commodities
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="securityType">The security type of the ticker. If securityType == Option, then a canonical symbol is created</param>
        /// <param name="market">The market the ticker resides in</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <param name="baseDataType">Optional for <see cref="SecurityType.Base"/> and used for generating the base data SID</param>
        /// <returns>A new Symbol object for the specified ticker</returns>
        public static Symbol Create(string ticker, SecurityType securityType, string market, string alias = null, Type baseDataType = null)
        {
            SecurityIdentifier sid;

            switch (securityType)
            {
                case SecurityType.Base:
                    sid = SecurityIdentifier.GenerateBase(baseDataType, ticker, market);
                    break;

                case SecurityType.Equity:
                    sid = SecurityIdentifier.GenerateEquity(ticker, market);
                    break;

                case SecurityType.Forex:
                    sid = SecurityIdentifier.GenerateForex(ticker, market);
                    break;

                case SecurityType.Cfd:
                    sid = SecurityIdentifier.GenerateCfd(ticker, market);
                    break;

                case SecurityType.Index:
                    sid = SecurityIdentifier.GenerateIndex(ticker, market);
                    break;

                case SecurityType.Option:
                    return CreateOption(ticker, market, default, default, default, SecurityIdentifier.DefaultDate);

                case SecurityType.Future:
                    sid = SecurityIdentifier.GenerateFuture(SecurityIdentifier.DefaultDate, ticker, market);
                    break;

                case SecurityType.Crypto:
                    sid = SecurityIdentifier.GenerateCrypto(ticker, market);
                    break;

                case SecurityType.CryptoFuture:
                    sid = SecurityIdentifier.GenerateCryptoFuture(SecurityIdentifier.DefaultDate, ticker, market);
                    break;

                case SecurityType.IndexOption:
                    return CreateOption(
                        Create(ticker, SecurityType.Index, market),
                        market,
                        OptionStyle.European,
                        default,
                        default,
                        SecurityIdentifier.DefaultDate);

                case SecurityType.FutureOption:
                    throw new NotImplementedException(Messages.Symbol.InsufficientInformationToCreateFutureOptionSymbol);

                case SecurityType.Commodity:
                default:
                    throw new NotImplementedException(Messages.Symbol.SecurityTypeNotImplementedYet(securityType));
            }

            return new Symbol(sid, alias ?? ticker);
        }

        /// <summary>
        /// Creates a new Symbol for custom data. This method allows for the creation of a new Base Symbol
        /// using the first ticker and the first traded date from the provided underlying Symbol. This avoids
        /// the issue for mappable types, where the ticker is remapped supposing the provided ticker value is from today.
        /// See <see cref="SecurityIdentifier"/>'s private method GetFirstTickerAndDate.
        /// The provided symbol is also set to <see cref="Symbol.Underlying"/> so that it can be accessed using the custom data Symbol.
        /// This is useful for associating custom data Symbols to other asset classes so that it is possible to filter using custom data
        /// and place trades on the underlying asset based on the filtered custom data.
        /// </summary>
        /// <param name="baseType">Type of BaseData instance</param>
        /// <param name="underlying">Underlying symbol to set for the Base Symbol</param>
        /// <param name="market">Market</param>
        /// <returns>New non-mapped Base Symbol that contains an Underlying Symbol</returns>
        public static Symbol CreateBase(PyObject baseType, Symbol underlying, string market = null)
        {
            return CreateBase(baseType.CreateType(), underlying, market);
        }

        /// <summary>
        /// Creates a new Symbol for custom data. This method allows for the creation of a new Base Symbol
        /// using the first ticker and the first traded date from the provided underlying Symbol. This avoids
        /// the issue for mappable types, where the ticker is remapped supposing the provided ticker value is from today.
        /// See <see cref="SecurityIdentifier"/>'s private method GetFirstTickerAndDate.
        /// The provided symbol is also set to <see cref="Symbol.Underlying"/> so that it can be accessed using the custom data Symbol.
        /// This is useful for associating custom data Symbols to other asset classes so that it is possible to filter using custom data
        /// and place trades on the underlying asset based on the filtered custom data.
        /// </summary>
        /// <param name="baseType">Type of BaseData instance</param>
        /// <param name="underlying">Underlying symbol to set for the Base Symbol</param>
        /// <param name="market">Market</param>
        /// <returns>New non-mapped Base Symbol that contains an Underlying Symbol</returns>
        public static Symbol CreateBase(Type baseType, Symbol underlying, string market = null)
        {
            // The SID Date is only defined for the following security types: base, equity, future, option.
            // Default to SecurityIdentifier.DefaultDate if there's no matching SecurityType
            var firstDate = underlying.SecurityType == SecurityType.Equity ||
                underlying.SecurityType.IsOption() ||
                underlying.SecurityType == SecurityType.Future ||
                underlying.SecurityType == SecurityType.Base
                    ? underlying.ID.Date
                    : (DateTime?)null;

            var sid = SecurityIdentifier.GenerateBase(baseType, underlying.ID.Symbol, market ?? Market.USA, mapSymbol: false, date: firstDate);
            return new Symbol(sid, underlying.Value, underlying);
        }

        /// <summary>
        /// Provides a convenience method for creating an option Symbol.
        /// </summary>
        /// <param name="underlying">The underlying ticker</param>
        /// <param name="market">The market the underlying resides in</param>
        /// <param name="style">The option style (American, European, ect..)</param>
        /// <param name="right">The option right (Put/Call)</param>
        /// <param name="strike">The option strike price</param>
        /// <param name="expiry">The option expiry date</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <param name="mapSymbol">Specifies if symbol should be mapped using map file provider</param>
        /// <returns>A new Symbol object for the specified option contract</returns>
        public static Symbol CreateOption(string underlying, string market, OptionStyle style, OptionRight right, decimal strike, DateTime expiry, string alias = null, bool mapSymbol = true)
        {
            var underlyingSid = SecurityIdentifier.GenerateEquity(underlying, market, mapSymbol);
            var underlyingSymbol = new Symbol(underlyingSid, underlying);

            return CreateOption(underlyingSymbol, market, style, right, strike, expiry, alias);
        }

        /// <summary>
        /// Provides a convenience method for creating an option Symbol using SecurityIdentifier.
        /// </summary>
        /// <param name="underlyingSymbol">The underlying security symbol</param>
        /// <param name="market">The market the underlying resides in</param>
        /// <param name="style">The option style (American, European, ect..)</param>
        /// <param name="right">The option right (Put/Call)</param>
        /// <param name="strike">The option strike price</param>
        /// <param name="expiry">The option expiry date</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from diferent markets</param>
        /// <returns>A new Symbol object for the specified option contract</returns>
        public static Symbol CreateOption(Symbol underlyingSymbol, string market, OptionStyle style, OptionRight right, decimal strike, DateTime expiry, string alias = null)
        {
            return CreateOption(underlyingSymbol, null, market, style, right, strike, expiry, alias);
        }

        /// <summary>
        /// Provides a convenience method for creating an option Symbol using SecurityIdentifier.
        /// </summary>
        /// <param name="underlyingSymbol">The underlying security symbol</param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="market">The market the underlying resides in</param>
        /// <param name="style">The option style (American, European, ect..)</param>
        /// <param name="right">The option right (Put/Call)</param>
        /// <param name="strike">The option strike price</param>
        /// <param name="expiry">The option expiry date</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from diferent markets</param>
        /// <returns>A new Symbol object for the specified option contract</returns>
        public static Symbol CreateOption(Symbol underlyingSymbol, string targetOption, string market, OptionStyle style, OptionRight right, decimal strike, DateTime expiry, string alias = null)
        {
            var sid = SecurityIdentifier.GenerateOption(expiry, underlyingSymbol.ID, targetOption, market, strike, right, style);

            return new Symbol(sid, alias ?? GetAlias(sid, underlyingSymbol), underlyingSymbol);
        }

        /// <summary>
        /// Provides a convenience method for creating an option Symbol from its SecurityIdentifier and alias.
        /// </summary>
        /// <param name="sid">The option SID</param>
        /// <param name="value">The alias</param>
        /// <param name="underlying">Optional underlying symbol to use. If null, it will we created from the given option SID and value</param>
        /// <returns>A new Symbol object for the specified option</returns>
        public static Symbol CreateOption(SecurityIdentifier sid, string value, Symbol underlying = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!sid.SecurityType.IsOption())
            {
                throw new ArgumentException(Messages.Symbol.SidNotForOption(sid), nameof(value));
            }

            if (IsCanonical(sid))
            {
                return new Symbol(sid, value);
            }

            if (underlying == null)
            {
                SymbolRepresentation.TryDecomposeOptionTickerOSI(value, sid.SecurityType,
                    out var _, out var underlyingValue, out var _, out var _, out var _);
                underlying = new Symbol(sid.Underlying, underlyingValue);
            }
            else if (underlying.ID != sid.Underlying)
            {
                throw new ArgumentException(Messages.Symbol.UnderlyingSidDoesNotMatch(sid, underlying), nameof(underlying));
            }

            return new Symbol(sid, value, underlying);
        }

        /// <summary>
        /// Simple method to create the canonical option symbol for any given underlying symbol
        /// </summary>
        /// <param name="underlyingSymbol">Underlying of this option</param>
        /// <param name="market">Market for this option</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <returns>New Canonical Option</returns>
        public static Symbol CreateCanonicalOption(Symbol underlyingSymbol, string market = null, string alias = null)
        {
            return CreateCanonicalOption(underlyingSymbol, null, market, alias);
        }

        /// <summary>
        /// Simple method to create the canonical option symbol for any given underlying symbol
        /// </summary>
        /// <param name="underlyingSymbol">Underlying of this option</param>
        /// <param name="targetOption">The target option ticker. This is useful when the option ticker does not match the underlying, e.g. SPX index and the SPXW weekly option. If null is provided will use underlying</param>
        /// <param name="market">Market for this option</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <returns>New Canonical Option</returns>
        public static Symbol CreateCanonicalOption(Symbol underlyingSymbol, string targetOption, string market = null, string alias = null)
        {
            var optionType = GetOptionTypeFromUnderlying(underlyingSymbol);
            market ??= underlyingSymbol.ID.Market;

            return CreateOption(underlyingSymbol,
                targetOption,
                market,
                optionType.DefaultOptionStyle(),
                default,
                default,
                SecurityIdentifier.DefaultDate,
                alias);
        }


        /// <summary>
        /// Provides a convenience method for creating a future Symbol.
        /// </summary>
        /// <param name="ticker">The ticker</param>
        /// <param name="market">The market the future resides in</param>
        /// <param name="expiry">The future expiry date</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <returns>A new Symbol object for the specified future contract</returns>
        public static Symbol CreateFuture(string ticker, string market, DateTime expiry, string alias = null)
        {
            var sid = SecurityIdentifier.GenerateFuture(expiry, ticker, market);

            return new Symbol(sid, alias ?? GetAlias(sid));
        }

        /// <summary>
        /// Method returns true, if symbol is a derivative canonical symbol
        /// </summary>
        /// <returns>true, if symbol is a derivative canonical symbol</returns>
        public bool IsCanonical()
        {
            return IsCanonical(ID);
        }

        /// <summary>
        /// Get's the canonical representation of this symbol
        /// </summary>
        /// <remarks>This is useful for access and performance</remarks>
        public Symbol Canonical
        {
            get
            {
                if (_canonical != null)
                {
                    return _canonical;
                }

                _canonical = this;
                if (!IsCanonical())
                {
                    if (SecurityType.IsOption())
                    {
                        _canonical = CreateCanonicalOption(Underlying, ID.Symbol, ID.Market, null);
                    }
                    else if (SecurityType == SecurityType.Future)
                    {
                        _canonical = Create(ID.Symbol, SecurityType.Future, ID.Market);
                    }
                    else
                    {
                        throw new InvalidOperationException(Messages.Symbol.CanonicalNotDefined);
                    }
                }
                return _canonical;
            }
        }

        /// <summary>
        /// Determines whether the symbol has a canonical representation
        /// </summary>
        public bool HasCanonical()
        {
            return !IsCanonical() && (SecurityType.IsOption() || SecurityType == SecurityType.Future);
        }

        /// <summary>
        /// Determines if the specified <paramref name="symbol"/> is an underlying of this symbol instance
        /// </summary>
        /// <param name="symbol">The underlying to check for</param>
        /// <returns>True if the specified <paramref name="symbol"/> is an underlying of this symbol instance</returns>
        public bool HasUnderlyingSymbol(Symbol symbol)
        {
            var current = this;
            while (current.HasUnderlying)
            {
                if (current.Underlying == symbol)
                {
                    return true;
                }

                current = current.Underlying;
            }

            return false;
        }

        #region Properties

        /// <summary>
        /// Gets the current symbol for this ticker
        /// </summary>
        [ProtoMember(1)]
        public string Value { get; private set; }

        /// <summary>
        /// Gets the security identifier for this symbol
        /// </summary>
        [ProtoMember(2)]
        public SecurityIdentifier ID { get; private set; }

        /// <summary>
        /// Gets whether or not this <see cref="Symbol"/> is a derivative,
        /// that is, it has a valid <see cref="Underlying"/> property
        /// </summary>
        public bool HasUnderlying
        {
            get { return !ReferenceEquals(Underlying, null); }
        }

        /// <summary>
        /// Gets the security underlying symbol, if any
        /// </summary>
        [ProtoMember(3)]
        public Symbol Underlying { get; private set; }


        /// <summary>
        /// Gets the security type of the symbol
        /// </summary>
        public SecurityType SecurityType
        {
            get { return ID.SecurityType; }
        }

        /// <summary>
        /// The Committee on Uniform Securities Identification Procedures (CUSIP) number corresponding to this <see cref="Symbol"/>
        /// </summary>
        public string CUSIP { get { return _securityDefinitionSymbolResolver.Value.CUSIP(this); } }

        /// <summary>
        /// The composite Financial Instrument Global Identifier (FIGI) corresponding to this <see cref="Symbol"/>
        /// </summary>
        public string CompositeFIGI { get { return _securityDefinitionSymbolResolver.Value.CompositeFIGI(this); } }

        /// <summary>
        /// The Stock Exchange Daily Official List (SEDOL) security identifier corresponding to this <see cref="Symbol"/>
        /// </summary>
        public string SEDOL { get { return _securityDefinitionSymbolResolver.Value.SEDOL(this); } }

        /// <summary>
        /// The International Securities Identification Number (ISIN) corresponding to this <see cref="Symbol"/>
        /// </summary>
        public string ISIN { get { return _securityDefinitionSymbolResolver.Value.ISIN(this); } }

        /// <summary>
        /// The Central Index Key number (CIK) corresponding to this <see cref="Symbol"/>
        /// </summary>
        public int? CIK { get { return _securityDefinitionSymbolResolver.Value.CIK(this); } }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Symbol"/> class
        /// </summary>
        /// <param name="sid">The security identifier for this symbol</param>
        /// <param name="value">The current ticker symbol value</param>
        public Symbol(SecurityIdentifier sid, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            ID = sid;
            if (ID.HasUnderlying)
            {
                Underlying = new Symbol(ID.Underlying, ID.Underlying.Symbol);
            }

            Value = GetAlias(sid, Underlying) ?? value.LazyToUpper();
        }

        /// <summary>
        /// Creates new symbol with updated mapped symbol. Symbol Mapping: When symbols change over time (e.g. CHASE-> JPM) need to update the symbol requested.
        /// Method returns newly created symbol
        /// </summary>
        public Symbol UpdateMappedSymbol(string mappedSymbol, uint contractDepthOffset = 0)
        {
            // Throw for any option SecurityType that is not for equities, we don't support mapping for them (FOPs and Index Options)
            if (ID.SecurityType.IsOption() && SecurityType != SecurityType.Option)
            {
                throw new ArgumentException(Messages.Symbol.SecurityTypeCannotBeMapped(ID.SecurityType));
            }

            if(ID.SecurityType == SecurityType.Future)
            {
                if (mappedSymbol == Value)
                {
                    // futures with no real continuous mapping
                    return this;
                }
                var id = SecurityIdentifier.Parse(mappedSymbol);
                var underlying = new Symbol(id, mappedSymbol);
                underlying = underlying.AdjustSymbolByOffset(contractDepthOffset);

                // we map the underlying
                return new Symbol(ID, underlying.Value, underlying);
            }

            // Avoid updating the current instance's underlying Symbol.
            var underlyingSymbol = Underlying;

            // Some universe Symbols, such as Constituent ETF universe Symbols and mapped custom data Symbols, have an
            // underlying equity ETF Symbol as their underlying. When we're checking to see if a specific BaseData
            // instance requires mapping, only the parent Symbol will be updated, which might not even need to be mapped
            // (e.g. universe symbols with no equity ticker in symbol value).
            // This will ensure that we map all of the underlying Symbol(s) that also require mapping updates.
            if (HasUnderlying)
            {
                underlyingSymbol = Underlying.UpdateMappedSymbol(mappedSymbol, contractDepthOffset);
            }

            // If this Symbol is not a custom data type, and the security type does not support mapping,
            // then we know for a fact that this Symbol should not be mapped.
            // Custom data types should be mapped, especially if this method is called on them because
            // they can have an underlying that is also mapped.
            if (SecurityType != SecurityType.Base && !this.RequiresMapping())
            {
                return new Symbol(ID, Value, underlyingSymbol);
            }

            if (SecurityType == SecurityType.Option)
            {
                mappedSymbol = !IsCanonical()
                    ? SymbolRepresentation.GenerateOptionTickerOSI(mappedSymbol, ID.OptionRight, ID.StrikePrice, ID.Date)
                    : Value;
            }

            return new Symbol(ID, mappedSymbol, underlyingSymbol);
        }

        /// <summary>
        /// Determines the SecurityType based on the underlying Symbol's SecurityType
        /// </summary>
        /// <param name="underlyingSymbol">Underlying Symbol of an option</param>
        /// <returns>SecurityType of the option</returns>
        /// <exception cref="ArgumentException">The provided underlying has no SecurityType able to represent it as an option</exception>
        public static SecurityType GetOptionTypeFromUnderlying(Symbol underlyingSymbol)
        {
            return GetOptionTypeFromUnderlying(underlyingSymbol.SecurityType);
        }

        /// <summary>
        /// Determines the SecurityType based on the underlying Symbol's SecurityType  <see cref="GetUnderlyingFromOptionType(SecurityType)"/>
        /// </summary>
        /// <param name="securityType">SecurityType of the underlying Symbol</param>
        /// <returns>SecurityType of the option</returns>
        /// <exception cref="ArgumentException">The provided underlying has no SecurityType able to represent it as an option</exception>
        public static SecurityType GetOptionTypeFromUnderlying(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Equity:
                    return SecurityType.Option;
                case SecurityType.Future:
                    return SecurityType.FutureOption;
                case SecurityType.Index:
                    return SecurityType.IndexOption;
                default:
                    throw new ArgumentException(Messages.Symbol.NoOptionTypeForUnderlying(securityType));
            }
        }

        /// <summary>
        /// Determines the underlying SecurityType based on the option Symbol's SecurityType <see cref="GetOptionTypeFromUnderlying(SecurityType)"/>
        /// </summary>
        /// <param name="securityType">SecurityType of the option Symbol</param>
        /// <returns>SecurityType of the underlying</returns>
        /// <exception cref="ArgumentException">The provided option has no SecurityType able to represent it as an underlying</exception>
        public static SecurityType GetUnderlyingFromOptionType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Option:
                    return SecurityType.Equity;
                case SecurityType.FutureOption:
                    return SecurityType.Future;
                case SecurityType.IndexOption:
                    return SecurityType.Index;
                default:
                    throw new ArgumentException(Messages.Symbol.NoUnderlyingForOption(securityType));
            }
        }

        /// <summary>
        /// Private constructor initializes a new instance of the <see cref="Symbol"/> class with underlying
        /// </summary>
        /// <param name="sid">The security identifier for this symbol</param>
        /// <param name="value">The current ticker symbol value</param>
        /// <param name="underlying">The underlying symbol</param>
        internal Symbol(SecurityIdentifier sid, string value, Symbol underlying)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            ID = sid;
            Value = value.LazyToUpper();
            Underlying = underlying;
        }

        #endregion

        #region Overrides of Object

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
            if (ReferenceEquals(this, obj)) return true;

            // compare strings just as you would a symbol object
            if (obj is string stringSymbol)
            {
                return Equals((Symbol)stringSymbol);
            }

            // compare a sid just as you would a symbol object
            if (obj is SecurityIdentifier sid)
            {
                return ID.Equals(sid);
            }

            if (obj.GetType() != GetType()) return false;
            return Equals((Symbol)obj);
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
            // only SID is used for comparisons
            unchecked { return ID.GetHashCode(); }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj"/> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj"/>. Greater than zero This instance follows <paramref name="obj"/> in the sort order.
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            var str = obj as string;
            if (str != null)
            {
                return string.Compare(Value, str, StringComparison.OrdinalIgnoreCase);
            }
            var sym = obj as Symbol;
            if (sym != null)
            {
                return string.Compare(Value, sym.Value, StringComparison.OrdinalIgnoreCase);
            }

            throw new ArgumentException(Messages.Symbol.UnexpectedObjectTypeToCompareTo);
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
            return SymbolCache.GetTicker(this);
        }

        #endregion

        #region Equality members

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Symbol other)
        {
            if (ReferenceEquals(this, other)) return true;

            if (ReferenceEquals(other, null)
                || ReferenceEquals(other, Empty))
            {
                // other is null or empty (equivalents)
                // so we need to know how We compare with Empty
                if (!_isEmpty.HasValue)
                {
                    // for accuracy we compare IDs not references here
                    _isEmpty = ID.Equals(Empty.ID);
                }
                return _isEmpty.Value;
            }

            // only SID is used for comparisons
            return ID.Equals(other.ID);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are equal, otherwise false</returns>
        public static bool operator ==(Symbol left, Symbol right)
        {
            if (ReferenceEquals(left, right))
            {
                // this is a performance shortcut
                return true;
            }

            if (left is null)
            {
                // Rely on the Equals method if possible
                return right is null || right.Equals(left);
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are equal, otherwise false</returns>
        /// <remarks>This is necessary in cases like Pythonnet passing a string
        /// as an object instead of using the implicit conversion</remarks>
        public static bool operator ==(Symbol left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                // this is a performance shortcut
                return true;
            }

            if (left is null)
            {
                // Rely on the Equals method if possible
                return right is null || right.Equals(left);
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are equal, otherwise false</returns>
        /// <remarks>This is necessary in cases like Pythonnet passing a string
        /// as an object instead of using the implicit conversion</remarks>
        public static bool operator ==(object left, Symbol right)
        {
            if (ReferenceEquals(left, right))
            {
                // this is a performance shortcut
                return true;
            }

            if (left is null)
            {
                return right is null;
            }

            if (left is Symbol leftSymbol)
            {
                return leftSymbol.Equals(right);
            }

            if (left is string leftStr)
            {
                return leftStr.Equals(right?.ToString(), StringComparison.InvariantCulture);
            }

            return false;
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are not equal, otherwise false</returns>
        public static bool operator !=(Symbol left, Symbol right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are not equal, otherwise false</returns>
        public static bool operator !=(Symbol left, object right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are not equal, otherwise false</returns>
        public static bool operator !=(object left, Symbol right)
        {
            return !(left == right);
        }

        #endregion

        #region Implicit operators

        /// <summary>
        /// Returns the symbol's string ticker
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The string ticker</returns>
        [Obsolete("Symbol implicit operator to string is provided for algorithm use only.")]
        public static implicit operator string(Symbol symbol)
        {
            return symbol.ToString();
        }

        /// <summary>
        /// Creates symbol using string as sid
        /// </summary>
        /// <param name="ticker">The string</param>
        /// <returns>The symbol</returns>
        [Obsolete("Symbol implicit operator from string is provided for algorithm use only.")]
        public static implicit operator Symbol(string ticker)
        {
            Symbol symbol;
            if (SymbolCache.TryGetSymbol(ticker, out symbol))
            {
                return symbol;
            }

            return new Symbol(new SecurityIdentifier(ticker, 0), ticker);
        }

        #endregion

        #region String methods

        // in order to maintain better compile time backwards compatibility,
        // we'll redirect a few common string methods to Value, but mark obsolete
#pragma warning disable 1591
        [Obsolete("Symbol.Contains is a pass-through for Symbol.Value.Contains")]
        public bool Contains(string value) { return Value.Contains(value); }
        [Obsolete("Symbol.EndsWith is a pass-through for Symbol.Value.EndsWith")]
        public bool EndsWith(string value) { return Value.EndsWithInvariant(value); }
        [Obsolete("Symbol.StartsWith is a pass-through for Symbol.Value.StartsWith")]
        public bool StartsWith(string value) { return Value.StartsWithInvariant(value); }
        [Obsolete("Symbol.ToLower is a pass-through for Symbol.Value.ToLower")]
        public string ToLower() { return Value.ToLowerInvariant(); }
        [Obsolete("Symbol.ToUpper is a pass-through for Symbol.Value.ToUpper")]
        public string ToUpper() { return Value.LazyToUpper(); }
#pragma warning restore 1591

        #endregion

        /// <summary>
        /// Centralized helper method to resolve alias for a symbol
        /// </summary>
        public static string GetAlias(SecurityIdentifier securityIdentifier, Symbol underlying = null)
        {
            string sym;
            switch (securityIdentifier.SecurityType)
            {
                case SecurityType.FutureOption:
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    sym = underlying.Value;
                    if (securityIdentifier.Symbol != underlying.ID.Symbol)
                    {
                        // If we have changed the SID and it does not match the underlying,
                        // we've mapped a future into another Symbol. We want to have a value
                        // representing the mapped ticker, not of the underlying.
                        // e.g. we want:
                        //     OG  C3200...|GC18Z20
                        // NOT
                        //     GC  C3200...|GC18Z20
                        sym = securityIdentifier.Symbol;
                    }

                    if (securityIdentifier.Date == SecurityIdentifier.DefaultDate)
                    {
                        return $"?{sym.LazyToUpper()}";
                    }

                    if (sym.Length > 5) sym += " ";

                    return SymbolRepresentation.GenerateOptionTickerOSI(sym, securityIdentifier.OptionRight, securityIdentifier.StrikePrice, securityIdentifier.Date);
                case SecurityType.Future:
                    sym = securityIdentifier.Symbol;
                    if (securityIdentifier.Date == SecurityIdentifier.DefaultDate)
                    {
                        return $"/{sym}";
                    }
                    return SymbolRepresentation.GenerateFutureTicker(sym, securityIdentifier.Date);
                default:
                    return null;
            }
        }

        private static bool IsCanonical(SecurityIdentifier sid)
        {
            return
                (sid.SecurityType == SecurityType.Future ||
                (sid.SecurityType.IsOption() && sid.HasUnderlying)) &&
                sid.Date == SecurityIdentifier.DefaultDate;
        }
    }
}
