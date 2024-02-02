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
    /// Represents the model responsible for applying cash settlement rules
    /// </summary>
    /// <remarks>This model applies cash settlement immediately</remarks>
    public class ImmediateSettlementModel : ISettlementModel
    {
        private string _accountCurrency;

        /// <summary>
        /// The account currency used to return the unsettled cash in the right currency
        /// </summary>
        protected string AccountCurrency
        {
            get
            {
                return string.IsNullOrEmpty(_accountCurrency) ? Currencies.USD : _accountCurrency;
            }
            set
            {
                if (string.IsNullOrEmpty(_accountCurrency))
                {
                    _accountCurrency = value;
                }
            }
        }

        /// <summary>
        /// Applies cash settlement rules
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        public virtual void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters)
        {
            var currency = applyFundsParameters.CashAmount.Currency;
            var amount = applyFundsParameters.CashAmount.Amount;
            applyFundsParameters.Portfolio.CashBook[currency].AddAmount(amount);

            // Keep it to return unsettled cash in the account currency
            AccountCurrency = applyFundsParameters.Portfolio.CashBook.AccountCurrency;
        }

        /// <summary>
        /// Scan for pending settlements
        /// </summary>
        /// <param name="settlementParameters">The settlement parameters</param>
        public virtual void Scan(ScanSettlementModelParameters settlementParameters)
        {
        }

        /// <summary>
        /// Gets the unsettled cash amount for the security
        /// </summary>
        public virtual CashAmount GetUnsettledCash()
        {
            return new CashAmount(0, AccountCurrency);
        }
    }
}
