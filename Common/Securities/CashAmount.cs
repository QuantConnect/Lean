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
    /// Represents a cash amount which can be converted to account currency using a currency converter
    /// </summary>
    public struct CashAmount
    {
        private readonly ICurrencyConverter _currencyConverter;

        /// <summary>
        /// The amount of cash
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// The currency in which the cash amount is denominated
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Returns a cash amount denominated in the account currency
        /// </summary>
        public CashAmount ValueInAccountCurrency => _currencyConverter.ConvertToAccountCurrency(this);

        /// <summary>
        /// Initializes a new instance of the <see cref="CashAmount"/> class
        /// </summary>
        /// <param name="amount">The amount</param>
        /// <param name="currency">The currency</param>
        /// <param name="currencyConverter">The currency converter</param>
        public CashAmount(decimal amount, string currency, ICurrencyConverter currencyConverter)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Invalid currency", nameof(currency));
            }

            if (currencyConverter == null)
            {
                throw new ArgumentException("Invalid currency converter", nameof(currencyConverter));
            }

            Amount = amount;
            Currency = currency;
            _currencyConverter = currencyConverter;
        }
    }
}
