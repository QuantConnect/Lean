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
using System.Globalization;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    /// <summary>
    /// Provides the mapping between Lean symbols and TD Ameritrade symbols.
    /// </summary>
    public class TDAmeritradeSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// Converts a Lean symbol instance to a TD Ameritrade symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The TD Ameritrade symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            return symbol.SecurityType == SecurityType.Option
                ? GetOptionSymbol(symbol)
                : symbol.Value;
        }

        private static string GetOptionSymbol(Symbol symbol)
        {
            var optionSymbol = symbol.ID.OptionRight == OptionRight.Call ? "C" : "P";
            return $"{symbol.Underlying.Value}_{symbol.ID.Date.ToString("MMddyy", CultureInfo.InvariantCulture)}{optionSymbol}{symbol.ID.StrikePrice}";
        }

        private Symbol GetOptionSymbolFromBrokerageSymbol(string brokerageSymbol)
        {
            var symbolAndInfo = brokerageSymbol.Split('_');
            var symbol = symbolAndInfo[0];
            var info = symbolAndInfo[1];
            OptionRight optionRight;
            string[] dateAndStrike;
            if (info.Contains('C', StringComparison.InvariantCulture))
            {
                optionRight = OptionRight.Call;
                dateAndStrike = info.Split('C');
            }
            else
            {
                optionRight = OptionRight.Put;
                dateAndStrike = info.Split('P');
            }
            var expiration = DateTime.ParseExact(dateAndStrike[0], "MMddyy", CultureInfo.InvariantCulture);
            decimal strike = decimal.Parse(dateAndStrike[1], CultureInfo.InvariantCulture);

            return GetLeanSymbol(symbol, SecurityType.Option, Market.USA.ToString(), expiration, strike, optionRight);
        }

        /// <summary>
        /// Converts a TD Ameritrade symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The TD Ameritrade symbol</param>
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
            Symbol symbol;
            if (securityType.IsOption() || securityType == SecurityType.Future)
            {
                SecurityIdentifier sid;
                string ticker = brokerageSymbol;
                switch (securityType)
                {
                    case SecurityType.Option:
                        return GetOptionSymbolFromBrokerageSymbol(brokerageSymbol);

                    case SecurityType.Future:
                        sid = SecurityIdentifier.GenerateFuture(SecurityIdentifier.DefaultDate, ticker, market);
                        break;

                    case SecurityType.IndexOption:
                        return Symbol.CreateOption(
                            Symbol.Create(ticker, SecurityType.Index, market),
                            market,
                            OptionStyle.European,
                            default(OptionRight),
                            0,
                            SecurityIdentifier.DefaultDate);
                    default:
                        throw new NotImplementedException(Invariant($"The security type has not been implemented yet: {securityType}"));
                }

                symbol = new Symbol(sid, ticker);
            }
            else
                symbol = Symbol.Create(brokerageSymbol, securityType, Market.USA);

            return symbol;

        }
    }
}
