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

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Bitfinex symbols.
    /// </summary>
    public class BitfinexSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// The list of known Bitfinex symbols.
        /// https://api.bitfinex.com/v1/symbols
        /// </summary>
        public static readonly HashSet<string> KnownTickers =
            new HashSet<string>(SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolPropertiesList(Market.Bitfinex, SecurityType.Crypto)
                .Select(x => x.Key.Symbol));

        /// <summary>
        /// Converts a Lean symbol instance to an Bitfinex symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Bitfinex symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.ID.SecurityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToBitfinexSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Bitfinex symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Bitfinex symbol: {brokerageSymbol}");

            if (securityType != SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (market != Market.Bitfinex)
                throw new ArgumentException($"Invalid market: {market}");

            return Symbol.Create(ConvertBitfinexSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Bitfinex);
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            return GetLeanSymbol(brokerageSymbol, securityType, Market.Bitfinex);
        }

        /// <summary>
        /// Returns the security type for an Bitfinex symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Bitfinex symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Bitfinex symbol: {brokerageSymbol}");

            return SecurityType.Crypto;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToBitfinexSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Bitfinex
        /// </summary>
        /// <param name="brokerageSymbol">The Bitfinex symbol</param>
        /// <returns>True if Bitfinex supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            // Strip leading 't' char
            return KnownTickers.Contains(brokerageSymbol.Substring(1));
        }

        /// <summary>
        /// Checks if the symbol is supported by Bitfinex
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Bitfinex supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var bitfinexSymbol = ConvertLeanSymbolToBitfinexSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(bitfinexSymbol) && GetBrokerageSecurityType(bitfinexSymbol) == symbol.ID.SecurityType;
        }

        /// <summary>
        /// Converts an Bitfinex symbol to a Lean symbol string
        /// </summary>
        private static string ConvertBitfinexSymbolToLeanSymbol(string bitfinexSymbol)
        {
            if (string.IsNullOrWhiteSpace(bitfinexSymbol) || !bitfinexSymbol.StartsWith("t"))
                throw new ArgumentException($"Invalid Bitfinex symbol: {bitfinexSymbol}");

            // Strip leading 't' char
            return bitfinexSymbol.Substring(1).ToUpperInvariant();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Bitfinex symbol
        /// </summary>
        private static string ConvertLeanSymbolToBitfinexSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // Prepend 't' for Trading pairs
            return "t" + leanSymbol.ToUpperInvariant();
        }
    }
}
