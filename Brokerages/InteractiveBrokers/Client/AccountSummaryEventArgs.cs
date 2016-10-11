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
    /// Event arguments class for the <see cref="InteractiveBrokersClient.AccountSummary"/> event
    /// </summary>
    public sealed class AccountSummaryEventArgs : EventArgs
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
        /// The account attribute being received.
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        /// The value of the attribute.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The currency in which the attribute is expressed.
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountSummaryEventArgs"/> class
        /// </summary>
        public AccountSummaryEventArgs(int requestId, string account, string tag, string value, string currency)
        {
            RequestId = requestId;
            Account = account;
            Tag = tag;
            Value = value;
            Currency = currency;
        }
    }
}