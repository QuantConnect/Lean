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
using QuantConnect.Brokerages;

namespace QuantConnect.ToolBox.DukascopyDownloader
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Dukascopy symbols.
    /// </summary>
    public class DukascopySymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Helper class to allow collection initializer on a List of tuples
        /// </summary>
        private class TupleList<T1, T2, T3> : List<Tuple<T1, T2, T3>>
        {
            public void Add(T1 item1, T2 item2, T3 item3)
            {
                Add(new Tuple<T1, T2, T3>(item1, item2, item3));
            }
        }

        /// <summary>
        /// The list of mappings from Dukascopy symbols to Lean symbols.
        /// </summary>
        /// <remarks>T1 is Dukascopy symbol, T2 is Lean symbol, T3 is point value (used by downloader)</remarks>
        private static readonly TupleList<string, string, int> DukascopySymbolMappings = new TupleList<string, string, int>
        {
            { "AUDCAD", "AUDCAD", 100000 },
            { "AUDCHF", "AUDCHF", 100000 },
            { "AUDJPY", "AUDJPY", 1000 },
            { "AUDNZD", "AUDNZD", 100000 },
            { "AUDSGD", "AUDSGD", 100000 },
            { "AUDUSD", "AUDUSD", 100000 },
            { "AUSIDXAUD", "AU200AUD", 1000 },
            { "BRAIDXBRL", "BRIDXBRL", 1000 },
            { "BRENTCMDUSD", "BCOUSD", 1000 },
            { "CADCHF", "CADCHF", 100000 },
            { "CADHKD", "CADHKD", 100000 },
            { "CADJPY", "CADJPY", 1000 },
            { "CHEIDXCHF", "CH20CHF", 1000 },
            { "CHFJPY", "CHFJPY", 1000 },
            { "CHFPLN", "CHFPLN", 100000 },
            { "CHFSGD", "CHFSGD", 100000 },
            { "COPPERCMDUSD", "XCUUSD", 1000 },
            { "DEUIDXEUR", "DE30EUR", 1000 },
            { "ESPIDXEUR", "ES35EUR", 1000 },
            { "EURAUD", "EURAUD", 100000 },
            { "EURCAD", "EURCAD", 100000 },
            { "EURCHF", "EURCHF", 100000 },
            { "EURDKK", "EURDKK", 100000 },
            { "EURGBP", "EURGBP", 100000 },
            { "EURHKD", "EURHKD", 100000 },
            { "EURHUF", "EURHUF", 1000 },
            { "EURJPY", "EURJPY", 1000 },
            { "EURMXN", "EURMXN", 100000 },
            { "EURNOK", "EURNOK", 100000 },
            { "EURNZD", "EURNZD", 100000 },
            { "EURPLN", "EURPLN", 100000 },
            { "EURRUB", "EURRUB", 100000 },
            { "EURSEK", "EURSEK", 100000 },
            { "EURSGD", "EURSGD", 100000 },
            { "EURTRY", "EURTRY", 100000 },
            { "EURUSD", "EURUSD", 100000 },
            { "EURZAR", "EURZAR", 100000 },
            { "EUSIDXEUR", "EU50EUR", 1000 },
            { "FRAIDXEUR", "FR40EUR", 1000 },
            { "GBPAUD", "GBPAUD", 100000 },
            { "GBPCAD", "GBPCAD", 100000 },
            { "GBPCHF", "GBPCHF", 100000 },
            { "GBPJPY", "GBPJPY", 1000 },
            { "GBPNZD", "GBPNZD", 100000 },
            { "GBPUSD", "GBPUSD", 100000 },
            { "GBRIDXGBP", "UK100GBP", 1000 },
            { "HKDJPY", "HKDJPY", 100000 },
            { "HKGIDXHKD", "HK33HKD", 1000 },
            { "ITAIDXEUR", "IT40EUR", 1000 },
            { "JPNIDXJPY", "JP225JPY", 1000 },
            { "LIGHTCMDUSD", "WTICOUSD", 1000 },
            { "MXNJPY", "MXNJPY", 1000 },
            { "NGASCMDUSD", "NATGASUSD", 1000 },
            { "NLDIDXEUR", "NL25EUR", 1000 },
            { "NZDCAD", "NZDCAD", 100000 },
            { "NZDCHF", "NZDCHF", 100000 },
            { "NZDJPY", "NZDJPY", 1000 },
            { "NZDSGD", "NZDSGD", 100000 },
            { "NZDUSD", "NZDUSD", 100000 },
            { "PDCMDUSD", "XPDUSD", 1000 },
            { "PTCMDUSD", "XPTUSD", 1000 },
            { "SGDJPY", "SGDJPY", 1000 },
            { "USA30IDXUSD", "US30USD", 1000 },
            { "USA500IDXUSD", "SPX500USD", 1000 },
            { "USATECHIDXUSD", "NAS100USD", 1000 },
            { "USDBRL", "USDBRL", 100000 },
            { "USDCAD", "USDCAD", 100000 },
            { "USDCHF", "USDCHF", 100000 },
            { "USDCNH", "USDCNY", 100000 },
            { "USDDKK", "USDDKK", 100000 },
            { "USDHKD", "USDHKD", 100000 },
            { "USDHUF", "USDHUF", 1000 },
            { "USDJPY", "USDJPY", 1000 },
            { "USDMXN", "USDMXN", 100000 },
            { "USDNOK", "USDNOK", 100000 },
            { "USDPLN", "USDPLN", 100000 },
            { "USDRUB", "USDRUB", 100000 },
            { "USDSEK", "USDSEK", 100000 },
            { "USDSGD", "USDSGD", 100000 },
            { "USDTRY", "USDTRY", 100000 },
            { "USDZAR", "USDZAR", 100000 },
            { "XAGUSD", "XAGUSD", 1000 },
            { "XAUUSD", "XAUUSD", 1000 },
            { "ZARJPY", "ZARJPY", 100000 }
        };

        private static readonly Dictionary<string, string> MapDukascopyToLean = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> MapLeanToDukascopy = new Dictionary<string, string>();
        private static readonly Dictionary<string, int> PointValues = new Dictionary<string, int>();

        /// <summary>
        /// The list of known Dukascopy currencies.
        /// </summary>
        private static readonly HashSet<string> KnownCurrencies = new HashSet<string>
        {
            "AUD", "BRL", "CAD", "CHF", "CNH", "DKK", "EUR", "GBP", "HKD", "HUF", "JPY", "MXN", "NOK", "NZD", "PLN", "RUB", "SEK", "SGD", "TRY", "USD", "ZAR"
        };

        /// <summary>
        /// Static constructor for the <see cref="DukascopySymbolMapper"/> class
        /// </summary>
        static DukascopySymbolMapper()
        {
            foreach (var mapping in DukascopySymbolMappings)
            {
                MapDukascopyToLean[mapping.Item1] = mapping.Item2;
                MapLeanToDukascopy[mapping.Item2] = mapping.Item1;
                PointValues[mapping.Item2] = mapping.Item3;
            }
        }

        /// <summary>
        /// Converts a Lean symbol instance to a Dukascopy symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Dukascopy symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToDukascopySymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts a Dukascopy symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Dukascopy symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid Dukascopy symbol: " + brokerageSymbol);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown Dukascopy symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Forex && securityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + securityType);

            if (market != Market.Dukascopy)
                throw new ArgumentException("Invalid market: " + market);

            return Symbol.Create(ConvertDukascopySymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Dukascopy);
        }

        /// <summary>
        /// Returns the security type for a Dukascopy symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Dukascopy symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            return (brokerageSymbol.Length == 6 && KnownCurrencies.Contains(brokerageSymbol.Substring(0, 3)) && KnownCurrencies.Contains(brokerageSymbol.Substring(3, 3)))
                ? SecurityType.Forex
                : SecurityType.Cfd;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            string dukascopySymbol;
            if (!MapLeanToDukascopy.TryGetValue(leanSymbol, out dukascopySymbol))
                throw new ArgumentException("Unknown Lean symbol: " + leanSymbol);

            return GetBrokerageSecurityType(dukascopySymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Dukascopy
        /// </summary>
        /// <param name="brokerageSymbol">The Dukascopy symbol</param>
        /// <returns>True if Dukascopy supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            return brokerageSymbol != null && MapDukascopyToLean.ContainsKey(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Dukascopy
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Dukascopy supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                return false;

            var dukascopySymbol = ConvertLeanSymbolToDukascopySymbol(symbol.Value);

            return MapDukascopyToLean.ContainsKey(dukascopySymbol) && GetBrokerageSecurityType(dukascopySymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Returns the point value for a Lean symbol
        /// </summary>
        public int GetPointValue(Symbol symbol)
        {
            return PointValues[symbol.Value];
        }

        /// <summary>
        /// Converts a Dukascopy symbol to a Lean symbol string
        /// </summary>
        private static string ConvertDukascopySymbolToLeanSymbol(string dukascopySymbol)
        {
            string leanSymbol;
            return MapDukascopyToLean.TryGetValue(dukascopySymbol, out leanSymbol) ? leanSymbol : string.Empty;
        }

        /// <summary>
        /// Converts a Lean symbol string to a Dukascopy symbol
        /// </summary>
        private static string ConvertLeanSymbolToDukascopySymbol(string leanSymbol)
        {
            string dukascopySymbol;
            return MapLeanToDukascopy.TryGetValue(leanSymbol, out dukascopySymbol) ? dukascopySymbol : string.Empty;
        }

    }
}
