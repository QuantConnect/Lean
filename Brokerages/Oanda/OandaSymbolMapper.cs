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
using QuantConnect.Securities;

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
        public static readonly HashSet<string> KnownTickers =
            new HashSet<string>(SymbolPropertiesDatabase
                    .FromDataFolder()
                    .GetSymbolPropertiesList(Market.Oanda, SecurityType.Forex)
                    .Select(x => ConvertLeanSymbolToOandaSymbol(x.Key.Symbol))
                    .Concat(SymbolPropertiesDatabase
                        .FromDataFolder()
                        .GetSymbolPropertiesList(Market.Oanda, SecurityType.Cfd)
                        .Select(x => ConvertLeanSymbolToOandaSymbol(x.Key.Symbol))));

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
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
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
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
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
            return KnownTickers.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Oanda
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Oanda supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value) || symbol.Value.Length <= 3)
                return false;

            var oandaSymbol = ConvertLeanSymbolToOandaSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(oandaSymbol) && GetBrokerageSecurityType(oandaSymbol) == symbol.ID.SecurityType;
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
