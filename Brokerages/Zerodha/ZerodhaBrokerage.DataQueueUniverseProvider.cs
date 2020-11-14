using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// ZerodhaBrokerage - IDataQueueUniverseProvider implementation
    /// </summary>
    public partial class ZerodhaBrokerage
    {
        #region IDataQueueUniverseProvider

        /// <summary>
        /// Method returns a collection of Symbols that are available at the broker.
        /// </summary>
        /// <param name="lookupName">String representing the name to lookup</param>
        /// <param name="securityType">Expected security type of the returned symbols (if any)</param>
        /// <param name="includeExpired">Include expired contracts</param>
        /// <param name="securityCurrency">Expected security currency(if any)</param>
        /// <param name="securityExchange">Expected security exchange name(if any)</param>
        /// <returns></returns>
        public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, bool includeExpired, string securityCurrency = null, string securityExchange = null)
        {
            Func<Symbol, string> lookupFunc;

            switch (securityType)
            {
                case SecurityType.Option:
                    // for option, futures contract we search the underlying
                    lookupFunc = symbol => symbol.HasUnderlying ? symbol.Underlying.Value : string.Empty;
                    break;
                case SecurityType.Future:
                    lookupFunc = symbol => symbol.ID.Symbol;
                    break;
                default:
                    lookupFunc = symbol => symbol.Value;
                    break;
            }

            var result = _symbolMapper.KnownSymbols.Where(x => lookupFunc(x) == lookupName &&
                                            x.ID.SecurityType == securityType &&
                                            (securityExchange == null || x.ID.Market == securityExchange))
                                         .ToList();

            return result.Select(x => x);
        }


        /// <summary>
        /// Returns whether the time can be advanced or not.
        /// </summary>
        /// <param name="securityType">The security type</param>
        /// <returns>true if the time can be advanced</returns>
        public bool CanAdvanceTime(SecurityType securityType)
        {
            return true;
        }

        #endregion
    }
}
