using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Static class contains common utility methods specific to symbols representing the future contracts
    /// </summary>
    public static class FutureSymbol
    {
        /// <summary>
        /// Returns true if the option is a standard contract that expires 3rd Friday of the month
        /// </summary>
        /// <param name="symbol">Option symbol</param>
        /// <returns></returns>
        public static bool IsStandard(Symbol symbol)
        {
            var date = symbol.ID.Date;
            var symbolToCheck = symbol.HasUnderlying ? symbol.Underlying : symbol;

            // Use our FutureExpiryFunctions to determine standard contracts dates.
            var expiryFunction = FuturesExpiryFunctions.FuturesExpiryFunction(symbolToCheck);
            var standardDate = expiryFunction(date);

            // If the date on this symbol and the nearest standard date are equal then it is a standard contract
            if (date == standardDate)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the future contract is a weekly contract
        /// </summary>
        /// <param name="symbol">Future symbol</param>
        /// <returns></returns>
        public static bool IsWeekly(Symbol symbol)
        {
            return !IsStandard(symbol);
        }
    }
}
