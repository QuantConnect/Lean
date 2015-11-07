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

using System;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a pending cash amount waiting for settlement time
    /// </summary>
    public class UnsettledCashAmount
    {
        /// <summary>
        /// The settlement time (in UTC)
        /// </summary>
        public DateTime SettlementTimeUtc { get; private set; }

        /// <summary>
        /// The currency symbol
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// The amount of cash
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Creates a new instance of the <see cref="UnsettledCashAmount"/> class
        /// </summary>
        public UnsettledCashAmount(DateTime settlementTimeUtc, string currency, decimal amount)
        {
            SettlementTimeUtc = settlementTimeUtc;
            Currency = currency;
            Amount = amount;
        }
    }
}
