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
using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages.Alpaca
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Alpaca symbols.
    /// </summary>
    public class AlpacaSymbolMapper : ISymbolMapper
    {
        private readonly IMapFileProvider _mapFileProvider;

        /// <summary>
        /// Constructs InteractiveBrokersSymbolMapper. Default parameters are used.
        /// </summary>
        public AlpacaSymbolMapper(IMapFileProvider mapFileProvider)
        {
            _mapFileProvider = mapFileProvider;
        }

        /// <summary>
        /// Converts a Lean symbol instance to an InteractiveBrokers symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The InteractiveBrokers symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            if (symbol.SecurityType != SecurityType.Equity)
                throw new ArgumentException($"Invalid security type: {symbol.SecurityType}");

            var mapFile = _mapFileProvider.Get(symbol.ID.Market).ResolveMapFile(symbol.ID.Symbol, symbol.ID.Date);
            return mapFile.GetMappedSymbol(DateTime.UtcNow, symbol.Value);
        }

        /// <summary>
        /// Converts an Alpaca symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Alpaca symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string market,
            DateTime expirationDate = default(DateTime),
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call
            )
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException($"Invalid symbol: {brokerageSymbol}");

            if (securityType != SecurityType.Equity)
                throw new ArgumentException($"Invalid security type: {securityType}");

            try
            {
                return Symbol.Create(brokerageSymbol, securityType, market);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid symbol: {brokerageSymbol}, security type: {securityType}, market: {market}.");
            }
        }
    }
}
