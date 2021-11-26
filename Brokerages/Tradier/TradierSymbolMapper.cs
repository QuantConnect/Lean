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

namespace QuantConnect.Brokerages.Tradier
{
    /// <summary>
    /// Provides the mapping between Lean symbols and Tradier symbols.
    /// </summary>
    public class TradierSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Converts a Lean symbol instance to a Tradier symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The Tradier symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Option
                ? symbol.Value.Replace(" ", "")
                : symbol.Value;
        }

        /// <summary>
        /// Converts a Tradier symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Tradier symbol</param>
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
            // unused
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a Tradier symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The Tradier symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            Symbol symbol;
            if (brokerageSymbol.Length > 15)
            {
                // convert the Tradier option symbol to OSI format
                var underlying = brokerageSymbol.Substring(0, brokerageSymbol.Length - 15);
                var ticker = underlying.PadRight(6, ' ') + brokerageSymbol.Substring(underlying.Length);
                symbol = SymbolRepresentation.ParseOptionTickerOSI(ticker);
            }
            else
            {
                symbol = Symbol.Create(brokerageSymbol, SecurityType.Equity, Market.USA);
            }

            return symbol;
        }
    }
}
