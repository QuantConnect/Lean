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
using Newtonsoft.Json;

namespace QuantConnect
{
    /// <summary>
    /// Represents a unique security identifier. This is made of two components,
    /// the unique SID and the Value. The value is the current ticker symbol while
    /// the SID is constant over the life of a security
    /// </summary>
    [JsonConverter(typeof(SymbolJsonConverter))]
    public sealed class Symbol : IEquatable<Symbol>, IComparable
    {
        /// <summary>
        /// Represents an unassigned symbol. This is intended to be used as an
        /// uninitialized, default value
        /// </summary>
        public static readonly Symbol Empty = new Symbol(SecurityIdentifier.Empty, string.Empty);

        /// <summary>
        /// Provides a convience method for creating a Symbol for most security types.
        /// This method currently does not support Option, Commodity, and Future
        /// </summary>
        /// <param name="ticker">The string ticker symbol</param>
        /// <param name="securityType">The security type of the ticker. If securityType == Option, then a canonical symbol is created</param>
        /// <param name="market">The market the ticker resides in</param>
        /// <param name="alias">An alias to be used for the symbol cache. Required when
        /// adding the same security from different markets</param>
        /// <returns>A new Symbol object for the specified ticker</returns>
        public static Symbol Create(string ticker, SecurityType securityType, string market, string alias = null)
        {
            SecurityIdentifier sid;
            switch (securityType)
            {
                case SecurityType.Base:
                    sid = SecurityIdentifier.GenerateBase(ticker, market);
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
                case SecurityType.Option:
                    alias = alias ?? "?" + ticker.ToUpper();
                    sid = SecurityIdentifier.GenerateOption(SecurityIdentifier.DefaultDate, ticker, market, 0, default(OptionRight), default(OptionStyle));
                    break;
                case SecurityType.Commodity:
                case SecurityType.Future:
                default:
                    throw new NotImplementedException("The security type has not been implemented yet: " + securityType);
            }

            return new Symbol(sid, alias ?? ticker);
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
        /// adding the same security from diferent markets</param>
        /// <returns>A new Symbol object for the specified option contract</returns>
        public static Symbol CreateOption(string underlying, string market, OptionStyle style, OptionRight right, decimal strike, DateTime expiry, string alias = null)
        {
            var sid = SecurityIdentifier.GenerateOption(expiry, underlying, market, strike, right, style);
            var sym = sid.Symbol;
            if (sym.Length > 5) sym += " ";
            // format spec: http://www.optionsclearing.com/components/docs/initiatives/symbology/symbology_initiative_v1_8.pdf
            alias = alias ?? string.Format("{0,-6}{1}{2}{3:00000000}", sym, sid.Date.ToString(DateFormat.SixCharacter), sid.OptionRight.ToString()[0], sid.StrikePrice * 1000m);
            return new Symbol(sid, alias);
        }

        #region Properties

        /// <summary>
        /// Gets the current symbol for this ticker
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets the security identifier for this symbol
        /// </summary>
        public SecurityIdentifier ID { get; private set; }

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
                throw new ArgumentNullException("value");
            }
            ID = sid;
            Value = value.ToUpper();
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
            var sidString = obj as string;
            if (sidString != null)
            {
                SecurityIdentifier sid;
                if (SecurityIdentifier.TryParse(sidString, out sid))
                {
                    return ID.Equals(sid);
                }
            }
            
            // compare a sid just as you would a symbol object
            if (obj is SecurityIdentifier)
            {
                return ID.Equals((SecurityIdentifier) obj);
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

            throw new ArgumentException("Object must be of type Symbol or string.");
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
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
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
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        /// <summary>
        /// Not equals operator 
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are not equal, otherwise false</returns>
        public static bool operator !=(Symbol left, Symbol right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return !left.Equals(right);
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

            SecurityIdentifier sid;
            if (SecurityIdentifier.TryParse(ticker, out sid))
            {
                return new Symbol(sid, sid.Symbol);
            }
            
            return Empty;
        }

        #endregion

        #region String methods

        // in order to maintain better compile time backwards compatibility,
        // we'll redirect a few common string methods to Value, but mark obsolete
#pragma warning disable 1591
        [Obsolete("Symbol.Contains is a pass-through for Symbol.Value.Contains")]
        public bool Contains(string value) { return Value.Contains(value); }
        [Obsolete("Symbol.EndsWith is a pass-through for Symbol.Value.EndsWith")]
        public bool EndsWith(string value) { return Value.EndsWith(value); }
        [Obsolete("Symbol.StartsWith is a pass-through for Symbol.Value.StartsWith")]
        public bool StartsWith(string value) { return Value.StartsWith(value); }
        [Obsolete("Symbol.ToLower is a pass-through for Symbol.Value.ToLower")]
        public string ToLower() { return Value.ToLower(); }
        [Obsolete("Symbol.ToUpper is a pass-through for Symbol.Value.ToUpper")]
        public string ToUpper() { return Value.ToUpper(); }
#pragma warning restore 1591

        #endregion
    }
}
