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
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides properties specific to Alpha Streams
    /// </summary>
    public class AlphaStreamsBrokerageModel : DefaultBrokerageModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaStreamsBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType"></param>
        public AlphaStreamsBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            if (accountType == AccountType.Cash)
            {
                throw new ArgumentException("The Alpha Streams brokerage does not currently support Cash trading.", nameof(accountType));
            }
        }
        
        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security) => new AlphaStreamsFeeModel();
        /// <summary>
        /// Gets a new slippage model that represents this brokerage's slippage costs
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override ISlippageModel GetSlippageModel(Security security) => new AlphaStreamsSlippageModel();
        /// <summary>
        /// Force all security types to be restricted to 1x leverage
        ///     - Current restriction to 1x is for the AS competition
        ///     - Will be update in the future
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security) => 1m;
        /// <summary>
        /// Gets a new settlement model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override ISettlementModel GetSettlementModel(Security security) => new ImmediateSettlementModel();
        /// <summary>
        /// Get buying power model for the specific security type
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            var leverage = GetLeverage(security);
            IBuyingPowerModel model;

            switch (security.Type)
            {
                case SecurityType.Crypto:
                    model = new CashBuyingPowerModel();
                    break;
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    model = new SecurityMarginModel(leverage, RequiredFreeBuyingPowerPercent);
                    break;
                case SecurityType.Option:
                    model = new OptionMarginModel(RequiredFreeBuyingPowerPercent);
                    break;
                case SecurityType.Future:
                    model = new FutureMarginModel(RequiredFreeBuyingPowerPercent);
                    break;
                default:
                    model = new SecurityMarginModel(leverage, RequiredFreeBuyingPowerPercent);
                    break;
            }
            return model;
        }
    }
}