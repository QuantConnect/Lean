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
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Util
{
    public static class CurrencyPairUtil
    {
        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters.
        /// Requires symbols in Currencies.CurrencySymbols dictionary to make accurate splits, important for crypto-currency symbols.
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed, for example, "EURUSD"</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        /// <param name="securityType">Type of security</param>
        public static void DecomposeCurrencyPair(string currencyPair, out string baseCurrency, out string quoteCurrency, SecurityType securityType = SecurityType.Base)
        {
            if (securityType == SecurityType.Forex && (currencyPair == null || currencyPair.Length != 6))
            {
                throw new ArgumentException($"Forex currency pair must not be null and length must be fixed at 6. Problematic pair: {currencyPair}");
            }
            else if (currencyPair == null || currencyPair.Length < 6 || currencyPair.Length > Currencies.MaxCharactersPerCurrencyPair)
            {
                throw new ArgumentException($"Currency pairs must not be null, length minimum of 6 and maximum of {Currencies.MaxCharactersPerCurrencyPair}. Problematic pair: {currencyPair}");
            }

            if (currencyPair.Length == 6)
            {
                // Old-code part for Forex (non-crypto) markets only.
                baseCurrency = currencyPair.Substring(0, 3);
                quoteCurrency = currencyPair.Substring(3);
                return;
            }

            baseCurrency = null;
            quoteCurrency = null;

            var bases  = new List<string>();
            var quotes = new List<string>();

            // Find bases
            foreach (var symbol in Currencies.CurrencySymbols.Keys)
            {
                if (currencyPair.IndexOf(symbol) == 0)
                {
                    bases.Add(symbol);
                }
            }

            // Find quotes
            foreach (var symbol in Currencies.CurrencySymbols.Keys)
            {
                if (currencyPair.Contains(symbol))
                {
                    int start = currencyPair.IndexOf(symbol, 3);

                    if (start >= 3 && start <= Currencies.MaxCharactersPerCurrencyCode)
                    {
                        quotes.Add(symbol);
                    }
                }
            }

            // Make combinations (combined) and compare to currencyPair
            // When 100% match found, break the loop.
            foreach (var b in bases)
            {
                foreach (var q in quotes)
                {
                    var combined = b + q;

                    if (combined.Equals(currencyPair))
                    {
                        baseCurrency = b;
                        quoteCurrency = q;
                        // Return, since if we came to this point, there was found atleast 1 base and 1 count, that matches original currencyPair
                        return;
                    }
                }
            }

            if (bases.Count == 0)
            {
                throw new ArgumentException($"No base currency found for the pair: {currencyPair}");
            }
            else if (quotes.Count == 0)
            {
                throw new ArgumentException($"No quote currency found for the pair: {currencyPair}");
            }

        }

        /// <summary>
        /// You have currencyPair AB and one known symbol (A or B). This function returns another one (B or A).
        /// </summary>
        /// <param name="currencyPair">Currency pair AB</param>
        /// <param name="knownSymbol">Known part of the currencyPair (either A or B)</param>
        /// <param name="securityType">Type of security</param>
        /// <returns>Returns other part of currencyPair (either B or A)</returns>
        public static string CurrencyPairDual(this string currencyPair, string knownSymbol, SecurityType securityType = SecurityType.Base)
        {
            string CurrencyA = null;
            string CurrencyB = null;

            DecomposeCurrencyPair(currencyPair, out CurrencyA, out CurrencyB, securityType);

            if (CurrencyA == knownSymbol)
            {
                return CurrencyB;
            }
            else if (CurrencyB == knownSymbol)
            {
                return CurrencyA;
            }
            else
            {
                throw new ArgumentException($"The knownSymbol {knownSymbol} isn't contained in currencyPair {currencyPair}.");
            }
        }

        public enum Match
        {
            /// <summary>
            /// No match was found
            /// </summary>
            NoMatch,

            /// <summary>
            /// Pair was found exact as it is
            /// </summary>
            ExactMatch,

            /// <summary>
            /// Only inverse pair was found
            /// </summary>
            InverseMatch
        }

        public static Match ComparePair(this string pairA, string pairB)
        {
            if (pairA == pairB)
            {
                return Match.ExactMatch;
            }

            string baseA;
            string quoteA;

            DecomposeCurrencyPair(pairA, out baseA, out quoteA);

            string baseB;
            string quoteB;

            DecomposeCurrencyPair(pairB, out baseB, out quoteB);

            if(baseA == quoteB && baseB == quoteA)
            {
                return Match.InverseMatch;
            }

            return Match.NoMatch;
        }

        public static Match ComparePair(this string pairA, string baseB, string quoteB)
        {
            if (pairA == baseB + quoteB)
            {
                return Match.ExactMatch;
            }

            string baseA;
            string quoteA;

            DecomposeCurrencyPair(pairA, out baseA, out quoteA);

            if (baseA == quoteB && baseB == quoteA)
            {
                return Match.InverseMatch;
            }

            return Match.NoMatch;
        }

        public static bool PairContainsCode(this string pair, string code)
        {
            string baseCode;
            string quoteCode;

            DecomposeCurrencyPair(pair, out baseCode, out quoteCode);

            return baseCode == code || quoteCode == code;
        }

    }
}
