using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents the key to a single entry in the <see cref="MarketHoursDatabase"/> or the <see cref="SymbolPropertiesDatabase"/>
    /// </summary>
    public class SecurityDatabaseKey : IEquatable<SecurityDatabaseKey>
    {
        private const string Wildcard = "[*]";

        /// <summary>
        /// The market. If null, ignore market filtering
        /// </summary>
        public readonly string Market;
        /// <summary>
        /// The symbol. If null, ignore symbol filtering
        /// </summary>
        public readonly string Symbol;
        /// <summary>
        /// The security type
        /// </summary>
        public readonly SecurityType SecurityType;
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityDatabaseKey"/> class
        /// </summary>
        /// <param name="market">The market</param>
        /// <param name="symbol">The symbol. specify null to apply to all symbols in market/security type</param>
        /// <param name="securityType">The security type</param>
        public SecurityDatabaseKey(string market, string symbol, SecurityType securityType)
        {
            Market = market;
            SecurityType = securityType;
            Symbol = symbol;
        }

        /// <summary>
        /// Parses the specified string as a <see cref="SecurityDatabaseKey"/>
        /// </summary>
        /// <param name="key">The string representation of the key</param>
        /// <returns>A new <see cref="SecurityDatabaseKey"/> instance</returns>
        public static SecurityDatabaseKey Parse(string key)
        {
            var parts = key.Split('-');
            if (parts.Length != 3)
            {
                throw new ArgumentException("The specified key was not in the expected format: " + key);
            }
            SecurityType type;
            if (!Enum.TryParse(parts[0], out type))
            {
                throw new ArgumentException("Unable to parse '" + parts[2] + "' as a SecurityType.");
            }

            var market = parts[1];
            if (market == Wildcard) market = null;

            var symbol = parts[2];
            if (symbol == Wildcard) symbol = null;

            return new SecurityDatabaseKey(market, symbol, type);
        }

        #region Equality members

        public bool Equals(SecurityDatabaseKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Market, other.Market) && Equals(Symbol, other.Symbol) && SecurityType == other.SecurityType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SecurityDatabaseKey)obj);
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

        public static bool operator ==(SecurityDatabaseKey left, SecurityDatabaseKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SecurityDatabaseKey left, SecurityDatabaseKey right)
        {
            return !Equals(left, right);
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}", SecurityType, Market ?? Wildcard, Symbol ?? Wildcard);
        }
    }
}
