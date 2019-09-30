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

using static QuantConnect.StringExtensions;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Messaging class signifying a change in a user's account
    /// </summary>
    public class AccountEvent
    {
        /// <summary>
        /// Gets the total cash balance of the account in units of <see cref="CurrencySymbol"/>
        /// </summary>
        public decimal CashBalance { get; private set; }

        /// <summary>
        /// Gets the currency symbol
        /// </summary>
        public string CurrencySymbol { get; private set; }

        /// <summary>
        /// Creates an AccountEvent
        /// </summary>
        /// <param name="currencySymbol">The currency's symbol</param>
        /// <param name="cashBalance">The total cash balance of the account</param>
        public AccountEvent(string currencySymbol, decimal cashBalance)
        {
            CashBalance = cashBalance;
            CurrencySymbol = currencySymbol;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Invariant($"Account {CurrencySymbol} Balance: {CashBalance:0.00}");
        }
    }
}
