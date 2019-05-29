/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
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
using QuantConnect.Brokerages;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Mapper of CoinApi Bitfinex symbols with more than 3 characters.
    /// </summary>
    /// <seealso cref="QuantConnect.Brokerages.ISymbolMapper" />
    public class CoinApiBitfinexSymbolMapper : ISymbolMapper
    {
        private HashSet<Tuple<string, string>> _mappedSymbols = new HashSet<Tuple<string, string>>
        {
            new Tuple<string, string>("anio", "nio"),
            new Tuple<string, string>("bchsv", "bsv"),
            new Tuple<string, string>("dash", "dsh"),
            new Tuple<string, string>("iota", "iot"),
            new Tuple<string, string>("mana", "mna"),
            new Tuple<string, string>("pkgo", "got"),
            new Tuple<string, string>("qtum", "qtm"),
            new Tuple<string, string>("usdt", "ust"),
            new Tuple<string, string>("yoyow", "yyw")
        };

        /// <summary>
        /// Converts a Lean symbol instance to a brokerage symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>
        /// The brokerage symbol
        /// </returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            var leanSymbol = symbol.Value;

            var pair = new[] { leanSymbol.Substring(0, 3), leanSymbol.Substring(3, 3) };

            for (var i = 0; i < pair.Length; i++)
            {
                foreach (var mappedSymbol in _mappedSymbols)
                {
                    if (mappedSymbol.Item2 == pair[i].ToLower())
                    {
                        pair[i] = mappedSymbol.Item1;
                    }
                }
            }

            return string.Join("_", pair.Select(p=>p.ToUpper()));
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
        /// <returns>
        /// A new Lean Symbol instance
        /// </returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = OptionRight.Call)
        {
            var pair = brokerageSymbol.Split('_');
            for (var i = 0; i < pair.Length; i++)
            {
                foreach (var mappedSymbol in _mappedSymbols)
                {
                    if (mappedSymbol.Item1 == pair[i].ToLower())
                    {
                        pair[i] = mappedSymbol.Item2;
                    }
                }
            }

            return Symbol.Create(string.Join(String.Empty, pair), SecurityType.Crypto, market);
        }
    }
}
