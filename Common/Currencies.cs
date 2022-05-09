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
        /// USD (United States Dollar) currency string
        /// </summary>
        public const string USD = "USD";

        /// <summary>
        /// EUR (Euro) currency string
        /// </summary>
        public const string EUR = "EUR";

        /// <summary>
        /// GBP (British pound sterling) currency string
        /// </summary>
        public const string GBP = "GBP";

        /// <summary>
        /// INR (Indian rupee) currency string
        /// </summary>
        public const string INR = "INR";

        /// <summary>
        /// IDR (Indonesian rupiah) currency string
        /// </summary>
        public const string IDR = "IDR";

        /// <summary>
        /// CNH (Chinese Yuan Renminbi) currency string
        /// </summary>
        public const string CNH = "CNH";

        /// <summary>
        /// CHF (Swiss Franc) currency string
        /// </summary>
        public const string CHF = "CHF";

        /// <summary>
        /// HKD (Hong Kong dollar) currency string
        /// </summary>
        public const string HKD = "HKD";

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
            {USD, "$"},
            {GBP, "₤"},
            {"JPY", "¥"},
            {EUR, "€"},
            {"NZD", "$"},
            {"AUD", "$"},
            {"CAD", "$"},
            {"CHF", "Fr"},
            {HKD, "$"},
            {"SGD", "$"},
            {"XAG", "Ag"},
            {"XAU", "Au"},
            {CNH, "¥"},
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
        /// Commonly StableCoins quote currencies
        /// </summary>
        private static readonly string[] _stableCoinsCurrencies = new string[] 
        { 
            USD,
            EUR,
            IDR,
            CHF
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in GDAX market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly Dictionary<string, int> _stableCoinsGDAX = new Dictionary<string, int> 
        {
            { "USDC", 0 }
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Binance market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly Dictionary<string, int> _stableCoinsBinance = new Dictionary<string, int>
        {
            { "USDC", 0 },
            { "USDT", 0 },
            { "USDP", 0 },
            { "BUSD", 0 },
            { "UST", 0 },
            { "TUSD", 0 },
            { "DAI", 0 },
            { "SUSD", 0},
            { "IDRT", 2}
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Bitfinex market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly Dictionary<string, int> _stableCoinsBitfinex = new Dictionary<string, int>
        {
            { "EURS", 1 },
            { "XCHF", 3 }
        };

        /// <summary>
        /// Dictionary to save StableCoins in different Markets
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, int>> _stableCoinsMarkets = new Dictionary<string, Dictionary<string, int>>
        {
            { Market.Binance , _stableCoinsBinance},
            { Market.Bitfinex , _stableCoinsBitfinex},
            { Market.GDAX , _stableCoinsGDAX},
        };

        /// <summary>
        /// Checks whether or not certain symbol is a StableCoin between a given market
        /// </summary>
        /// <param name="symbol">The symbol from which we want to know if it's a StableCoin</param>
        /// <param name="market">The market in which we want to search for that StaleCoin</param>
        /// <param name="quoteCurrency">If the symbol was indeed a StableCoin and this parameter is
        /// defined, it will check if the quote currency associated with the StableCoin is the
        /// same as the given in the parameters. Otherwise, it will just check whether or not the 
        /// given symbol is a StableCoin</param>
        /// <returns></returns>
        public static bool IsStableCoin(string symbol, string market, string quoteCurrency = null)
        {
            if (_stableCoinsMarkets.TryGetValue(market, out var stableCoins) && stableCoins.TryGetValue(symbol, out var index))
            {
                if (quoteCurrency == null)
                {
                    return true;
                }

                return quoteCurrency == _stableCoinsCurrencies[index];
            }
            return false;
        }

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
