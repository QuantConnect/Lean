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
        /// <param name="accountType">The type of account to be modelled, defaults to <see cref="AccountType.Margin"/> does not accept <see cref="AccountType.Cash"/>.</param>
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
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security) => new AlphaStreamsFeeModel();

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public override ISlippageModel GetSlippageModel(Security security) => new AlphaStreamsSlippageModel();

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public override decimal GetLeverage(Security security)
        {
            if (AccountType == AccountType.Cash)
            {
                return 1m;
            }

            switch (security.Type)
            {
                case SecurityType.Equity:
                    return 2m;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return 10m;

                case SecurityType.Crypto:
                    return 1m;

                case SecurityType.Base:
                case SecurityType.Commodity:
                case SecurityType.Option:
                case SecurityType.FutureOption:
                case SecurityType.Future:
                default:
                    return 1m;
            }
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        public override ISettlementModel GetSettlementModel(Security security) => new ImmediateSettlementModel();
    }
}
