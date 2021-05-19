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
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;

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
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        public static void DecomposeCurrencyPair(Symbol currencyPair, out string baseCurrency, out string quoteCurrency)
        {
            if (currencyPair == null)
            {
                throw new ArgumentException("Currency pair must not be null");
            }

            var securityType = currencyPair.SecurityType;

            if (securityType != SecurityType.Forex &&
                securityType != SecurityType.Cfd &&
                securityType != SecurityType.Crypto)
            {
                throw new ArgumentException($"Unsupported security type: {securityType}");
            }

            if (securityType == SecurityType.Forex)
            {
                Forex.DecomposeCurrencyPair(currencyPair.Value, out baseCurrency, out quoteCurrency);
                return;
            }

            var symbolProperties = SymbolPropertiesDatabase.Value.GetSymbolProperties(
                currencyPair.ID.Market,
                currencyPair,
                currencyPair.SecurityType,
                Currencies.USD);

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

            if (currencyPair.SecurityType == SecurityType.Cfd || currencyPair.SecurityType == SecurityType.Crypto)
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
            if (pairA.Value == baseCurrencyB + quoteCurrencyB)
            {
                return Match.ExactMatch;
            }

            if (pairA.Value == quoteCurrencyB + baseCurrencyB)
            {
                return Match.InverseMatch;
            }

            return Match.NoMatch;
        }
    }
}
