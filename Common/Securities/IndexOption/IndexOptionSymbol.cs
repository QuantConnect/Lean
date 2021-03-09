namespace QuantConnect.Securities.IndexOption
{
    public static class IndexOptionSymbol
    {
        /// <summary>
        /// Determines if the Index Option Symbol is for a monthly contract
        /// </summary>
        /// <param name="symbol">Index Option Symbol</param>
        /// <returns>True if monthly contract, false otherwise</returns>
        public static bool IsStandard(Symbol symbol)
        {
            if (symbol.ID.Market != Market.USA)
            {
                return true;
            }

            switch (symbol.ID.Symbol)
            {
                // These are known assets that are weeklies and P.M. settled contracts.
                case "SPXW":
                case "VIXW":
                case "NDXP":
                case "NQX":
                    return false;

                default:
                    return true;
            }
        }
    }
}
