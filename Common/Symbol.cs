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
    public class Symbol : IEquatable<Symbol>, IComparable
    {
        /// <summary>
        /// Represents an unassigned symbol. This is intended to be used as an
        /// uninitialized, default value
        /// </summary>
        public static readonly Symbol Empty = new Symbol(string.Empty, string.Empty);

        #region Properties

        /// <summary>
        /// Gets the current symbol for this ticker
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets permtick used to identify securities in map files
        /// </summary>
        public string Permtick { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Symbol"/> class using the specified
        /// string as both the symbol's value and sid
        /// </summary>
        /// <param name="symbol"></param>
        public Symbol(string symbol)
            : this(symbol, symbol)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Symbol"/> class
        /// </summary>
        /// <param name="permtick">The security's unique identifier</param>
        /// <param name="value">The security's current ticker symbol</param>
        public Symbol(string permtick, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (permtick == null)
            {
                throw new ArgumentNullException("permtick");
            }
            Value = value.ToUpper();
            Permtick = permtick.ToUpper();
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
            // compare strings just as you would a symbol object
            var sid = obj as string;
            if (sid != null) return Permtick.Equals(sid);
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
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
            unchecked
            {
                // only SID is used for comparisons
                return Permtick.GetHashCode();
            }
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
                return string.Compare(Permtick, str, StringComparison.OrdinalIgnoreCase);
            }
            var sym = obj as Symbol;
            if (sym != null)
            {
                return string.Compare(Permtick, sym.Permtick, StringComparison.OrdinalIgnoreCase);
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
            return Permtick;
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
            return string.Equals(Permtick, other.Permtick);
        }

        /// <summary>
        /// Equals operator 
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are equal, otherwise false</returns>
        public static bool operator ==(Symbol left, Symbol right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Not equals operator 
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>True if both symbols are not equal, otherwise false</returns>
        public static bool operator !=(Symbol left, Symbol right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region Implicit operators

        /// <summary>
        /// Returns the symbol's SID
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The SID</returns>
        public static implicit operator string(Symbol symbol)
        {
            return symbol.Permtick;
        }

        /// <summary>
        /// Creates symbol using string as sid
        /// </summary>
        /// <param name="symbol">The string</param>
        /// <returns>The symbol</returns>
        public static implicit operator Symbol(string symbol)
        {
            return new Symbol(symbol);
        }

        #endregion

        #region String methods

        // in order to maintain better compile time backwards compatibility,
        // we'll redirect a few common string methods to Permtick
#pragma warning disable 1591
        public bool Contains(string value) { return Permtick.Contains(value); }
        public bool EndsWith(string value) { return Permtick.EndsWith(value); }
        public bool StartsWith(string value) { return Permtick.StartsWith(value); }
        public string ToLower() { return Permtick.ToLower(); }
        public string ToUpper() { return Permtick.ToUpper(); }
#pragma warning restore 1591

        #endregion
    }
}
