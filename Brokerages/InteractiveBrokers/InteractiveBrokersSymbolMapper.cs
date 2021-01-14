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

using Newtonsoft.Json;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using IB = QuantConnect.Brokerages.InteractiveBrokers.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IBApi;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// Provides the mapping between Lean symbols and InteractiveBrokers symbols.
    /// </summary>
    public class InteractiveBrokersSymbolMapper : ISymbolMapper
    {
        private readonly IMapFileProvider _mapFileProvider;

        // we have a special treatment of futures, because IB renamed several exchange tickers (like GBP instead of 6B). We fix this:
        // We map those tickers back to their original names using the map below
        private readonly Dictionary<string, string> _ibNameMap = new Dictionary<string, string>();

        /// <summary>
        /// Constructs InteractiveBrokersSymbolMapper. Default parameters are used.
        /// </summary>
        public InteractiveBrokersSymbolMapper(IMapFileProvider mapFileProvider) :
            this(Path.Combine("InteractiveBrokers", "IB-symbol-map.json"))
        {
            _mapFileProvider = mapFileProvider;
        }

        /// <summary>
        /// Constructs InteractiveBrokersSymbolMapper
        /// </summary>
        /// <param name="ibNameMap">New names map (IB -> LEAN)</param>
        public InteractiveBrokersSymbolMapper(Dictionary<string, string> ibNameMap)
        {
            _ibNameMap = ibNameMap;
        }

        /// <summary>
        /// Constructs InteractiveBrokersSymbolMapper
        /// </summary>
        /// <param name="ibNameMapFullName">Full file name of the map file</param>
        public InteractiveBrokersSymbolMapper(string ibNameMapFullName)
        {
            if (File.Exists(ibNameMapFullName))
            {
                _ibNameMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(ibNameMapFullName));
            }
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

            var ticker = GetMappedTicker(symbol);

            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("Invalid symbol: " + symbol.ToString());

            if (symbol.ID.SecurityType != SecurityType.Forex &&
                symbol.ID.SecurityType != SecurityType.Equity &&
                symbol.ID.SecurityType != SecurityType.Option &&
                symbol.ID.SecurityType != SecurityType.FutureOption &&
                symbol.ID.SecurityType != SecurityType.Future)
                throw new ArgumentException("Invalid security type: " + symbol.ID.SecurityType);

            if (symbol.ID.SecurityType == SecurityType.Forex && ticker.Length != 6)
                throw new ArgumentException("Forex symbol length must be equal to 6: " + symbol.Value);

            switch (symbol.ID.SecurityType)
            {
                case SecurityType.Option:
                    // Final case is for equities. We use the mapped value to select
                    // the equity we want to trade.
                    return GetMappedTicker(symbol.Underlying);

                case SecurityType.FutureOption:
                    // We use the underlying Future Symbol since IB doesn't use
                    // the Futures Options' ticker, but rather uses the underlying's
                    // Symbol, mapped to the brokerage.
                    return GetBrokerageSymbol(symbol.Underlying);

                case SecurityType.Future:
                    return GetBrokerageRootSymbol(symbol.ID.Symbol);

                case SecurityType.Equity:
                    return ticker.Replace(".", " ");
            }

            return ticker;
        }

        /// <summary>
        /// Converts an InteractiveBrokers symbol to a Lean symbol instance
        /// </summary>
        /// <param name="brokerageSymbol">The InteractiveBrokers symbol</param>
        /// <param name="securityType">The security type</param>
        /// <param name="market">The market</param>
        /// <param name="expirationDate">Expiration date of the security(if applicable)</param>
        /// <param name="strike">The strike of the security (if applicable)</param>
        /// <param name="optionRight">The option right of the security (if applicable)</param>
        /// <returns>A new Lean Symbol instance</returns>
        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market, DateTime expirationDate = default(DateTime), decimal strike = 0, OptionRight optionRight = 0)
        {
            if (string.IsNullOrWhiteSpace(brokerageSymbol))
                throw new ArgumentException("Invalid symbol: " + brokerageSymbol);

            if (securityType != SecurityType.Forex &&
                securityType != SecurityType.Equity &&
                securityType != SecurityType.Option &&
                securityType != SecurityType.Future &&
                securityType != SecurityType.FutureOption)
                throw new ArgumentException("Invalid security type: " + securityType);

            try
            {
                switch (securityType)
                {
                    case SecurityType.Future:
                        return Symbol.CreateFuture(GetLeanRootSymbol(brokerageSymbol), market, expirationDate);

                    case SecurityType.Option:
                        return Symbol.CreateOption(brokerageSymbol, market, OptionStyle.American, optionRight, strike, expirationDate);

                    case SecurityType.FutureOption:
                        var future = FuturesOptionsUnderlyingMapper.GetUnderlyingFutureFromFutureOption(
                            GetLeanRootSymbol(brokerageSymbol),
                            market,
                            expirationDate,
                            DateTime.Now);

                        if (future == null)
                        {
                            // This is the worst case scenario, because we didn't find a matching futures contract for the FOP.
                            // Note that this only applies to CBOT symbols for now.
                            throw new ArgumentException($"The Future Option with expected underlying of {future} with expiry: {expirationDate:yyyy-MM-dd} has no matching underlying future contract.");
                        }

                        return Symbol.CreateOption(
                            future,
                            market,
                            OptionStyle.American,
                            optionRight,
                            strike,
                            expirationDate);

                    case SecurityType.Equity:
                        brokerageSymbol = brokerageSymbol.Replace(" ", ".");
                        break;
                }

                return Symbol.Create(brokerageSymbol, securityType, market);
            }
            catch (Exception)
            {
                throw new ArgumentException($"Invalid symbol: {brokerageSymbol}, security type: {securityType}, market: {market}.");
            }
        }

        /// <summary>
        /// IB specific versions of the symbol mapping (GetBrokerageRootSymbol) for future root symbols
        /// </summary>
        /// <param name="rootSymbol">LEAN root symbol</param>
        /// <returns></returns>
        public string GetBrokerageRootSymbol(string rootSymbol)
        {
            var brokerageSymbol = _ibNameMap.FirstOrDefault(kv => kv.Value == rootSymbol);

            return brokerageSymbol.Key ?? rootSymbol;
        }

        /// <summary>
        /// IB specific versions of the symbol mapping (GetLeanRootSymbol) for future root symbols
        /// </summary>
        /// <param name="brokerageRootSymbol">IB Brokerage root symbol</param>
        /// <returns></returns>
        public string GetLeanRootSymbol(string brokerageRootSymbol)
        {
            return _ibNameMap.ContainsKey(brokerageRootSymbol) ? _ibNameMap[brokerageRootSymbol] : brokerageRootSymbol;
        }

        /// <summary>
        /// Parses a contract for future with malformed data.
        /// Malformed data usually manifests itself by having "0" assigned to some values
        /// we expect, like the contract's expiry date. The contract is returned by IB
        /// like this, usually due to a high amount of data subscriptions that are active
        /// in an account, surpassing IB's imposed limit. Read more about this here: https://interactivebrokers.github.io/tws-api/rtd_fqa_errors.html#rtd_common_errors_maxmktdata
        ///
        /// We are provided a string in the Symbol in malformed contracts that can be
        /// parsed to construct the clean contract, which is done by this method.
        /// </summary>
        /// <param name="malformedContract">Malformed contract (for futures), i.e. a contract with invalid values ("0") in some of its fields</param>
        /// <param name="symbolPropertiesDatabase">The symbol properties database to use</param>
        /// <returns>Clean Contract for the future</returns>
        /// <remarks>
        /// The malformed contract returns data similar to the following when calling <see cref="InteractiveBrokersBrokerage.GetContractDetails"/>: ES       MAR2021
        /// </remarks>
        public Contract ParseMalformedContractFutureSymbol(Contract malformedContract, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            Log.Trace($"InteractiveBrokersSymbolMapper.ParseMalformedContractFutureSymbol(): Parsing malformed contract: {InteractiveBrokersBrokerage.GetContractDescription(malformedContract)} with trading class: \"{malformedContract.TradingClass}\"");

            // capture any character except spaces, match spaces, capture any char except digits, capture digits
            var matches = Regex.Matches(malformedContract.Symbol, @"^(\S*)\s*(\D*)(\d*)");

            var match = matches[0].Groups;
            var contractSymbol = match[1].Value;
            var contractMonthExpiration = DateTime.ParseExact(match[2].Value, "MMM", CultureInfo.CurrentCulture).Month;
            var contractYearExpiration = match[3].Value;

            var leanSymbol = GetLeanRootSymbol(contractSymbol);
            string market;
            if (!symbolPropertiesDatabase.TryGetMarket(leanSymbol, SecurityType.Future, out market))
            {
                market = InteractiveBrokersBrokerageModel.DefaultMarketMap[SecurityType.Future];
            }
            var canonicalSymbol = Symbol.Create(leanSymbol, SecurityType.Future, market);
            var contractMonthYear = new DateTime(int.Parse(contractYearExpiration, CultureInfo.InvariantCulture), contractMonthExpiration, 1);
            // we get the expiration date using the FuturesExpiryFunctions
            var contractExpirationDate = FuturesExpiryFunctions.FuturesExpiryFunction(canonicalSymbol)(contractMonthYear);

            return new Contract
            {
                Symbol = contractSymbol,
                Multiplier = malformedContract.Multiplier,
                LastTradeDateOrContractMonth = $"{contractExpirationDate:yyyyMMdd}",
                Exchange = malformedContract.Exchange,
                SecType = malformedContract.SecType,
                IncludeExpired = false,
                Currency = malformedContract.Currency
            };
        }

        private string GetMappedTicker(Symbol symbol)
        {
            var ticker = symbol.Value;
            if (symbol.ID.SecurityType == SecurityType.Equity)
            {
                var mapFile = _mapFileProvider.Get(symbol.ID.Market).ResolveMapFile(symbol.ID.Symbol, symbol.ID.Date);
                ticker = mapFile.GetMappedSymbol(DateTime.UtcNow, symbol.Value);
            }

            return ticker;
        }

        /// <summary>
        /// Parses a contract for options with malformed data.
        /// Malformed data usually manifests itself by having "0" assigned to some values
        /// we expect, like the contract's expiry date. The contract is returned by IB
        /// like this, usually due to a high amount of data subscriptions that are active
        /// in an account, surpassing IB's imposed limit. Read more about this here: https://interactivebrokers.github.io/tws-api/rtd_fqa_errors.html#rtd_common_errors_maxmktdata
        ///
        /// We are provided a string in the Symbol in malformed contracts that can be
        /// parsed to construct the clean contract, which is done by this method.
        /// </summary>
        /// <param name="malformedContract">Malformed contract (for options), i.e. a contract with invalid values ("0") in some of its fields</param>
        /// <param name="exchange">Exchange that the contract's asset lives on/where orders will be routed through</param>
        /// <returns>Clean Contract for the option</returns>
        /// <remarks>
        /// The malformed contract returns data similar to the following when calling <see cref="InteractiveBrokersBrokerage.GetContractDetails"/>:
        /// OPT SPY JUN2021 350 P [SPY 210618P00350000 100] USD 0 0 0
        ///
        /// ... which the contents inside [] follow the pattern:
        ///
        /// [SYMBOL YY_MM_DD_OPTIONRIGHT_STRIKE(divide by 1000) MULTIPLIER]
        /// </remarks>
        public static Contract ParseMalformedContractOptionSymbol(Contract malformedContract, string exchange = "Smart")
        {
            Log.Trace($"InteractiveBrokersSymbolMapper.ParseMalformedContractOptionSymbol(): Parsing malformed contract: {InteractiveBrokersBrokerage.GetContractDescription(malformedContract)} with trading class: \"{malformedContract.TradingClass}\"");

            // we search for the '[ ]' pattern, inside of it we: (capture any character except spaces, match spaces) -> 3 times
            var matches = Regex.Matches(malformedContract.Symbol, @"^.*[\[](\S*)\s*(\S*)\s*(\S*)[\]]");

            var match = matches[0].Groups;
            var contractSymbol = match[1].Value;
            var contractSpecification = match[2].Value;
            var multiplier = match[3].Value;
            var expiryDate = "20" + contractSpecification.Substring(0, 6);
            var contractRight = contractSpecification[6] == 'C' ? IB.RightType.Call : IB.RightType.Put;
            var contractStrike = long.Parse(contractSpecification.Substring(7), CultureInfo.InvariantCulture) / 1000.0;

            return new Contract
            {
                Symbol = contractSymbol,
                Multiplier = multiplier,
                LastTradeDateOrContractMonth = expiryDate,
                Right = contractRight,
                Strike = contractStrike,
                Exchange = exchange,
                SecType = malformedContract.SecType,
                IncludeExpired = false,
                Currency = malformedContract.Currency
            };
        }
    }
}
