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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.AccountUpdateMulti"/> event
    /// </summary>
    public sealed class AccountUpdateMultiEventArgs : EventArgs
    {
        /// <summary>
        /// The request's unique identifier.
        /// </summary>
        public int RequestId { get; private set; }

        /// <summary>
        /// The account ID.
        /// </summary>
        public string Account { get; private set; }

        /// <summary>
        /// the model code with updates
        /// </summary>
        public string ModelCode { get; private set; }

        /// <summary>
        /// the name of parameter
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// the value of parameter
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// the currency of parameter
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountUpdateMultiEventArgs"/> class
        /// </summary>
        public AccountUpdateMultiEventArgs(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            RequestId = requestId;
            Account = account;
            ModelCode = modelCode;
            Key = key;
            Value = value;
            Currency = currency;
        }
    }
}