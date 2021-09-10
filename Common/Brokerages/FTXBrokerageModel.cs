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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// FTX Brokerage model
    /// </summary>
    public class FTXBrokerageModel : DefaultBrokerageModel
    {
        private const decimal _defaultLeverage = 3m;

        /// <summary>
        /// Constructor for Kraken brokerage model
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public FTXBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// For margin trading, max leverage = 5
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            return AccountType == AccountType.Margin
                ? new SecurityMarginModel(_defaultLeverage)
                : new CashBuyingPowerModel();
        }
    }
}
