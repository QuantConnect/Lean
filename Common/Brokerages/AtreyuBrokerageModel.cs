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

using QuantConnect.Data.Shortable;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Atreyu specific properties
    /// </summary>
    public class AtreyuBrokerageModel : DefaultBrokerageModel
    {
        private readonly IShortableProvider _shortableProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public AtreyuBrokerageModel(AccountType accountType = AccountType.Margin) : base(accountType)
        {
            _shortableProvider = new AtreyuShortableProvider(SecurityType.Equity, Market.USA);
        }

        /// <summary>
        /// Provides Atreyu fee model
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new AtreyuFeeModel();
        }

        /// <summary>
        /// Gets the shortable provider
        /// </summary>
        /// <returns>Shortable provider</returns>
        public override IShortableProvider GetShortableProvider()
        {
            return _shortableProvider;
        }
    }
}
