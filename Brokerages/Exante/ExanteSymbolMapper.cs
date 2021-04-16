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

namespace QuantConnect.Brokerages.Exante
{
    public class ExanteSymbolMapper : ISymbolMapper
    {
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

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Option:
                    return symbol.Underlying;

                case SecurityType.Future:
                    return symbol.ID.Symbol;

                case SecurityType.Equity:
                    return $"{ticker.LazyToUpper()}.{symbol.ID.Market.LazyToUpper()}";

                case SecurityType.Index:
                    return ticker;

                default:
                    return ticker;
            }
        }

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
                securityType != SecurityType.Cfd)
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
