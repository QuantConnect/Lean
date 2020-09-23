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
using QuantConnect.Brokerages;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Polygon.io symbols.
    /// </summary>
    public class PolygonSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
            {
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));
            }

            switch (symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return symbol.Value;

                case SecurityType.Forex:
                    return symbol.Value.Substring(0, 3) + "/" + symbol.Value.Substring(3);

                case SecurityType.Crypto:
                    return symbol.Value.Substring(0, symbol.Value.Length - 3) + "-" + symbol.Value.Substring(symbol.Value.Length - 3);

                default:
                    throw new Exception($"PolygonSymbolMapper.GetBrokerageSymbol(): unsupported security type: {symbol.SecurityType}");
            }
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
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market,
            DateTime expirationDate = new DateTime(), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException("Invalid symbol: " + brokerageSymbol);
            }

            switch (securityType)
            {
                case SecurityType.Equity:
                    return Symbol.Create(brokerageSymbol, securityType, market);

                case SecurityType.Forex:
                    return Symbol.Create(brokerageSymbol.Replace("/", ""), securityType, market);

                case SecurityType.Crypto:
                    return Symbol.Create(brokerageSymbol.Replace("-", ""), securityType, market);

                default:
                    throw new Exception($"PolygonSymbolMapper.GetLeanSymbol(): unsupported security type: {securityType}");
            }
        }
    }
}
