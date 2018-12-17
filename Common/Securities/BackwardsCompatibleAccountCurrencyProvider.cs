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

using QuantConnect.Interfaces;

namespace QuantConnect.Securities
{
    /// <summary>
    /// This class is a <see cref="IAccountCurrencyProvider"/> used
    /// to maintain backwards compatibility.
    /// It will always return <see cref="Currencies.USD"/>
    /// </summary>
    public class BackwardsCompatibleAccountCurrencyProvider : IAccountCurrencyProvider
    {
        /// <summary>
        /// Gets the account currency
        /// </summary>
        public string AccountCurrency => Currencies.USD;

        private BackwardsCompatibleAccountCurrencyProvider()
        {
        }

        /// <summary>
        /// Gets the static instance
        /// </summary>
        public static readonly BackwardsCompatibleAccountCurrencyProvider Instance
            = new BackwardsCompatibleAccountCurrencyProvider();
    }
}
