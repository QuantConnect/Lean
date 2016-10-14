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

namespace QuantConnect.Brokerages.InteractiveBrokers.Client
{
    /// <summary>
    /// Event arguments class for the <see cref="InteractiveBrokersClient.ManagedAccounts"/> event
    /// </summary>
    public sealed class ManagedAccountsEventArgs : EventArgs
    {
        /// <summary>
        /// The comma delimited list of FA managed accounts.
        /// </summary>
        public string AccountsList { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedAccountsEventArgs"/> class
        /// </summary>
        public ManagedAccountsEventArgs(string accountsList)
        {
            AccountsList = accountsList;
        }
    }
}