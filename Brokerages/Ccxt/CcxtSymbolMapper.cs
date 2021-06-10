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

namespace QuantConnect.Brokerages.Ccxt
{
    /// <summary>
    /// Provides the mapping between Lean symbols and CCXT symbols
    /// </summary>
    public class CcxtSymbolMapper : ISymbolMapper
    {
        private readonly string _market;
        private readonly SymbolPropertiesDatabaseSymbolMapper _symbolMapper;

        private readonly Dictionary<string, string> _mapCcxtExchangesToLeanMarkets = new()
        {
            { "binance", Market.Binance },
            { "bittrex", Market.Bittrex },
            { "coinbasepro", Market.GDAX },
            { "ftx", Market.Ftx },
            { "gateio", Market.GateIo },
            { "kraken", Market.Kraken }
        };

        /// <summary>
        /// Creates a new instance of the <see cref="CcxtSymbolMapper"/> class.
        /// </summary>
        /// <param name="exchangeName">The CCXT exchange name</param>
        public CcxtSymbolMapper(string exchangeName)
        {
            if (!_mapCcxtExchangesToLeanMarkets.TryGetValue(exchangeName, out _market))
            {
                throw new NotSupportedException($"Unsupported CCXT exchange: {exchangeName}");
            }

            _symbolMapper = new SymbolPropertiesDatabaseSymbolMapper(_market);
        }

        /// <summary>
        /// Returns the LEAN market for the selected exchange name
        /// </summary>
        /// <returns>The Lean market</returns>
        public string GetLeanMarket() => _market;

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            var symbolProperties = _symbolMapper.GetSymbolProperties(symbol);

            return symbol.Value.Replace(symbolProperties.QuoteCurrency, "/" + symbolProperties.QuoteCurrency, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Converts a brokerage symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid brokerage symbol: {brokerageSymbol}");
            }

            var ticker = brokerageSymbol.Replace("/", string.Empty, StringComparison.InvariantCulture);

            return Symbol.Create(ticker, SecurityType.Crypto, _market);
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
        public Symbol GetLeanSymbol(
            string brokerageSymbol,
            SecurityType securityType,
            string market,
            DateTime expirationDate = default,
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call
            )
        {
            // unused
            throw new NotImplementedException();
        }

    }
}
