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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

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
        /// JPY (Japanese yen) currency string
        /// </summary>
        public const string JPY = "JPY";

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
            {JPY, "¥"},
            {EUR, "€"},
            {"NZD", "$"},
            {"AUD", "$"},
            {"CAD", "$"},
            {CHF, "Fr"},
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
            {"RUB", "₽"},
            {"BRL", "R$"},
            {"GNF", "Fr"},
            {IDR, "Rp"},

            {"BTC", "₿"},
            {"BCH", "Ƀ"},
            {"BSV", "Ɓ"},
            {"LTC", "Ł"},
            {"ETH", "Ξ"},
            {"EOS", "ε"},
            {"XRP", "✕"},
            {"XLM", "*"},
            {"ETC", "ξ"},
            {"ZRX", "ZRX"},
            {"USDT", "₮"},
            {"ADA", "₳"},
            {"SOL", "◎"},
            {"DOT", "●"},
            {"DOGE", "Ð"},
            {"DAI", "◈"},
            {"ALGO", "Ⱥ"},
            {"ICP", "∞"},
            {"XMR", "ɱ"},
            {"XTZ", "ꜩ"},
            {"IOTA", "ɨ"},
            {"MIOTA", "ɨ"},
            {"MKR", "Μ"},
            {"ZEC", "ⓩ"},
            {"DASH", "Đ"},
            {"XNO", "Ӿ"},
            {"REP", "Ɍ"},
            {"STEEM", "ȿ"},
            {"THETA", "ϑ"},
            {"FIL", "⨎"},
            {"BAT", "⟁"},
            {"LSK", "Ⱡ"},
            {"NAV", "Ꞥ"}
        };

        /// <summary>
        /// Stable pairs in GDAX. We defined them because they have different fees in GDAX market
        /// </summary>
        [Obsolete("StablePairsGDAX is deprecated. Use StablePairsCoinbase instead.")]
        public static readonly HashSet<string> StablePairsGDAX = StablePairsCoinbase;

        /// <summary>
        /// Stable pairs in Coinbase. We defined them because they have different fees in Coinbase market
        /// </summary>
        public static readonly HashSet<string> StablePairsCoinbase = new()
        {
            "DAIUSDC",
            "DAIUSD",
            "GYENUSD",
            "PAXUSD",
            "PAXUSDT",
            "MUSDUSD",
            "USDCEUR",
            "USDCGBP",
            "USDTEUR",
            "USDTGBP",
            "USDTUSD",
            "USDTUSDC",
            "USTEUR",
            "USTUSD",
            "USTUSDT",
            "WBTCBTC"
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Coinbase market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly HashSet<string> _stableCoinsWithoutPairsCoinbase = new HashSet<string>
        {
            "USDCUSD"
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Binance market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly HashSet<string> _stableCoinsWithoutPairsBinance = new HashSet<string>
        {
            "USDCUSD",
            "USDTUSD",
            "USDPUSD",
            "SUSDUSD",
            "BUSDUSD",
            "USTUSD",
            "TUSDUSD",
            "FDUSDUSD",
            "DAIUSD",
            "IDRTIDR",
            "BNFCRUSD"
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Bitfinex market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly HashSet<string> _stableCoinsWithoutPairsBitfinex = new HashSet<string>
        {
            "EURSEUR",
            "XCHFCHF"
        };

        /// <summary>
        /// Define some StableCoins that don't have direct pairs for base currencies in our SPDB in Binance market
        /// This is because some CryptoExchanges do not define direct pairs with the stablecoins they offer.
        ///
        /// We use this to allow setting cash amounts for these stablecoins without needing a conversion
        /// security.
        /// </summary>
        private static readonly HashSet<string> _stableCoinsWithoutPairsBybit = new HashSet<string>
        {
            "USDCUSD",
            "USDTUSD",
            "USDPUSD",
            "SUSDUSD",
            "BUSDUSD",
            "USTUSD",
            "TUSDUSD",
            "DAIUSD"
        };

        /// <summary>
        /// Dictionary to save StableCoins in different Markets
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> _stableCoinsWithoutPairsMarkets = new Dictionary<string, HashSet<string>>
        {
            { Market.Binance , _stableCoinsWithoutPairsBinance},
            { Market.Bitfinex , _stableCoinsWithoutPairsBitfinex},
            { Market.Coinbase, _stableCoinsWithoutPairsCoinbase},
            { Market.Bybit , _stableCoinsWithoutPairsBybit},
        };

        /// <summary>
        /// Checks whether or not certain symbol is a StableCoin without pair in a given market
        /// </summary>
        /// <param name="symbol">The Symbol from wich we want to know if it's a StableCoin without pair</param>
        /// <param name="market">The market in which we want to search for that StableCoin</param>
        /// <returns>True if the given symbol is a StableCoin without pair in the given market</returns>
        public static bool IsStableCoinWithoutPair(string symbol, string market)
        {
            if (_stableCoinsWithoutPairsMarkets.TryGetValue(market, out var stableCoins) && stableCoins.Contains(symbol))
            {
                return true;
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
            if (string.IsNullOrEmpty(currency))
            {
                return string.Empty;
            }

            return CurrencySymbols.TryGetValue(currency, out var currencySymbol) ? currencySymbol : currency;
        }

        /// <summary>
        /// Converts the string representation of number with currency in the format {currency}{value} to its decimal equivalent.
        /// It throws if the value cannot be converted to a decimal number.
        /// </summary>
        /// <param name="value">The value with currency</param>
        /// <returns>The decimal equivalent to the value</returns>
        public static decimal Parse(string value)
        {
            decimal parsedValue;

            if (!TryParse(value, out parsedValue))
            {
                throw new ArgumentException(Messages.Currencies.FailedConversionToDecimal(value));
            }

            return parsedValue;
        }

        /// <summary>
        /// Converts the string representation of number with currency in the format {currency}{value} to its decimal equivalent.
        /// </summary>
        /// <param name="value">The value with currency</param>
        /// <param name="parsedValue">The decimal equivalent to the string value after conversion</param>
        /// <returns>True if the value was succesfuly converted</returns>
        public static bool TryParse(string value, out decimal parsedValue)
        {
            // Strip out the currency (any character before the first number) ignoring blank spaces since they are not supposed to be in numbers with currency
            value = Regex.Replace(value, @"^[^\d\s-+]+", string.Empty);
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue);
        }
    }
}
