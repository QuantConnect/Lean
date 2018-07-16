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
namespace QuantConnect.Brokerages.Kraken
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Kraken symbols.
    /// </summary>
    public class KrakenSymbolMapper : ISymbolMapper
    {
        private class KrakenAsset
        {   // currency code
            public string Code;
            public string CodeAlt;
        }

        private object KnownAssetsLock = new object();
        private List<KrakenAsset> KnownAssets = new List<KrakenAsset>();

        private List<string> KnownPairs = new List<string>();
        private List<DataType.AssetPair> KnownPairsWhole = new List<DataType.AssetPair>();

        private static List<string> Prefixes = new List<string>() { "", "Z", "X" };

        /// <summary>
        /// Updates all Kraken Symbols
        /// </summary>
        /// <param name="restApi">Existing KrakenRestApi object</param>
        public void UpdateSymbols(KrakenRestApi restApi)
        {
            //Logging.Log.Trace("UpdateSymbols(restApi)");

            Dictionary<string, DataType.AssetInfo> assetInfoDict = restApi.GetAssetInfo();
            Dictionary<string, DataType.AssetPair> assetPairDict = restApi.GetAssetPairs();

            lock (KnownAssetsLock)
            {
                KnownAssets.Clear();

                foreach (KeyValuePair<string, DataType.AssetInfo> KVPair in assetInfoDict)
                {
                    // Not a symbol we are interested in
                    // https://support.kraken.com/hc/en-us/articles/204799657-What-are-Kraken-fee-credits-KFEE-
                    if (KVPair.Key == "KFEE")
                        continue;

                    KrakenAsset krakenAsset = new KrakenAsset() { Code = KVPair.Key, CodeAlt = KVPair.Value.Altname };

                    KnownAssets.Add(krakenAsset);

                    //Logging.Log.Trace($"KrakenAsset {{ Code: { KVPair.Key }, CodeAlt: { KVPair.Value.Altname } }}");
                }
            }

            KnownPairs.Clear();
            KnownPairsWhole.Clear();

            foreach (KeyValuePair<string, DataType.AssetPair> KVPair in assetPairDict)
            {
                // remove dark pool markets
                if (KVPair.Key.Contains(".d"))
                    continue;

                // KrakenPair krakenPair = new KrakenPair() { Pair = KVPair.Key, PairAlt = KVPair.Value.Altname };
                KnownPairs.Add(KVPair.Key);
                KnownPairsWhole.Add(KVPair.Value);

                //Logging.Log.Trace($"KnownPair {KVPair.Key}");
            }
        }

        private void DecomposeKrakenPair(string krakenPair, out string baseCode, out string quoteCode)
        {
            //Logging.Log.Trace($"DecomposeKrakenPair({krakenPair}, out baseCode, out quoteCode)");

            List<string> foundCodes = new List<string>();

            lock (KnownAssetsLock)
            {
                foreach (var krakenCode in KnownAssets)
                {
                    if (krakenPair.Contains(krakenCode.Code))
                    {
                        foundCodes.Add(krakenCode.Code);
                    }

                    if (krakenCode.Code != krakenCode.CodeAlt && krakenPair.Contains(krakenCode.CodeAlt))
                    {
                        foundCodes.Add(krakenCode.CodeAlt);
                    }
                }
            }

            foreach(string b in foundCodes)
            {
                foreach (string q in foundCodes)
                {
                    if(b + q == krakenPair)
                    {
                        baseCode  = b;
                        quoteCode = q;

                        return;
                    }
                }
            }

            throw new Kraken.DataType.KrakenException($"Decomposing kraken pair {krakenPair} unsucessful");
        }

        /// <summary>
        /// Gets pair from currency A and currency B
        /// </summary>
        /// <param name="currencyA">First currency</param>
        /// <param name="currencyB">Second currency</param>
        /// <returns>Returns pair (string: pair, bool: is currencyA first) </pair></returns>
        public string GetPair(string currencyA, string currencyB)
        {
            //Logging.Log.Trace($"GetPair({currencyA},{currencyB})");

            if (string.IsNullOrEmpty(currencyA) || string.IsNullOrEmpty(currencyB))
                throw new DataType.KrakenException("No emtpy strings!");

            string strippedA = StripPrefixes(currencyA);
            string strippedB = StripPrefixes(currencyB);

            if (string.IsNullOrEmpty(strippedA) || string.IsNullOrEmpty(strippedB))
                throw new DataType.KrakenException("No empty strings!");

            foreach (string krakenPair in KnownPairs)
            {
                int A = krakenPair.IndexOf(strippedA);
                int B = krakenPair.IndexOf(strippedB);

                if (A >= 0 && B >= 0)
                {
                    if (A < B)
                        return krakenPair;
                }
            }

            throw new DataType.KrakenException($"Pair not found for {currencyA} {currencyB} pair");
        }

        private string LeanCodeToKrakenCode(string leanCode)
        {
            //Logging.Log.Trace($"LeanCodeToKrakenCode({leanCode})");

            if (leanCode == "BTC")
                leanCode = "XBT";

            foreach (var prefix in Prefixes)
            {
                string possibleCode = prefix + leanCode;

                lock (KnownAssetsLock)
                {
                    if (KnownAssets.Exists(krakenAsset => krakenAsset.Code == possibleCode || krakenAsset.CodeAlt == possibleCode))
                    {
                        return possibleCode;
                    }
                }
            }

            throw new DataType.KrakenException($"No possible Kraken code found for {leanCode}");
        }

        public string KrakenToLeanCode(string krakenCode)
        {
            //Logging.Log.Trace($"KrakenToLeanCode({krakenCode})");

            krakenCode = StripPrefixes(krakenCode);

            if (krakenCode == "XBT")
                krakenCode = "BTC";

            return krakenCode;
        }

        /// <summary>
        /// Converts a Lean symbol instance to an Kraken symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Kraken symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            //Logging.Log.Trace($"GetBrokerageSymbol({symbol.Value})");

            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            string baseCode  = null;
            string quoteCode = null;

            Util.CurrencyPairUtil.DecomposeCurrencyPair(symbol.Value.ToUpper(), out baseCode, out quoteCode);

            baseCode = LeanCodeToKrakenCode(baseCode);
            quoteCode = LeanCodeToKrakenCode(quoteCode);

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            foreach(var prefixA in Prefixes)
            {
                foreach (var prefixB in Prefixes)
                {
                    builder.Append(prefixA);
                    builder.Append(baseCode);
                    builder.Append(prefixB);
                    builder.Append(quoteCode);

                    string possibleMatch = builder.ToString();
                    builder.Clear();

                    if(KnownPairs.Contains(possibleMatch)) {
                        return possibleMatch;
                    }
                }
            }

            throw new ArgumentException($"Unknown LEAN symbol: {symbol.Value}");
        }

        private string StripPrefixes(string currencyCode)
        {
            currencyCode = currencyCode.ToUpper();

            //Logging.Log.Trace($"StripPrefixes({currencyCode})");

            string firstChar = currencyCode[0].ToString();

            if (Prefixes.Exists(prefix => firstChar.Equals(prefix)))
            {
                var noPrefixes = currencyCode.Substring(1, currencyCode.Length - 1);

                if (noPrefixes.Length > 2) {
                    return noPrefixes;
                }
            }
            return currencyCode;
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
            //Logging.Log.Trace($"GetLeanSymbol({brokerageSymbol}, {securityType}, {market}, ... )");

            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid Kraken symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + securityType);

            if (market != Market.Kraken)
                throw new ArgumentException("Invalid market: " + market);

            string krakenBase = null;
            string krakenQuote = null;

            DecomposeKrakenPair(brokerageSymbol, out krakenBase, out krakenQuote);

            krakenBase = StripPrefixes(krakenBase);
            krakenQuote = StripPrefixes(krakenQuote);

            krakenBase = KrakenToLeanCode(krakenBase);
            krakenQuote = KrakenToLeanCode(krakenQuote);

            string leanSymbol = krakenBase + krakenQuote;

            if(Currencies.CryptoCurrencyPairs.Contains(leanSymbol) || Currencies.CurrencyPairs.Contains(leanSymbol))
            {
                return Symbol.Create(leanSymbol, SecurityType.Crypto, Market.Kraken);
            }

            throw new DataType.KrakenException("Converting Kraken symbol to Lean was unsucessful");
        }

    }
}
