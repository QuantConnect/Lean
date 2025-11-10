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

using QuantConnect.Securities;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides utility methods for working with <see cref="CashAmount"/> instances
    /// </summary>
    public static class CashAmountUtil
    {
        /// <summary>
        /// Determines if a cash balance should be added to the cash book
        /// </summary>
        /// <param name="balance">The cash balance to check</param>
        /// <param name="accountCurrency">The algorithm's account currency</param>
        /// <returns>True if the balance should be added, false otherwise</returns>
        public static bool ShouldAddCashBalance(CashAmount balance, string accountCurrency)
        {
            // Don't add zero quantity currencies except the account currency
            return balance.Amount != 0 || balance.Currency == accountCurrency;
        }
    }
}