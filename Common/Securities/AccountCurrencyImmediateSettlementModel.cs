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
    /// <remarks>This model converts the amount to the account currency and applies cash settlement immediately</remarks>
    public class AccountCurrencyImmediateSettlementModel : ImmediateSettlementModel
    {
        /// <summary>
        /// Applies cash settlement rules
        /// </summary>
        /// <param name="applyFundsParameters">The funds application parameters</param>
        public override void ApplyFunds(ApplyFundsSettlementModelParameters applyFundsParameters)
        {
            var currency = applyFundsParameters.CashAmount.Currency;
            var amount = applyFundsParameters.CashAmount.Amount;
            var portfolio = applyFundsParameters.Portfolio;
            var amountInAccountCurrency = portfolio.CashBook.ConvertToAccountCurrency(amount, currency);

            portfolio.CashBook[portfolio.CashBook.AccountCurrency].AddAmount(amountInAccountCurrency);
        }
    }
}
