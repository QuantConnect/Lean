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

namespace QuantConnect.Brokerages.Exante
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Exante symbols.
    /// </summary>
    public class ExanteSymbolMapper : ISymbolMapper
    {
        private readonly Dictionary<string, string> _tickerToExchange;

        public ExanteSymbolMapper(Dictionary<string, string> tickerToExchange)
        {
            _tickerToExchange = tickerToExchange;
        }

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value))
                throw new ArgumentException($"Invalid symbol: {(symbol == null ? "null" : symbol.ToString())}");

            var ticker = symbol.ID.Symbol;

            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException($"Invalid symbol: {symbol}");

            if (symbol.ID.SecurityType != SecurityType.Forex &&
                symbol.ID.SecurityType != SecurityType.Equity &&
                symbol.ID.SecurityType != SecurityType.Index &&
                symbol.ID.SecurityType != SecurityType.Option &&
                symbol.ID.SecurityType != SecurityType.Future &&
                symbol.ID.SecurityType != SecurityType.Cfd &&
                symbol.ID.SecurityType != SecurityType.Crypto &&
                symbol.ID.SecurityType != SecurityType.Index)
                throw new ArgumentException($"Invalid security type: {symbol.ID.SecurityType}");

            if (symbol.ID.SecurityType == SecurityType.Forex && ticker.Length != 6)
                throw new ArgumentException($"Forex symbol length must be equal to 6: {symbol.Value}");

            string symbolId;
            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Option:
                    symbolId = symbol.Underlying;
                    break;

                case SecurityType.Future:
                    symbolId = symbol.ID.Symbol;
                    break;

                case SecurityType.Equity:
                    symbolId = symbol.ID.Symbol;
                    break;

                case SecurityType.Index:
                    symbolId = ticker;
                    break;

                default:
                    symbolId = ticker;
                    break;
            }

            symbolId = symbolId.LazyToUpper();

            if (!_tickerToExchange.TryGetValue(symbolId, out var exchange))
            {
                throw new ArgumentException($"Unknown exchange for symbol '{symbolId}'");
            }

            return $"{symbolId}.{exchange}";
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
            DateTime expirationDate = default(DateTime),
            decimal strike = 0,
            OptionRight optionRight = OptionRight.Call
            )
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException("Invalid symbol: " + brokerageSymbol);
            }

            if (securityType != SecurityType.Forex &&
                securityType != SecurityType.Equity &&
                securityType != SecurityType.Index &&
                securityType != SecurityType.Option &&
                securityType != SecurityType.IndexOption &&
                securityType != SecurityType.Future &&
                securityType != SecurityType.FutureOption &&
                securityType != SecurityType.Cfd &&
                securityType != SecurityType.Crypto)
            {
                throw new ArgumentException("Invalid security type: " + securityType);
            }

            Symbol symbol;
            switch (securityType)
            {
                case SecurityType.Option:
                    symbol = Symbol.CreateOption(brokerageSymbol, market, OptionStyle.American,
                        optionRight, strike, expirationDate);
                    break;

                case SecurityType.Future:
                    symbol = Symbol.CreateFuture(brokerageSymbol, market, expirationDate);
                    break;

                default:
                    symbol = Symbol.Create(brokerageSymbol, securityType, market);
                    break;
            }

            return symbol;
        }
    }
}
