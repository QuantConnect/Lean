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
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Util
{
    public static class CurrencyPairUtil
    {
        private static Lazy<SymbolPropertiesDatabase> symbolPropertiesDatabase = new Lazy<SymbolPropertiesDatabase>(SymbolPropertiesDatabase.FromDataFolder);

        /// <summary>
        /// Decomposes the specified currency pair into a base and quote currency provided as out parameters.
        /// Requires symbols in Currencies.CurrencySymbols dictionary to make accurate splits, important for crypto-currency symbols.
        /// </summary>
        /// <param name="currencyPair">The input currency pair to be decomposed, for example, "EURUSD"</param>
        /// <param name="baseCurrency">The output base currency</param>
        /// <param name="quoteCurrency">The output quote currency</param>
        public static void DecomposeCurrencyPair(Symbol currencyPair, out string baseCurrency, out string quoteCurrency)
        {
            if (currencyPair == null)
            {
                throw new ArgumentException($"Currency pair must not be null");
            }

            if (currencyPair.SecurityType == SecurityType.Crypto)
            {
                var symbolProperties = symbolPropertiesDatabase.Value.GetSymbolProperties(
                    currencyPair.ID.Market,
                    currencyPair,
                    currencyPair.SecurityType,
                    "USD");
                Crypto.DecomposeCurrencyPair(currencyPair, symbolProperties, out baseCurrency, out quoteCurrency);
            }
            else
            {
                Forex.DecomposeCurrencyPair(currencyPair.Value, out baseCurrency, out quoteCurrency);
            }
        }

        /// <summary>
        /// You have currencyPair AB and one known symbol (A or B). This function returns another one (B or A).
        /// </summary>
        /// <param name="currencyPair">Currency pair AB</param>
        /// <param name="knownSymbol">Known part of the currencyPair (either A or B)</param>
        /// <returns>Returns other part of currencyPair (either B or A)</returns>
        public static string CurrencyPairDual(this Symbol currencyPair, string knownSymbol)
        {
            string CurrencyA = null;
            string CurrencyB = null;

            DecomposeCurrencyPair(currencyPair, out CurrencyA, out CurrencyB);

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

        public static Match ComparePair(this Symbol pairA, Symbol pairB)
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

        public static Match ComparePair(this Symbol pairA, string baseB, string quoteB)
        {
            if (pairA.Value == baseB + quoteB)
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

        public static bool PairContainsCode(this Symbol pair, string code)
        {
            string baseCode;
            string quoteCode;

            DecomposeCurrencyPair(pair, out baseCode, out quoteCode);

            return baseCode == code || quoteCode == code;
        }

    }
}
