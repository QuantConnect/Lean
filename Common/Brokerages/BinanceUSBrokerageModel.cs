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

using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Binance.US specific properties
    /// </summary>
    public class BinanceUSBrokerageModel : BinanceBrokerageModel
    {
        /// <summary>
        /// Market name
        /// </summary>
        protected override string MarketName => Market.BinanceUS;

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.BinanceUS);

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modeled, defaults to <see cref="AccountType.Cash"/></param>
        public BinanceUSBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException(Messages.BinanceUSBrokerageModel.UnsupportedAccountType);
            }
        }

        /// <summary>
        /// Binance global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            // margin trading is not currently supported by Binance.US
            return 1m;
        }
    }
}
