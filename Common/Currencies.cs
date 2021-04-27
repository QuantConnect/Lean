/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;

namespace QuantConnect
{
    /// <summary>
    /// Provides commonly used currency pairs and symbols
    /// </summary>
    public static class Currencies
    {
        /// <summary>
        /// USD currency string
        /// </summary>
        public static string USD = "USD";

        /// <summary>
        /// EUR currency string
        /// </summary>
        public static string EUR = "EUR";

        /// <summary>
        /// GBP currency string
        /// </summary>
        public static string GBP = "GBP";

        /// <summary>
        /// INR currency string
        /// </summary>
        public static string INR = "INR";

        /// <summary>
        /// Null currency used when a real one is not required
        /// </summary>
        public const string NullCurrency = "QCC";

        /// <summary>
        /// A mapping of currency codes to their display symbols
        /// </summary>
        /// <remarks>
        /// Now used by Forex and CFD, should probably be moved out into its own class
        /// </remarks>
        public static readonly IReadOnlyDictionary<string, string> CurrencySymbols = new Dictionary<string, string>
        {
            {"USD", "$"},
            {"GBP", "₤"},
            {"JPY", "¥"},
            {"EUR", "€"},
            {"NZD", "$"},
            {"AUD", "$"},
            {"CAD", "$"},
            {"CHF", "Fr"},
            {"HKD", "$"},
            {"SGD", "$"},
            {"XAG", "Ag"},
            {"XAU", "Au"},
            {"CNH", "¥"},
            {"CNY", "¥"},
            {"CZK", "Kč"},
            {"DKK", "kr"},
            {"HUF", "Ft"},
            {"INR", "₹"},
            {"MXN", "$"},
            {"NOK", "kr"},
            {"PLN", "zł"},
            {"SAR", "﷼"},
            {"SEK", "kr"},
            {"THB", "฿"},
            {"TRY", "₺"},
            {"TWD", "NT$"},
            {"ZAR", "R"},

            {"BTC", "฿"},
            {"BCH", "฿"},
            {"LTC", "Ł"},
            {"ETH", "Ξ"},

            {"EOS", "EOS"},
            {"XRP", "XRP"},
            {"XLM", "XLM"},
            {"ETC", "ETC"},
            {"ZRX", "ZRX"},
            {"USDT", "USDT"}
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        public static HashSet<Symbol> StableCoinsWithoutPairs = new HashSet<Symbol>
        {
            // Binance StableCoins Missing 1-1 Pairs
            Symbol.Create("USDCUSD", SecurityType.Crypto, Market.Binance), // USD -> USDC
            Symbol.Create("BGBPGBP", SecurityType.Crypto, Market.Binance), // GBP -> BGBP

            // Coinbase StableCoins Missing 1-1 Pairs
            Symbol.Create("USDCUSD", SecurityType.Crypto, Market.GDAX), // USD -> USDC

            // Bitfinex StableCoins Missing 1-1 Pairs
            Symbol.Create("EURSEUR", SecurityType.Crypto, Market.Bitfinex), // EUR -> EURS
        };

        /// <summary>
        /// Gets the currency symbol for the specified currency code
        /// </summary>
        /// <param name="currency">The currency code</param>
        /// <returns>The currency symbol</returns>
        public static string GetCurrencySymbol(string currency)
        {
            string currencySymbol;
            return CurrencySymbols.TryGetValue(currency, out currencySymbol) ? currencySymbol : "$";
        }
    }
}
