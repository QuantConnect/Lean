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

namespace QuantConnect.Brokerages.Oanda
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Oanda symbols.
    /// </summary>
    public class OandaSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// The list of known Oanda symbols.
        /// </summary>
        public static readonly HashSet<string> KnownSymbols = new HashSet<string>
        {
            "AU200_AUD",
            "AUD_CAD",
            "AUD_CHF",
            "AUD_HKD",
            "AUD_JPY",
            "AUD_NZD",
            "AUD_SGD",
            "AUD_USD",
            "BCO_USD",
            "CAD_CHF",
            "CAD_HKD",
            "CAD_JPY",
            "CAD_SGD",
            "CH20_CHF",
            "CHF_HKD",
            "CHF_JPY",
            "CHF_ZAR",
            "CORN_USD",
            "DE10YB_EUR",
            "DE30_EUR",
            "EU50_EUR",
            "EUR_AUD",
            "EUR_CAD",
            "EUR_CHF",
            "EUR_CZK",
            "EUR_DKK",
            "EUR_GBP",
            "EUR_HKD",
            "EUR_HUF",
            "EUR_JPY",
            "EUR_NOK",
            "EUR_NZD",
            "EUR_PLN",
            "EUR_SEK",
            "EUR_SGD",
            "EUR_TRY",
            "EUR_USD",
            "EUR_ZAR",
            "FR40_EUR",
            "GBP_AUD",
            "GBP_CAD",
            "GBP_CHF",
            "GBP_HKD",
            "GBP_JPY",
            "GBP_NZD",
            "GBP_PLN",
            "GBP_SGD",
            "GBP_USD",
            "GBP_ZAR",
            "HK33_HKD",
            "HKD_JPY",
            "JP225_USD",
            "NAS100_USD",
            "NATGAS_USD",
            "NL25_EUR",
            "NZD_CAD",
            "NZD_CHF",
            "NZD_HKD",
            "NZD_JPY",
            "NZD_SGD",
            "NZD_USD",
            "SG30_SGD",
            "SGD_CHF",
            "SGD_HKD",
            "SGD_JPY",
            "SOYBN_USD",
            "SPX500_USD",
            "SUGAR_USD",
            "TRY_JPY",
            "UK100_GBP",
            "UK10YB_GBP",
            "US2000_USD",
            "US30_USD",
            "USB02Y_USD",
            "USB05Y_USD",
            "USB10Y_USD",
            "USB30Y_USD",
            "USD_CAD",
            "USD_CHF",
            "USD_CNH",
            "USD_CNY",
            "USD_CZK",
            "USD_DKK",
            "USD_HKD",
            "USD_HUF",
            "USD_INR",
            "USD_JPY",
            "USD_MXN",
            "USD_NOK",
            "USD_PLN",
            "USD_SAR",
            "USD_SEK",
            "USD_SGD",
            "USD_THB",
            "USD_TRY",
            "USD_TWD",
            "USD_ZAR",
            "WHEAT_USD",
            "WTICO_USD",
            "XAG_AUD",
            "XAG_CAD",
            "XAG_CHF",
            "XAG_EUR",
            "XAG_GBP",
            "XAG_HKD",
            "XAG_JPY",
            "XAG_NZD",
            "XAG_SGD",
            "XAG_USD",
            "XAU_AUD",
            "XAU_CAD",
            "XAU_CHF",
            "XAU_EUR",
            "XAU_GBP",
            "XAU_HKD",
            "XAU_JPY",
            "XAU_NZD",
            "XAU_SGD",
            "XAU_USD",
            "XAU_XAG",
            "XCU_USD",
            "XPD_USD",
            "XPT_USD",
            "ZAR_JPY"
        };

        /// <summary>
        /// The list of known Oanda currencies.
        /// </summary>
        private static readonly HashSet<string> KnownCurrencies = new HashSet<string>
        {
            "AUD", "CAD", "CHF", "CNH", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD", "HUF", "INR", "JPY",
            "MXN", "NOK", "NZD", "PLN", "SAR", "SEK", "SGD", "THB", "TRY", "TWD", "USD", "ZAR"
        };

        /// <summary>
        /// Converts a Lean symbol instance to an Oanda symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Oanda symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || symbol == Symbol.Empty || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToOandaSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Oanda symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid Oanda symbol: " + brokerageSymbol);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown Oanda symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Forex && securityType != SecurityType.Cfd)
                throw new ArgumentException("Invalid security type: " + securityType);

            if (market != Market.Oanda)
                throw new ArgumentException("Invalid market: " + market);

            return Symbol.Create(ConvertOandaSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Oanda);
        }

        /// <summary>
        /// Returns the security type for an Oanda symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            var tokens = brokerageSymbol.Split('_');
            if (tokens.Length != 2)
                throw new ArgumentException("Unable to determine SecurityType for Oanda symbol: " + brokerageSymbol);

            return KnownCurrencies.Contains(tokens[0]) && KnownCurrencies.Contains(tokens[1])
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
            return GetBrokerageSecurityType(ConvertLeanSymbolToOandaSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Oanda
        /// </summary>
        /// <param name="brokerageSymbol">The Oanda symbol</param>
        /// <returns>True if Oanda supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            return KnownSymbols.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Oanda
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Oanda supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value)) 
                return false;

            var oandaSymbol = ConvertLeanSymbolToOandaSymbol(symbol.Value);

            return KnownSymbols.Contains(oandaSymbol) && GetBrokerageSecurityType(oandaSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Oanda symbol to a Lean symbol string
        /// </summary>
        private static string ConvertOandaSymbolToLeanSymbol(string oandaSymbol)
        {
            // Lean symbols are equal to Oanda symbols with underscores removed
            return oandaSymbol.Replace("_", "");
        }

        /// <summary>
        /// Converts a Lean symbol string to an Oanda symbol
        /// </summary>
        private static string ConvertLeanSymbolToOandaSymbol(string leanSymbol)
        {
            // All Oanda symbols end with '_XYZ', where XYZ is the quote currency
            return leanSymbol.Insert(leanSymbol.Length - 3, "_");
        }

    }
}
