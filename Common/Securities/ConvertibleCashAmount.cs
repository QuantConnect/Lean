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

namespace QuantConnect.Securities
{
    /// <summary>
    /// A cash amount that can easily be converted into account currency
    /// </summary>
    public class ConvertibleCashAmount
    {
        /// <summary>
        /// The amount
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// The cash associated with the amount
        /// </summary>
        public Cash Cash { get; }

        /// <summary>
        /// The amount in account currency
        /// </summary>
        public decimal InAccountCurrency => Amount * Cash.ConversionRate;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ConvertibleCashAmount (decimal amount, Cash cash)
        {
            Amount = amount;
            Cash = cash;
        }

        /// <summary>
        /// The amount in account currency
        /// </summary>
        public static implicit operator decimal(ConvertibleCashAmount convertibleCashAmount)
        {
            return convertibleCashAmount.InAccountCurrency;
        }

        /// <summary>
        /// The amount in account currency
        /// </summary>
        public static implicit operator CashAmount(ConvertibleCashAmount convertibleCashAmount)
        {
            return new CashAmount(convertibleCashAmount.Amount, convertibleCashAmount.Cash.Symbol);
        }
    }
}
