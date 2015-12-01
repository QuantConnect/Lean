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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// This class is a symbol mapper that simply returns the input symbol. 
    /// It can be used as a default implementation of <see cref="ISymbolMapper"/>
    /// for brokerages with no symbol mapping needed.
    /// </summary>
    public class IdentitySymbolMapper : ISymbolMapper
    {
        private readonly string _market;

        /// <summary>
        /// Creates an instance of the <see cref="IdentitySymbolMapper"/> class
        /// </summary>
        /// <param name="market">The market to be used</param>
        public IdentitySymbolMapper(string market)
        {
            _market = market;
        }

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The brokerage symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || symbol == Symbol.Empty || string.IsNullOrWhiteSpace(symbol.Value))
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));

            return symbol.Value;
        }

        /// <summary>
        /// Converts a brokerage symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid brokerage symbol: " + brokerageSymbol);

            return Symbol.Create(brokerageSymbol, GetSecurityType(brokerageSymbol), _market);
        }

        /// <summary>
        /// Returns the security type for a brokerage symbol
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetSecurityType(string brokerageSymbol)
        {
            return SecurityType.Equity;
        }

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// </summary>
        /// <param name="brokerageSymbol">The brokerage symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            return true;
        }

        /// <summary>
        /// Checks if the symbol is supported by the brokerage
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if the brokerage supports the symbol</returns>
        public bool IsKnownLeanSymbol(Symbol symbol)
        {
            return true;
        }

    }
}
