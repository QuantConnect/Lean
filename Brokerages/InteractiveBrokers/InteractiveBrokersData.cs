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

using System.Collections.Concurrent;
using QuantConnect.Brokerages.InteractiveBrokers.FinancialAdvisor;

namespace QuantConnect.Brokerages.InteractiveBrokers
{
    /// <summary>
    /// This class contains account data and FA configuration
    /// </summary>
    public class InteractiveBrokersData
    {
        /// <summary>
        /// The account data for the selected account and/or all sub-accounts
        /// </summary>
        public ConcurrentDictionary<string, InteractiveBrokersAccountData> AccountData { get; } = new ConcurrentDictionary<string, InteractiveBrokersAccountData>();

        /// <summary>
        /// The configuration data for the financial advisor account
        /// </summary>
        public FinancialAdvisorConfiguration FinancialAdvisorConfiguration { get; } = new FinancialAdvisorConfiguration();

        /// <summary>
        /// Returns the holdings for the account or the aggregated holdings for the financial advisor sub-accounts
        /// </summary>
        /// <param name="account">The account</param>
        /// <returns>A concurrent dictionary with the cash balances</returns>
        public ConcurrentDictionary<string, Holding> GetAccountHoldings(string account)
        {
            if (FinancialAdvisorConfiguration.IsMasterAccount(account))
            {
                // TODO: aggregate holdings
            }

            InteractiveBrokersAccountData accountData;
            return AccountData.TryGetValue(account, out accountData)
                ? accountData.AccountHoldings
                : new ConcurrentDictionary<string, Holding>();
        }

        /// <summary>
        /// Returns the balances for the account or the aggregated balances for the financial advisor sub-accounts
        /// </summary>
        /// <param name="account">The account</param>
        /// <returns>A concurrent dictionary with the cash balances</returns>
        public ConcurrentDictionary<string, decimal> GetAccountCashBalances(string account)
        {
            if (FinancialAdvisorConfiguration.IsMasterAccount(account))
            {
                // aggregate balances
                var dictionary = new ConcurrentDictionary<string, decimal>();

                foreach (var kvpAccount in AccountData)
                {
                    foreach (var kvpBalance in kvpAccount.Value.CashBalances)
                    {
                        var currency = kvpBalance.Key;

                        decimal balance;
                        if (dictionary.TryGetValue(currency, out balance))
                        {
                            dictionary[currency] += balance;
                        }
                        else
                        {
                            dictionary.TryAdd(currency, kvpBalance.Value);
                        }
                    }
                }

                return dictionary;
            }

            InteractiveBrokersAccountData accountData;
            return AccountData.TryGetValue(account, out accountData)
                ? accountData.CashBalances
                : new ConcurrentDictionary<string, decimal>();
        }

        public void SetAccountProperty(string account, string key, string value, string currency)
        {
            InteractiveBrokersAccountData accountData;
            if (!AccountData.TryGetValue(account, out accountData))
            {
                accountData = new InteractiveBrokersAccountData();
                AccountData.TryAdd(account, accountData);
            }

            accountData.AccountProperties[currency + ":" + key] = value;
        }

        public void SetAccountCashBalance(string account, string currency, decimal balance)
        {
            InteractiveBrokersAccountData accountData;
            if (!AccountData.TryGetValue(account, out accountData))
            {
                accountData = new InteractiveBrokersAccountData();
                AccountData.TryAdd(account, accountData);
            }

            accountData.CashBalances.AddOrUpdate(currency, balance);
        }

        public void SetAccountHolding(string account, Holding holding)
        {
            InteractiveBrokersAccountData accountData;
            if (!AccountData.TryGetValue(account, out accountData))
            {
                accountData = new InteractiveBrokersAccountData();
                AccountData.TryAdd(account, accountData);
            }

            accountData.AccountHoldings[holding.Symbol.Value] = holding;
        }

        /// <summary>
        /// Clears this instance of <see cref="InteractiveBrokersData"/>
        /// </summary>
        public void Clear()
        {
            AccountData.Clear();
            FinancialAdvisorConfiguration.Clear();
        }
    }
}
