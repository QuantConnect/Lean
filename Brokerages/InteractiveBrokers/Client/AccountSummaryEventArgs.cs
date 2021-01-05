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
    public class AccountSummaryEventArgs : EventArgs
    {
        /// <summary>
        /// The request's identifier.
        /// </summary>
        public int RequestId { get; }

        /// <summary>
        /// The account id.
        /// </summary>
        public string Account { get; }

        /// <summary>
        /// The account's attribute being received.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// The account's attribute's value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The currency on which the value is expressed.
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountSummaryEventArgs"/> class
        /// </summary>
        public AccountSummaryEventArgs(int reqId, string account, string tag, string value, string currency)
        {
            RequestId = reqId;
            Account = account;
            Tag = tag;
            Value = value;
            Currency = currency;
        }
    }
}