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

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Binance symbols.
    /// </summary>
    public class BinanceSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// The list of known Binance symbols.
        /// </summary>
        public static readonly HashSet<string> KnownTickers =
            new HashSet<string>(SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolPropertiesList(Market.Binance, SecurityType.Crypto)
                .Select(x => x.Key.Symbol));

        /// <summary>
        /// Converts a Lean symbol instance to an Binance symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Binance symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.SecurityType != SecurityType.Crypto)
                throw new ArgumentException("Invalid security type: " + symbol.SecurityType);

            var brokerageSymbol = ConvertLeanSymbolToBrokerageSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException("Unknown symbol: " + symbol.Value);

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts an Binance symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Binance symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Binance symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Binance symbol: {brokerageSymbol}");

            if (securityType != SecurityType.Crypto)
                throw new ArgumentException($"Invalid security type: {securityType}");

            if (market != Market.Binance)
                throw new ArgumentException($"Invalid market: {market}");

            return Symbol.Create(ConvertBrokerageSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.Binance);
        }

        /// <summary>
        /// Converts an Binance symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Binance symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            return GetLeanSymbol(brokerageSymbol, securityType, Market.Binance);
        }

        /// <summary>
        /// Returns the security type for an Binance symbol
        /// </summary>
        /// <param name="brokerageSymbol">The Binance symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid Binance symbol: {brokerageSymbol}");

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
                throw new ArgumentException($"Unknown Binance symbol: {brokerageSymbol}");

            return SecurityType.Crypto;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToBrokerageSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by Binance
        /// </summary>
        /// <param name="brokerageSymbol">The Binance symbol</param>
        /// <returns>True if Binance supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                return false;

            return KnownTickers.Contains(brokerageSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by Binance
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if Binance supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value) || symbol.Value.Length <= 3)
                return false;

            var binanceSymbol = ConvertLeanSymbolToBrokerageSymbol(symbol.Value);

            return IsKnownBrokerageSymbol(binanceSymbol) && GetBrokerageSecurityType(binanceSymbol) == symbol.SecurityType;
        }

        /// <summary>
        /// Converts an Binance symbol to a Lean symbol string
        /// </summary>
        private static string ConvertBrokerageSymbolToLeanSymbol(string binanceSymbol)
        {
            if (string.IsNullOrWhiteSpace(binanceSymbol))
                throw new ArgumentException($"Invalid Binance symbol: {binanceSymbol}");

            // return as it is due to Binance has similar Symbol format
            return binanceSymbol.LazyToUpper();
        }

        /// <summary>
        /// Converts a Lean symbol string to an Binance symbol
        /// </summary>
        private static string ConvertLeanSymbolToBrokerageSymbol(string leanSymbol)
        {
            if (string.IsNullOrWhiteSpace(leanSymbol))
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");

            // return as it is due to Binance has similar Symbol format
            return leanSymbol.LazyToUpper();
        }
    }
}
