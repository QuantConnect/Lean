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
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Combines a symbol and a security type, used as subscription keys in data feeds
    /// </summary>
    public sealed class SymbolSecurityType
    {
        public readonly Symbol Symbol;
        public readonly SecurityType SecurityType;

        /// <summary>
        /// Initialzies a new instance of the <see cref="SymbolSecurityType"/> class
        /// </summary>
        /// <param name="symbol">The symbol of the security</param>
        /// <param name="securityType">The security type of the security</param>
        public SymbolSecurityType(Symbol symbol, SecurityType securityType)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol");
            }
            Symbol = symbol;
            SecurityType = securityType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolSecurityType"/> class
        /// </summary>
        /// <param name="security">The security</param>
        public SymbolSecurityType(Security security)
            : this(security.Symbol, security.Type)
        {
            // convenience ctor
        }

        /// <summary>
        /// Initilizes a new instance of the <see cref="SymbolSecurityType"/> class
        /// </summary>
        /// <param name="subscription">The subscription</param>
        public SymbolSecurityType(Subscription subscription)
            : this(subscription.Security)
        {
            // convenience ctor
        }

        /// <summary>
        /// Determines whether the specified <see cref="SymbolSecurityType"/> is equal to the current <see cref="SymbolSecurityType"/>.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="other">The object to compare with the current object. </param><filterpriority>2</filterpriority>
        public bool Equals(SymbolSecurityType other)
        {
            return String.Equals(Symbol, other.Symbol) && SecurityType == other.SecurityType;
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
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SymbolSecurityType) obj);
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
                return (Symbol.GetHashCode()*397) ^ (int) SecurityType;
            }
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
            return string.Format("{0}: {1}", SecurityType, Symbol);
        }
    }
}