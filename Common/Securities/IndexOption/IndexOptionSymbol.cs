using System.Collections.Generic;

namespace QuantConnect.Securities.IndexOption
{
    public static class IndexOptionSymbol
    {
        private static readonly HashSet<string> _supportedIndexOptionTickers = new HashSet<string>
        {
            "SPX",
            "NDX",
            "VIX",
            "SPXW",
            "NQX",
            "VIXW"
        };

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
                // These are known assets that are weeklies or end-of-month settled contracts.
                case "SPXW":
                case "VIXW":
                case "NDXP":
                case "NQX":
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks if the ticker provided is a supported Index Option
        /// </summary>
        /// <param name="ticker">Ticker of the index option</param>
        /// <returns>true if the ticker matches an index option's ticker</returns>
        /// <remarks>
        /// This is only used in IB brokerage, since they don't distinguish index options
        /// from regular equity options. When we do the conversion from a contract to a SecurityType,
        /// the only information we're provided that can reverse it to the <see cref="SecurityType.IndexOption"/>
        /// enum value is the ticker.
        /// </remarks>
        public static bool IsIndexOption(string ticker)
        {
            return _supportedIndexOptionTickers.Contains(ticker.ToUpper());
        }
    }
}
