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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a string key for a market, symbol and security type
    /// Used for lookups in <see cref="MarketHoursDatabase"/>
    /// </summary>
    class SecurityKey : IEquatable<SecurityKey>
    {
        public readonly string Market;
        public readonly string Symbol;
        public readonly SecurityType SecurityType;

        /// <summary>
        /// Creates an instance of the <see cref="SecurityKey"/> class
        /// </summary>
        public SecurityKey(string market, string symbol, SecurityType securityType)
        {
            Market = market;
            SecurityType = securityType;
            Symbol = symbol;
        }

        #region Equality members

        public bool Equals(SecurityKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Market, other.Market) && Equals(Symbol, other.Symbol) && SecurityType == other.SecurityType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SecurityKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Market != null ? Market.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)SecurityType;
                return hashCode;
            }
        }

        public static bool operator ==(SecurityKey left, SecurityKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SecurityKey left, SecurityKey right)
        {
            return !Equals(left, right);
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}", Market ?? "[null]", Symbol ?? "[null]", SecurityType);
        }
    }
}
