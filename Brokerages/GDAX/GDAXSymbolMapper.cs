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
using System.Linq;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Provides the mapping between Lean symbols and GDAX symbols.
    /// </summary>
    public class GDAXSymbolMapper : ISymbolMapper
    {
        /// <summary>
        /// The list of known GDAX symbols.
        /// </summary>
        public static readonly HashSet<string> KnownTickers =
            new HashSet<string>(SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolPropertiesList(Market.GDAX, SecurityType.Crypto)
                .Select(x => x.Key.Symbol));

        /// <summary>
        /// Converts a Lean symbol instance to a GDAX symbol
        /// </summary>
        /// <param name="symbol">A Lean symbol instance</param>
        /// <returns>The GDAX symbol</returns>
        public string GetBrokerageSymbol(Symbol symbol)
        {
            if (symbol == null || string.IsNullOrWhiteSpace(symbol.Value))
            {
                throw new ArgumentException("Invalid symbol: " + (symbol == null ? "null" : symbol.ToString()));
            }

            if (symbol.ID.SecurityType != SecurityType.Crypto)
            {
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);
            }

            if (symbol.ID.Market != Market.GDAX)
            {
                throw new ArgumentException($"Invalid market: {symbol.ID.Market}");
            }

            var brokerageSymbol = ConvertLeanSymbolToGdaxSymbol(symbol.Value);

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
            {
                throw new ArgumentException("Unknown GDAX symbol: " + symbol.Value);
            }

            return brokerageSymbol;
        }

        /// <summary>
        /// Converts a GDAX symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The GDAX symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid GDAX symbol: {brokerageSymbol}");
            }

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
            {
                throw new ArgumentException($"Unknown GDAX symbol: {brokerageSymbol}");
            }

            if (securityType != SecurityType.Crypto)
            {
                throw new ArgumentException($"Invalid security type: {securityType}");
            }

            if (market != Market.GDAX)
            {
                throw new ArgumentException($"Invalid market: {market}");
            }

            return Symbol.Create(ConvertGdaxSymbolToLeanSymbol(brokerageSymbol), GetBrokerageSecurityType(brokerageSymbol), Market.GDAX);
        }

        /// <summary>
        /// Converts a GDAX symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The GDAX symbol</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var securityType = GetBrokerageSecurityType(brokerageSymbol);
            return GetLeanSymbol(brokerageSymbol, securityType, Market.GDAX);
        }

        /// <summary>
        /// Returns the security type for a GDAX symbol
        /// </summary>
        /// <param name="brokerageSymbol">The GDAX symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetBrokerageSecurityType(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
            {
                throw new ArgumentException($"Invalid GDAX symbol: {brokerageSymbol}");
            }

            if (!IsKnownBrokerageSymbol(brokerageSymbol))
            {
                throw new ArgumentException($"Unknown GDAX symbol: {brokerageSymbol}");
            }

            return SecurityType.Crypto;
        }

        /// <summary>
        /// Returns the security type for a Lean symbol
        /// </summary>
        /// <param name="leanSymbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetLeanSecurityType(string leanSymbol)
        {
            return GetBrokerageSecurityType(ConvertLeanSymbolToGdaxSymbol(leanSymbol));
        }

        /// <summary>
        /// Checks if the symbol is supported by GDAX
        /// </summary>
        /// <param name="brokerageSymbol">The GDAX symbol</param>
        /// <returns>True if GDAX supports the symbol</returns>
        public bool IsKnownBrokerageSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol) || !brokerageSymbol.Contains("-"))
            {
                return false;
            }

            var leanSymbol = ConvertGdaxSymbolToLeanSymbol(brokerageSymbol);

            return KnownTickers.Contains(leanSymbol);
        }

        /// <summary>
        /// Checks if the symbol is supported by GDAX
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>True if GDAX supports the symbol</returns>
        public static bool IsKnownLeanSymbol(Symbol symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol?.Value))
            {
                return false;
            }

            return KnownTickers.Contains(symbol.Value);
        }

        /// <summary>
        /// Converts a GDAX symbol to a Lean symbol string
        /// </summary>
        private static string ConvertGdaxSymbolToLeanSymbol(string brokerageSymbol)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol) || !brokerageSymbol.Contains("-"))
            {
                throw new ArgumentException($"Invalid GDAX symbol: {brokerageSymbol}");
            }

            return brokerageSymbol.Replace("-", "");
        }

        /// <summary>
        /// Converts a Lean symbol to a GDAX symbol
        /// </summary>
        private static string ConvertLeanSymbolToGdaxSymbol(string leanSymbol)
        {
            var symbol = Symbol.Create(leanSymbol, SecurityType.Crypto, Market.GDAX);

            if (!IsKnownLeanSymbol(symbol))
            {
                throw new ArgumentException($"Invalid Lean symbol: {leanSymbol}");
            }

            var symbolProperties = SymbolPropertiesDatabase
                .FromDataFolder()
                .GetSymbolProperties(Market.GDAX, leanSymbol, SecurityType.Crypto, Currencies.USD);

            string baseCurrency, quoteCurrency;
            Crypto.DecomposeCurrencyPair(symbol, symbolProperties, out baseCurrency, out quoteCurrency);

            return $"{baseCurrency}-{quoteCurrency}";
        }
    }
}
