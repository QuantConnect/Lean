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

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="ICurrencyConverter"/> that does NOT perform conversions.
    /// This implementation will throw if the specified cashAmount is not in units of account currency.
    /// </summary>
    public class IdentityCurrencyConverter : ICurrencyConverter
    {
        /// <summary>
        /// Gets account currency
        /// </summary>
        public string AccountCurrency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ICurrencyConverter"/> class
        /// </summary>
        /// <param name="accountCurrency">The algorithm's account currency</param>
        public IdentityCurrencyConverter(string accountCurrency)
        {
            AccountCurrency = accountCurrency;
        }

        /// <summary>
        /// Converts a cash amount to the account currency.
        /// This implementation can only handle cash amounts in units of the account currency.
        /// </summary>
        /// <param name="cashAmount">The <see cref="CashAmount"/> instance to convert</param>
        /// <returns>A new <see cref="CashAmount"/> instance denominated in the account currency</returns>
        public CashAmount ConvertToAccountCurrency(CashAmount cashAmount)
        {
            if (!string.Equals(cashAmount.Currency, AccountCurrency, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException($"The {nameof(IdentityCurrencyConverter)} can only handle CashAmounts in units of the account currency");
            }

            return cashAmount;
        }
    }
}