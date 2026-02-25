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
using System.Linq;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides the mapping between Lean symbols and brokerage symbols using the symbol properties database
    /// </summary>
    public class SymbolPropertiesDatabaseSymbolMapper : ISymbolMapper
    {
        private readonly string _market;

        // map Lean symbols to symbol properties
        private Dictionary<Symbol, SymbolProperties> _symbolPropertiesMap;

        // map brokerage symbols to Lean symbols we do it per security type because they could overlap, for example binance futures and spot
        private Dictionary<SecurityType, Dictionary<string, Symbol>> _symbolMap;

        // Timestamp of the last successful reload
        private DateTime _lastReloadTime;

        // Minimum time between reloads
        private static readonly TimeSpan _minReloadInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Creates a new instance of the <see cref="SymbolPropertiesDatabaseSymbolMapper"/> class.
        /// </summary>
        /// <param name="market">The Lean market</param>
        public SymbolPropertiesDatabaseSymbolMapper(string market)
        {
            _market = market;
            TryRefreshMappings();
        }

        /// <summary>
        /// Attempts to refresh the mappings if the minimum reload interval has passed
        /// </summary>
        /// <returns>True if the mappings were refreshed, false otherwise</returns>
        private bool TryRefreshMappings()
        {
            // Check if enough time has passed since the last reload
            if (DateTime.UtcNow - _lastReloadTime < _minReloadInterval)
            {
                return false;
            }

            var symbolPropertiesList =
                SymbolPropertiesDatabase
                    .FromDataFolder()
                    .GetSymbolPropertiesList(_market)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value.MarketTicker))
                    .ToList();

            var symbolPropertiesMap =
                symbolPropertiesList
                    .ToDictionary(
                        x => Symbol.Create(x.Key.Symbol, x.Key.SecurityType, x.Key.Market),
                        x => x.Value);

            var symbolMap = new Dictionary<SecurityType, Dictionary<string, Symbol>>();
            foreach (var group in symbolPropertiesMap.GroupBy(x => x.Key.SecurityType))
            {
                symbolMap[group.Key] = group.ToDictionary(
                            x => x.Value.MarketTicker,
                            x => x.Key);
            }

            _symbolPropertiesMap = symbolPropertiesMap;
            _symbolMap = symbolMap;

            // Update the last reload time
            _lastReloadTime = DateTime.UtcNow;
            return true;
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

            // First attempt with current mappings
            if (!_symbolPropertiesMap.TryGetValue(symbol, out var symbolProperties))
            {
                // If not found, try to refresh the mappings and check again
                if (!TryRefreshMappings() || !_symbolPropertiesMap.TryGetValue(symbol, out symbolProperties))
                {
                    throw new ArgumentException($"Unknown symbol: {symbol.Value}/{symbol.SecurityType}/{symbol.ID.Market}");
                }
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

            // First attempt with current mappings
            if (!TryGetLeanSymbol(brokerageSymbol, securityType, out var symbol))
            {
                // If not found, try to refresh and check again
                if (!TryRefreshMappings() || !TryGetLeanSymbol(brokerageSymbol, securityType, out symbol))
                {
                    throw new ArgumentException($"Unknown brokerage symbol: {brokerageSymbol}/{securityType}");
                }
            }

            return symbol;
        }

        private bool TryGetLeanSymbol(string brokerageSymbol, SecurityType securityType, out Symbol symbol)
        {
            symbol = null;
            return _symbolMap.TryGetValue(securityType, out var symbols) && symbols.TryGetValue(brokerageSymbol, out symbol);
        }

        /// <summary>
        /// Checks if the Lean symbol is supported by the brokerage
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value))
            {
                return false;
            }

            // First check current mappings
            if (_symbolPropertiesMap.ContainsKey(symbol))
            {
                return true;
            }

            // If not found, try to refresh and check again
            return TryRefreshMappings() && _symbolPropertiesMap.ContainsKey(symbol);
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

            // First attempt with current mappings
            var result = GetMatchingSymbols(brokerageSymbol);
            if (result.Count == 0)
            {
                // If not found, try to refresh and check again
                if (!TryRefreshMappings() || (result = GetMatchingSymbols(brokerageSymbol)).Count == 0)
                {
                    throw new ArgumentException($"Unknown brokerage symbol: {brokerageSymbol}");
                }
            }

            if (result.Count > 1)
            {
                throw new ArgumentException($"Found multiple brokerage symbols: {string.Join(",", result)}");
            }

            return result[0].SecurityType;
        }

        private List<Symbol> GetMatchingSymbols(string brokerageSymbol)
        {
            return _symbolMap.Select(kvp =>
            {
                kvp.Value.TryGetValue(brokerageSymbol, out var symbol);
                return symbol;
            }).Where(s => s != null).ToList();
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

            if (_symbolMap.Any(kvp => kvp.Value.ContainsKey(brokerageSymbol)))
            {
                return true;
            }

            // If not found, try to refresh and check again
            return TryRefreshMappings() && _symbolMap.Any(kvp => kvp.Value.ContainsKey(brokerageSymbol));
        }
    }
}
