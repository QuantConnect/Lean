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

//https://api.kraken.com/0/public/AssetPairs // API FOR ASSET PAIRS
namespace QuantConnect.Brokerages.Kraken {
    /// <summary>
    /// Provides the mapping between Lean symbols and Kraken symbols.
    /// </summary>
    public class KrakenSymbolMapper : ISymbolMapper {
        private bool HasLoadedSymbolsFromApi = false;
        private KrakenRestApi _restApi = null;

        /// <summary>
        /// Updates all Kraken Symbols
        /// </summary>
        /// <param name="restApi">Existing KrakenRestApi object</param>
        /*public void UpdateSymbols(KrakenRestApi restApi) {

            Dictionary<string, DataType.AssetPair> dict = restApi.GetAssetPairs();

            foreach (KeyValuePair<string, DataType.AssetPair> pair in dict) {

                // clear previous symbols
                KnownSymbolStrings.Clear();
                KrakenCurrencies.Clear();

                string currencyPair = pair.Key;

                DataType.AssetPair assetPair = pair.Value;

                string baseName = assetPair.AclassBase;
                string quoteName = assetPair.AclassQuote;

                KnownSymbolStrings.Add(currencyPair);

                KrakenCurrencies.Add(baseName);
                KrakenCurrencies.Add(quoteName);
            }
        }*/


        private static readonly Dictionary<string, string> ToKrakenSymbol = new Dictionary<string, string>() {
            
            { "ETHBTC", "XETHXXBT" },
            { "ETHEUR", "XETHZEUR" },
            { "ETHUSD", "XETHZUSD" },
            { "ICNETH", "XICNXETH" },
            { "ICNBTC", "XICNXXBT" },
            { "LTCXBT", "XLTCXXBT" },
            { "LTCEUR", "XLTCZEUR" },
            { "LTCUSD", "XLTCZUSD" },
            { "BTCEUR", "XXBTZEUR" },
            { "BTCUSD", "XXBTZUSD" },
            { "XLMEUR", "XXLMZEUR" },
            { "XLMBTC", "XXLMXXBT" },
            { "XLMUSD", "XXLMZUSD" },
        };
        
        static KrakenSymbolMapper() {

            foreach (var pair in ToKrakenSymbol)
                ToLeanSymbol.Add(pair.Value, pair.Key);
        }

        private static readonly Dictionary<string, string> ToLeanSymbol = new Dictionary<string, string>();

        /// <summary>
        /// Returns array with length of 2. 0 = base currency, 1 = quote currency
        /// </summary>
        /// <param name="symbolString">Source symbol</param>
        /// <returns></returns>
        private static string[] SplitSymbol(string symbolString) {

            int halfLength = (int) Math.Ceiling(symbolString.Length / 2f);

            return new string[] {

                symbolString.Substring(0, halfLength),
                symbolString.Substring(halfLength, halfLength)
            };
        }

        /// <summary>
        /// Converts a Lean symbol instance to an Oanda symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Kraken symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToKrakenSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Kraken symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Kraken symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid Kraken symbol: " + brokerageSymbol);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown Kraken symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + securityType);

            if (market != Market.Kraken)
                throw new ArgumentException("Invalid market: " + market);

            return Symbol.Create(ConvertKrakenSymbolToLeanSymbol(brokerageSymbol), SecurityType.Crypto, Market.Kraken);
        }

        /// <summary>
        /// Gets pair from currency A and currency B
        /// </summary>
        /// <param name="currencyA">First currency</param>
        /// <param name="currencyB">Second currency</param>
        /// <returns>Returns pair (string: pair, bool: is currencyA first) </pair></returns>
        public KeyValuePair<string, bool> GetPair(string currencyA, string currencyB) {

            if (currencyA.Length == 0 || currencyB.Length == 0)
                throw new DataType.KrakenException("No emtpy strings!");

            foreach (string krakenPair in ToLeanSymbol.Keys) {

                int A = krakenPair.IndexOf(currencyA);
                int B = krakenPair.IndexOf(currencyB);

                if (A >= 0 && B >= 0) {

                    return new KeyValuePair<string, bool>(krakenPair, A < B);
                }    
            }

            throw new DataType.KrakenException("Pair not found!");
        }
        /// <summary>
        /// Checks if the symbol is supported by Kraken
        /// </summary>
        /// <param name="brokerageSymbol">The Kraken symbol</param>
        /// <returns>True if Kraken supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol) {
            return ToLeanSymbol.ContainsKey(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Kraken
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Kraken supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol) {
            return ToKrakenSymbol.ContainsKey(symbol.ID.Symbol);
        }

        /// <summary>
        /// Converts an Kraken symbol to a Lean symbol string
        /// </summary>
        private static string ConvertKrakenSymbolToLeanSymbol(string krakenSymbol)
        {
            try {
                return ToLeanSymbol[krakenSymbol];
            }
            catch {
                throw new DataType.KrakenException("Unknown Kraken Symbol");
            }
        }


        /// <summary>
        /// Converts a Lean symbol string to an Kraken symbol
        /// </summary>
        private static string ConvertLeanSymbolToKrakenSymbol(string leanSymbol)
        {
            try {
                return ToKrakenSymbol[leanSymbol];
            }
            catch {
                throw new DataType.KrakenException("Unknown Lean Symbol - unsupported by Kraken");
            }

        }
    }
}
