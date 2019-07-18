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
    /// Provides an implementation of <see cref="ICurrencyConverter"/> for use in
    /// tests that don't depend on this behavior.
    /// </summary>
    public class ErrorCurrencyConverter : ICurrencyConverter
    {
        /// <summary>
        /// Gets account currency
        /// </summary>
        public string AccountCurrency
        {
            get
            {
                throw new InvalidOperationException(
                    "Unexpected usage of ErrorCurrencyConverter.AccountCurrency");
            }
        }

        /// <summary>
        /// Provides access to the single instance of <see cref="ErrorCurrencyConverter"/>.
        /// This is done this way to ensure usage is explicit.
        /// </summary>
        public static ICurrencyConverter Instance = new ErrorCurrencyConverter();

        private ErrorCurrencyConverter()
        {
        }

        /// <summary>
        /// Converts a cash amount to the account currency
        /// </summary>
        /// <param name="cashAmount">The <see cref="CashAmount"/> instance to convert</param>
        /// <returns>A new <see cref="CashAmount"/> instance denominated in the account currency</returns>
        public CashAmount ConvertToAccountCurrency(CashAmount cashAmount)
        {
            throw new InvalidOperationException($"This method purposefully throws as a proof that a " +
                $"test does not depend on {nameof(ICurrencyConverter)}.If this exception is encountered, " +
                $"it means the test DOES depend on {nameof(ICurrencyConverter)} and should be properly " +
                $"updated to use a real implementation of {nameof(ICurrencyConverter)}.");
        }
    }
}
