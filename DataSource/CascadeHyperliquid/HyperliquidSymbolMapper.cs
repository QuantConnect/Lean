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
    /// Symbol mapper for Hyperliquid perpetual futures and spot
    /// </summary>
    /// <remarks>
    /// Hyperliquid perpetuals:
    /// - Use simple coin names: "BTC", "ETH", "SOL"
    /// - All settled in USDC
    /// - LEAN representation: BTCUSDC, ETHUSDC, etc. with SecurityType.CryptoFuture
    ///   (Using USDC suffix for proper currency pair decomposition)
    ///
    /// Hyperliquid spot:
    /// - Spot tokens use "U" prefix: "UBTC", "UETH", "USOL"
    /// - Quote currency is USD
    /// - LEAN representation: UBTCUSD, UETHUSD, etc. with SecurityType.Crypto
    /// </remarks>
    public class HyperliquidSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// The quote currency for Hyperliquid perpetuals
        /// </summary>
        public const string QuoteCurrency = "USDC";

        /// <summary>
        /// The quote currency for Hyperliquid spot
        /// </summary>
        public const string SpotQuoteCurrency = "USD";

        /// <summary>
        /// Supported security types for Hyperliquid
        /// </summary>
        public readonly HashSet<SecurityType> SupportedSecurityTypes = new()
        {
            SecurityType.CryptoFuture,
            SecurityType.Crypto
        };

        /// <summary>
        /// Converts a LEAN symbol to Hyperliquid coin format
        /// </summary>
        /// <param name="symbol">LEAN symbol</param>
        /// <returns>Hyperliquid coin symbol (e.g., "BTC" for perps, "UBTC" for spot)</returns>
        /// <exception cref="ArgumentException">If symbol is not supported</exception>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrEmpty(symbol.Value))
            {
                throw new ArgumentNullException(nameof(symbol), "Symbol cannot be null or empty");
            }

            if (!SupportedSecurityTypes.Contains(symbol.SecurityType))
            {
                throw new ArgumentException(
                    $"Hyperliquid only supports CryptoFuture and Crypto, but received {symbol.SecurityType}",
                    nameof(symbol));
            }

            var ticker = symbol.Value;

            if (symbol.SecurityType == SecurityType.Crypto)
            {
                // Spot: UBTCUSD -> UBTC, UETHUSD -> UETH
                // Remove USD suffix
                if (ticker.EndsWith("USD", StringComparison.OrdinalIgnoreCase))
                {
                    return ticker.Substring(0, ticker.Length - 3);
                }
                return ticker;
            }

            // CryptoFuture (perps): BTCUSDC -> BTC, ETHUSDC -> ETH
            // Handle common quote currency suffixes (USDC first as it's the correct one)
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
        /// <param name="brokerageSymbol">Hyperliquid coin symbol (e.g., "BTC" for perps, "UBTC" for spot)</param>
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
                    $"Hyperliquid only supports CryptoFuture and Crypto, but received {securityType}",
                    nameof(securityType));
            }

            string leanTicker;
            if (securityType == SecurityType.Crypto)
            {
                // Spot: UBTC -> UBTCUSD, UETH -> UETHUSD
                leanTicker = $"{brokerageSymbol.ToUpperInvariant()}{SpotQuoteCurrency}";
            }
            else
            {
                // CryptoFuture: BTC -> BTCUSDC, ETH -> ETHUSDC
                leanTicker = $"{brokerageSymbol.ToUpperInvariant()}{QuoteCurrency}";
            }

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
            return QuoteCurrency;
        }
    }
}
