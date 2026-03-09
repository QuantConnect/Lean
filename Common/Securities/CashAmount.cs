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

using ProtoBuf;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a cash amount which can be converted to account currency using a currency converter
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public struct CashAmount
    {
        /// <summary>
        /// The amount of cash
        /// </summary>
        [ProtoMember(1)]
        public decimal Amount { get; }

        /// <summary>
        /// The currency in which the cash amount is denominated
        /// </summary>
        [ProtoMember(2)]
        public string Currency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CashAmount"/> class
        /// </summary>
        /// <param name="amount">The amount</param>
        /// <param name="currency">The currency</param>
        public CashAmount(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Will determine if two <see cref="CashAmount"/> instances are equal
        /// Useful to compare against the default instance
        /// </summary>
        /// <returns>True if <see cref="Currency"/> and <see cref="Amount"/> are equal</returns>
        public static bool operator ==(CashAmount lhs, CashAmount rhs)
        {
            return Equals(lhs, rhs);
        }

        /// <summary>
        /// Will determine if two <see cref="CashAmount"/> instances are different
        /// Useful to compare against the default instance
        /// </summary>
        /// <returns>True if <see cref="Currency"/> or <see cref="Amount"/> are different</returns>
        public static bool operator !=(CashAmount lhs, CashAmount rhs)
        {
            return !Equals(lhs, rhs);
        }

        /// <summary>
        /// Used to compare two <see cref="CashAmount"/> instances.
        /// Useful to compare against the default instance
        /// </summary>
        /// <param name="obj">The other object to compare with</param>
        /// <returns>True if <see cref="Currency"/> and <see cref="Amount"/> are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj is CashAmount)
            {
                var cashAmountObj = (CashAmount) obj;
                return Amount == cashAmountObj.Amount
                    && Currency == cashAmountObj.Currency;
            }
            return false;
        }

        /// <summary>
        /// Get Hash Code for this Object
        /// </summary>
        /// <returns>Integer Hash Code</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
