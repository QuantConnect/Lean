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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage symbols using the symbol properties database
    /// </summary>
    public class SymbolPropertiesDatabaseSymbolMapper : ISymbolMapper
    {
        private readonly string _market;

        // map Lean symbols to symbol properties
        private readonly Dictionary<Symbol, SymbolProperties> _symbolPropertiesMap;

        // map brokerage symbols to Lean symbols
        private readonly Dictionary<string, Symbol> _symbolMap;

        /// <summary>
        /// Creates a new instance of the <see cref="SymbolPropertiesDatabaseSymbolMapper"/> class.
        /// </summary>
        /// <param name="market">The Lean market</param>
        public SymbolPropertiesDatabaseSymbolMapper(string market)
        {
            _market = market;

            var symbolPropertiesList =
                SymbolPropertiesDatabase
                    .FromDataFolder()
                    .GetSymbolPropertiesList(_market)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value.MarketTicker))
                    .ToList();

            _symbolPropertiesMap =
                symbolPropertiesList
                    .ToDictionary(
                        x => Symbol.Create(x.Key.Symbol, x.Key.SecurityType, x.Key.Market),
                        x => x.Value);

            _symbolMap =
                _symbolPropertiesMap
                    .ToDictionary(
                        x => x.Value.MarketTicker,
                        x => x.Key);
        }

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
            {
                throw new ArgumentException($"Invalid symbol: {(symbol == null ? "null" : symbol.Value)}");
            }

            if (symbol.ID.Market != _market)
            {
                throw new ArgumentException($"Invalid market: {symbol.ID.Market}");
            }

            SymbolProperties symbolProperties;
            if (!_symbolPropertiesMap.TryGetValue(symbol, out symbolProperties) )
            {
                throw new ArgumentException($"Unknown symbol: {symbol.Value}/{symbol.SecurityType}/{symbol.ID.Market}");
            }

            if (string.IsNullOrWhiteSpace(symbolProperties.MarketTicker))
            {
                throw new ArgumentException($"MarketTicker not found in database for symbol: {symbol.Value}");
            }

            return symbolProperties.MarketTicker;
        }

        /// <summary>
        /// Converts a brokerage symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid brokerage symbol: {brokerageSymbol}");
            }

            if (market != _market)
            {
                throw new ArgumentException($"Invalid market: {market}");
            }

            Symbol symbol;
            if (!_symbolMap.TryGetValue(brokerageSymbol, out symbol))
            {
                throw new ArgumentException($"Unknown brokerage symbol: {brokerageSymbol}");
            }

            if (symbol.SecurityType != securityType)
            {
                throw new ArgumentException($"Invalid security type: {symbol.SecurityType}");
            }

            return symbol;
        }

        /// <summary>
        /// Checks if the Lean symbol is supported by the brokerage
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            return !string.IsNullOrWhiteSpace(symbol?.Value) && _symbolPropertiesMap.ContainsKey(symbol);
        }

        /// <summary>
        /// Returns the security type for a brokerage symbol
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid brokerage symbol: {brokerageSymbol}");
            }

            Symbol symbol;
            if (!_symbolMap.TryGetValue(brokerageSymbol, out symbol))
            {
                throw new ArgumentException($"Unknown brokerage symbol: {brokerageSymbol}");
            }

            return symbol.SecurityType;
        }

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                return false;
            }

            return _symbolMap.ContainsKey(brokerageSymbol);
        }
    }
}
