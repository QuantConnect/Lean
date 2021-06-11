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
using QuantConnect.Benchmarks;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Ccxt
{
    /// <summary>
    /// Provides CCXT specific properties
    /// </summary>
    public class CcxtBrokerageModel : DefaultBrokerageModel
    {
        private readonly string _market;

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets => GetDefaultMarkets();

        /// <summary>
        /// Initializes a new instance of the <see cref="CcxtBrokerageModel"/> class
        /// </summary>
        /// <param name="exchangeName">The CCXT exchange name</param>
        /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Cash"/></param>
        public CcxtBrokerageModel(string exchangeName, AccountType accountType = AccountType.Cash) : base(accountType)
        {
            _market = new CcxtSymbolMapper(exchangeName).GetLeanMarket();

            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException("Margin trading is not currently supported.");
            }
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// Margin trading is not currently supported
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return new CashBuyingPowerModel();
        }

        /// <summary>
        /// Binance global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            // margin trading is not currently supported
            return 1m;
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, _market);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Provides Binance fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BinanceFeeModel();
        }

        private IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets()
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = _market;
            return map.ToReadOnlyDictionary();
        }
    }
}
