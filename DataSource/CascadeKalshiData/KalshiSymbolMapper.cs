/*
 * Cascade Labs - Kalshi Symbol Mapper
 * Maps between Kalshi market tickers and LEAN Symbols
 */

using System.Linq;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Maps between Kalshi market tickers and LEAN Symbols.
    /// Kalshi tickers are used directly as the symbol value.
    /// </summary>
    public class KalshiSymbolMapper
    {
        /// <summary>
        /// The market identifier for Kalshi in LEAN
        /// </summary>
        public const string KalshiMarket = "kalshi";

        /// <summary>
        /// Create a LEAN Symbol from a Kalshi market ticker
        /// </summary>
        /// <param name="ticker">Kalshi market ticker (e.g., KXHIGHNY-26JAN20-T62, INXD-25FEB07-B5350)</param>
        /// <returns>LEAN Symbol with SecurityType.Base and market "kalshi"</returns>
        public Symbol GetLeanSymbol(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException("Ticker cannot be null or empty", nameof(ticker));
            }

            return Symbol.Create(ticker.ToUpperInvariant(), SecurityType.PredictionMarket, KalshiMarket);
        }

        /// <summary>
        /// Get the Kalshi market ticker from a LEAN symbol
        /// </summary>
        /// <param name="symbol">LEAN Symbol</param>
        /// <returns>Kalshi market ticker</returns>
        public string GetKalshiTicker(Symbol symbol)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            return symbol.Value.ToUpperInvariant();
        }

        /// <summary>
        /// Check if a symbol is a valid Kalshi symbol
        /// </summary>
        public bool IsKalshiSymbol(Symbol symbol)
        {
            if (symbol == null) return false;
            if (symbol.SecurityType != SecurityType.PredictionMarket) return false;
            if (string.IsNullOrWhiteSpace(symbol.Value)) return false;

            // Kalshi market or USA (for backwards compatibility)
            return symbol.ID.Market == KalshiMarket || symbol.ID.Market == Market.USA;
        }

        /// <summary>
        /// Check if a ticker string looks like a Kalshi ticker.
        /// Kalshi tickers have format: SERIES-YYMONDD-STRIKE
        /// Examples: KXHIGHNY-26JAN16-T33, INXD-25FEB07-B5350
        /// </summary>
        public bool IsKalshiTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker)) return false;

            // Must contain hyphens
            var parts = ticker.ToUpperInvariant().Split('-');
            if (parts.Length < 2) return false;

            // First part should be the series (letters/numbers)
            var series = parts[0];
            if (string.IsNullOrEmpty(series)) return false;
            if (!series.All(c => char.IsLetterOrDigit(c))) return false;

            // Known Kalshi series prefixes (weather, politics, finance, etc.)
            var knownPrefixes = new[] { "KX", "INX", "CPI", "NGAS", "BTX", "ETH", "SPY", "NDX", "RUT", "DJI" };
            if (knownPrefixes.Any(p => series.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Second part should look like a date (e.g., 26JAN16)
            if (parts.Length >= 2)
            {
                var datePart = parts[1];
                if (datePart.Length >= 5 && datePart.Length <= 7)
                {
                    // Should have digits followed by letters followed by digits
                    var hasDigits = datePart.Any(char.IsDigit);
                    var hasLetters = datePart.Any(char.IsLetter);
                    if (hasDigits && hasLetters)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
