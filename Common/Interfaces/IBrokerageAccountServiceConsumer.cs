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

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Algorithm capability used by live setup handlers to install optional brokerage account services.
    /// </summary>
    public interface IBrokerageAccountServiceConsumer
    {
        /// <summary>
        /// Installs the provider used to read and refresh brokerage account state.
        /// </summary>
        /// <param name="provider">Brokerage account-state provider.</param>
        void SetBrokerageAccountStateProvider(IBrokerageAccountStateProvider provider);

        /// <summary>
        /// Installs the provider used to change brokerage account-group membership.
        /// </summary>
        /// <param name="manager">Brokerage account-group manager.</param>
        void SetBrokerageAccountGroupManager(IBrokerageAccountGroupManager manager);

        /// <summary>
        /// Installs the provider used to change brokerage account-group allocation values.
        /// </summary>
        /// <param name="manager">Brokerage account-group allocation manager.</param>
        void SetBrokerageAccountGroupAllocationManager(IBrokerageAccountGroupAllocationManager manager);
    }
}
