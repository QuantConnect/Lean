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

using System.Collections.Generic;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// FTX.US Brokerage model
    /// </summary>
    public class FTXUSBrokerageModel : FTXBrokerageModel
    {
        /// <summary>
        /// Market name
        /// </summary>
        protected override string MarketName => Market.FTXUS;

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } =
            GetDefaultMarkets(Market.FTXUS);

        /// <summary>
        /// Creates an instance of <see cref="FTXUSBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">Cash or Margin</param>
        public FTXUSBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType) { }

        /// <summary>
        /// Provides FTX.US fee model
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security) => new FTXUSFeeModel();
    }
}
