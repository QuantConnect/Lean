/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014-2023 QuantConnect Corporation.
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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides GDAX specific properties
    /// </summary>
    [Obsolete("GDAXBrokerageModel is deprecated. Use CoinbaseBrokerageModel instead.")]
    public class GDAXBrokerageModel : CoinbaseBrokerageModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GDAXBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Cash"/></param>
        public GDAXBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        { }
    }
}
