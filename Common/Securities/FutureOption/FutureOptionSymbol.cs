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
    }
}
