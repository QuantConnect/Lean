/*
 * Cascade Labs - Hyperliquid Symbol Mapper
 *
 * Maps between LEAN symbols and Hyperliquid coin symbols
 * Hyperliquid uses simple coin names like "BTC", "ETH", "SOL"
 * All perpetuals are settled in USDC
 */

using QuantConnect.Brokerages;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Symbol mapper for Hyperliquid perpetual futures
    /// </summary>
    /// <remarks>
    /// Hyperliquid perpetuals:
    /// - Use simple coin names: "BTC", "ETH", "SOL"
    /// - All settled in USDC
    /// - LEAN representation: BTCUSD, ETHUSD, etc. with SecurityType.CryptoFuture
    /// </remarks>
    public class HyperliquidSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Supported security types for Hyperliquid
        /// </summary>
        public readonly HashSet<SecurityType> SupportedSecurityTypes = new()
        {
            SecurityType.CryptoFuture
        };

        /// <summary>
        /// Converts a LEAN symbol to Hyperliquid coin format
        /// </summary>
        /// <param name="symbol">LEAN symbol</param>
        /// <returns>Hyperliquid coin symbol (e.g., "BTC")</returns>
        /// <exception cref="ArgumentException">If symbol is not a CryptoFuture</exception>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrEmpty(symbol.Value))
            {
                throw new ArgumentNullException(nameof(symbol), "Symbol cannot be null or empty");
            }

            if (!SupportedSecurityTypes.Contains(symbol.SecurityType))
            {
                throw new ArgumentException(
                    $"Hyperliquid only supports {SecurityType.CryptoFuture}, but received {symbol.SecurityType}",
                    nameof(symbol));
            }

            // LEAN symbol format: BTCUSD, ETHUSD, SOLUSD, etc.
            // Hyperliquid format: BTC, ETH, SOL
            // Remove the quote currency (USD or USDC)
            var ticker = symbol.Value;

            // Handle common quote currency suffixes
            var quoteSuffixes = new[] { "USDC", "USD", "PERP" };
            foreach (var suffix in quoteSuffixes)
            {
                if (ticker.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return ticker.Substring(0, ticker.Length - suffix.Length);
                }
            }

            // If no suffix found, return as-is
            // This handles cases where the symbol is already in the correct format
            return ticker;
        }

        /// <summary>
        /// Converts a Hyperliquid coin symbol to a LEAN symbol
        /// </summary>
        /// <param name="brokerageSymbol">Hyperliquid coin symbol (e.g., "BTC")</param>
        /// <param name="securityType">LEAN security type</param>
        /// <param name="market">Market identifier</param>
        /// <param name="expirationDate">Expiration date (not used for perpetuals)</param>
        /// <param name="strike">Strike price (not used)</param>
        /// <param name="optionRight">Option right (not used)</param>
        /// <returns>LEAN Symbol object</returns>
        public Symbol GetLeanSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string market,
            DateTime expirationDate = default,
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentNullException(nameof(brokerageSymbol), "Brokerage symbol cannot be null or empty");
            }

            if (!SupportedSecurityTypes.Contains(securityType))
            {
                throw new ArgumentException(
                    $"Hyperliquid only supports {SecurityType.CryptoFuture}, but received {securityType}",
                    nameof(securityType));
            }

            // Convert Hyperliquid coin to LEAN format
            // BTC -> BTCUSD (as LEAN represents perpetuals with USD quote)
            var leanTicker = $"{brokerageSymbol.ToUpperInvariant()}USD";

            return Symbol.Create(leanTicker, securityType, market);
        }

        /// <summary>
        /// Validates if a symbol is supported by Hyperliquid
        /// </summary>
        /// <param name="symbol">Symbol to validate</param>
        /// <returns>True if the symbol is supported</returns>
        public bool IsSymbolSupported(Symbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }

            return SupportedSecurityTypes.Contains(symbol.SecurityType);
        }

        /// <summary>
        /// Gets the quote currency for Hyperliquid perpetuals
        /// </summary>
        /// <returns>Quote currency (USDC)</returns>
        public static string GetQuoteCurrency()
        {
            return "USDC";
        }
    }
}
