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
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Crypto;

namespace QuantConnect.Util
{
    /// <summary>
    /// Utility methods for decomposing and comparing currency pairs
    /// </summary>
    public static class CurrencyPairUtil
    {
        private static readonly Lazy<SymbolPropertiesDatabase> SymbolPropertiesDatabase =
            new Lazy<SymbolPropertiesDatabase>(Securities.SymbolPropertiesDatabase.FromDataFolder);

        /// <summary>
        /// Tries to decomposes the specified currency pair into a base and quote currency provided as out parameters
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        /// <returns>True if was able to decompose the currency pair</returns>
        public static bool TryDecomposeCurrencyPair(Symbol currencyPair, out string baseCurrency, out string quoteCurrency)
        {
            baseCurrency = null;
            quoteCurrency = null;

            if (!IsValidSecurityType(currencyPair?.SecurityType, throwException: false))
            {
                return false;
            }

            try
            {
                DecomposeCurrencyPair(currencyPair, out baseCurrency, out quoteCurrency);
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        /// <param name="defaultQuoteCurrency">Optionally can provide a default quote currency</param>
        public static void DecomposeCurrencyPair(Symbol currencyPair, out string baseCurrency, out string quoteCurrency, string defaultQuoteCurrency = Currencies.USD)
        {
            IsValidSecurityType(currencyPair?.SecurityType, throwException: true);
            var securityType = currencyPair.SecurityType;

            if (securityType == SecurityType.Forex)
            {
                Forex.DecomposeCurrencyPair(currencyPair.Value, out baseCurrency, out quoteCurrency);
                return;
            }

            var symbolProperties = SymbolPropertiesDatabase.Value.GetSymbolProperties(
                currencyPair.ID.Market,
                currencyPair,
                currencyPair.SecurityType,
                defaultQuoteCurrency);

            if (securityType == SecurityType.Cfd)
            {
                Cfd.DecomposeCurrencyPair(currencyPair, symbolProperties, out baseCurrency, out quoteCurrency);
            }
            else
            {
                Crypto.DecomposeCurrencyPair(currencyPair, symbolProperties, out baseCurrency, out quoteCurrency);
            }
        }

        /// <summary>
        /// Checks whether a symbol is decomposable into a base and a quote currency
        /// </summary>
        /// <param name="currencyPair">The pair to check for</param>
        /// <returns>True if the pair can be decomposed into base and quote currencies, false if not</returns>
        public static bool IsForexDecomposable(string currencyPair)
        {
            return !string.IsNullOrEmpty(currencyPair) && currencyPair.Length == 6;
        }

        /// <summary>
        /// Checks whether a symbol is decomposable into a base and a quote currency
        /// </summary>
        /// <param name="currencyPair">The pair to check for</param>
        /// <returns>True if the pair can be decomposed into base and quote currencies, false if not</returns>
        public static bool IsDecomposable(Symbol currencyPair)
        {
            if (currencyPair == null)
            {
                return false;
            }

            if (currencyPair.SecurityType == SecurityType.Forex)
            {
                return currencyPair.Value.Length == 6;
            }

            if (currencyPair.SecurityType == SecurityType.Cfd || currencyPair.SecurityType == SecurityType.Crypto || currencyPair.SecurityType == SecurityType.CryptoFuture)
            {
                var symbolProperties = SymbolPropertiesDatabase.Value.GetSymbolProperties(
                    currencyPair.ID.Market,
                    currencyPair,
                    currencyPair.SecurityType,
                    Currencies.USD);

                return currencyPair.Value.EndsWith(symbolProperties.QuoteCurrency);
            }

            return false;
        }

        /// <summary>
        /// You have currencyPair AB and one known symbol (A or B). This function returns the other symbol (B or A).
        /// </summary>
        /// <param name="currencyPair">Currency pair AB</param>
        /// <param name="knownSymbol">Known part of the currencyPair (either A or B)</param>
        /// <returns>The other part of currencyPair (either B or A), or null if known symbol is not part of currencyPair</returns>
        public static string CurrencyPairDual(this Symbol currencyPair, string knownSymbol)
        {
            string baseCurrency;
            string quoteCurrency;

            DecomposeCurrencyPair(currencyPair, out baseCurrency, out quoteCurrency);

            return CurrencyPairDual(baseCurrency, quoteCurrency, knownSymbol);
        }

        /// <summary>
        /// You have currencyPair AB and one known symbol (A or B). This function returns the other symbol (B or A).
        /// </summary>
        /// <param name="baseCurrency">The base currency of the currency pair</param>
        /// <param name="quoteCurrency">The quote currency of the currency pair</param>
        /// <param name="knownSymbol">Known part of the currencyPair (either A or B)</param>
        /// <returns>The other part of currencyPair (either B or A), or null if known symbol is not part of the currency pair</returns>
        public static string CurrencyPairDual(string baseCurrency, string quoteCurrency, string knownSymbol)
        {
            if (baseCurrency == knownSymbol)
            {
                return quoteCurrency;
            }

            if (quoteCurrency == knownSymbol)
            {
                return baseCurrency;
            }

            return null;
        }

        /// <summary>
        /// Represents the relation between two currency pairs
        /// </summary>
        public enum Match
        {
            /// <summary>
            /// The two currency pairs don't match each other normally nor when one is reversed
            /// </summary>
            NoMatch,

            /// <summary>
            /// The two currency pairs match each other exactly
            /// </summary>
            ExactMatch,

            /// <summary>
            /// The two currency pairs are the inverse of each other
            /// </summary>
            InverseMatch
        }

        /// <summary>
        /// Returns how two currency pairs are related to each other
        /// </summary>
        /// <param name="pairA">The first pair</param>
        /// <param name="baseCurrencyB">The base currency of the second pair</param>
        /// <param name="quoteCurrencyB">The quote currency of the second pair</param>
        /// <returns>The <see cref="Match"/> member that represents the relation between the two pairs</returns>
        public static Match ComparePair(this Symbol pairA, string baseCurrencyB, string quoteCurrencyB)
        {
            var pairAValue = pairA.ID.Symbol;

            // Check for a stablecoin between the currencies
            if (TryDecomposeCurrencyPair(pairA, out var baseCurrencyA, out  var quoteCurrencyA))
            {
                var currencies = new string[] { baseCurrencyA, quoteCurrencyA, baseCurrencyB, quoteCurrencyB};
                var isThereAnyMatch = false;

                // Compute all the potential stablecoins
                var potentialStableCoins = new int[][] 
                {
                    new int[]{ 1, 3 },
                    new int[]{ 1, 2 },
                    new int[]{ 0, 3 },
                    new int[]{ 0, 2 }
                };

                foreach(var pair in potentialStableCoins)
                {
                    if (Currencies.IsStableCoinWithoutPair(currencies[pair[0]] + currencies[pair[1]], pairA.ID.Market)
                        || Currencies.IsStableCoinWithoutPair(currencies[pair[1]] + currencies[pair[0]], pairA.ID.Market))
                    {
                        // If there's a stablecoin between them, assign to currency in pair A the value
                        // of the currency in pair B 
                        currencies[pair[0]] = currencies[pair[1]];
                        isThereAnyMatch = true;
                    }
                }

                // Update the value of pairAValue if there was a match
                if (isThereAnyMatch)
                {
                    pairAValue = currencies[0] + currencies[1];
                }
            }

            if (pairAValue == baseCurrencyB + quoteCurrencyB)
            {
                return Match.ExactMatch;
            }
            
            if (pairAValue == quoteCurrencyB + baseCurrencyB)
            {
                return Match.InverseMatch;
            }

            return Match.NoMatch;
        }

        private static bool IsValidSecurityType(SecurityType? securityType, bool throwException)
        {
            if (securityType == null)
            {
                if (throwException)
                {
                    throw new ArgumentException("Currency pair must not be null");
                }
                return false;
            }

            if (securityType != SecurityType.Forex &&
                securityType != SecurityType.Cfd &&
                securityType != SecurityType.Crypto &&
                securityType != SecurityType.CryptoFuture)
            {
                if (throwException)
                {
                    throw new ArgumentException($"Unsupported security type: {securityType}");
                }
                return false;
            }

            return true;
        }
    }
}
