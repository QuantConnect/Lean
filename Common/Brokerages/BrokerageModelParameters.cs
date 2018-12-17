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
 *
*/

using QuantConnect.Interfaces;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Defines the parameters for a new <see cref="IBrokerageModel"/> instance
    /// </summary>
    public class BrokerageModelParameters
    {
        /// <summary>
        /// The account type
        /// </summary>
        public AccountType AccountType { get; }

        /// <summary>
        /// The account currency provider
        /// </summary>
        public IAccountCurrencyProvider AccountCurrencyProvider { get; }

        /// <summary>
        /// Creates a new <see cref="BrokerageModelParameters"/> instance
        /// </summary>
        /// <param name="accountCurrencyProvider"></param>
        /// <param name="accountType">The type of account to be modeled, defaults to
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public BrokerageModelParameters(
            IAccountCurrencyProvider accountCurrencyProvider,
            AccountType accountType = AccountType.Margin)
        {
            AccountCurrencyProvider = accountCurrencyProvider;
            AccountType = accountType;
        }
    }
}
