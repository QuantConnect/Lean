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

using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that delisting liquidation proceeds on a cash account
    /// correctly settle through AlgorithmManager scanning Total securities collection (#9838).
    /// </summary>
    public class DelistingCashSettlementRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private bool _delisted;

        public override void Initialize()
        {
            SetStartDate(2007, 5, 16);
            SetEndDate(2007, 5, 25);
            SetAccountCurrency("USD");
            SetBrokerageModel(BrokerageName.Default, AccountType.Cash);
            SetCash(100000);

            var security = AddEquity("AAA.1", Resolution.Daily);
            _symbol = security.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && !_delisted)
            {
                SetHoldings(_symbol, 1);
            }

            if (slice.Delistings.TryGetValue(_symbol, out var delisting))
            {
                if (delisting.Type == DelistingType.Delisted)
                {
                    _delisted = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_delisted)
            {
                throw new RegressionTestException("Security was not delisted as expected.");
            }

            // After settlement date, all liquidation proceeds from delisted asset must be settled
            if (Portfolio.UnsettledCash != 0)
            {
                throw new RegressionTestException($"Unsettled cash from delisting liquidation was not settled! Unsettled cash: {Portfolio.UnsettledCash}");
            }
        }

        public bool CanRunLocally => true;
        public List<Language> Languages => new() { Language.CSharp };
        public long DataPoints => 54;
        public int AlgorithmHistoryDataPoints => 0;
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-5.58%"},
            {"Compounding Annual Return", "-87.351%"},
            {"Drawdown", "5.600%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "94421.6"},
            {"Net Profit", "-5.578%"},
            {"Sharpe Ratio", "-5.495"},
            {"Sortino Ratio", "-10.306"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.585"},
            {"Beta", "-1.085"},
            {"Annual Standard Deviation", "0.15"},
            {"Annual Variance", "0.023"},
            {"Information Ratio", "-5.081"},
            {"Tracking Error", "0.206"},
            {"Treynor Ratio", "0.76"},
            {"Total Fees", "$36.70"},
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "18.33%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "1450ea23a3a1ef4ee2398ec757c39223"}
        };
    }
}
