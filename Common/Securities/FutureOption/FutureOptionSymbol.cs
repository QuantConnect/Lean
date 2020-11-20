using System;

namespace QuantConnect.Securities.FutureOption
{
    /// <summary>
    /// Static helper methods to resolve Futures Options Symbol-related tasks.
    /// </summary>
    public static class FutureOptionSymbol
    {
        /// <summary>
        /// Detects if the future option contract is standard, i.e. not weekly, not short-term, not mid-sized, etc.
        /// </summary>
        /// <param name="_">Symbol</param>
        /// <returns>true</returns>
        /// <remarks>
        /// We have no way of identifying the type of FOP contract based on the properties contained within the Symbol.
        /// </remarks>
        public static bool IsStandard(Symbol _) => true;

        /// <summary>
        /// Gets the last day of trading, aliased to be the Futures options' expiry
        /// </summary>
        /// <param name="symbol">Futures Options Symbol</param>
        /// <returns>Last day of trading date</returns>
        public static DateTime GetLastDayOfTrading(Symbol symbol) => symbol.ID.Date.Date;
    }
}
