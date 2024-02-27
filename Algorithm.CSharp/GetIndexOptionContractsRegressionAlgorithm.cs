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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Util;
using System.Diagnostics.Contracts;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test we can get option contracts for NQX index option
    /// </summary>
    public class GetIndexOptionContractsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _nqx;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2021, 2, 8);
            SetEndDate(2021, 2, 20);
            SetCash(100000);
            UniverseSettings.Resolution = Resolution.Hour;

            var index = AddIndex("NDX", Resolution.Hour).Symbol;
            var option = AddIndexOption(index, "NQX", Resolution.Hour);
            option.SetFilter(universe => universe.IncludeWeeklys().Strikes(-2, 2).Expiration(0, 30));

            _nqx = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            var weekly_chain = slice.OptionChains.get(_nqx);

            if (!weekly_chain.IsNullOrEmpty())
            {
                foreach (var contract in weekly_chain)
                {
                    if (Portfolio.Invested)
                    {
                        continue;
                    }

                    MarketOrder(contract.Symbol, 1);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1568755;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.98%"},
            {"Compounding Annual Return", "2914.619%"},
            {"Drawdown", "2.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "11.545%"},
            {"Sharpe Ratio", "30.282"},
            {"Sortino Ratio", "1978.245"},
            {"Probabilistic Sharpe Ratio", "99.961%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "12.417"},
            {"Beta", "0.116"},
            {"Annual Standard Deviation", "0.41"},
            {"Annual Variance", "0.168"},
            {"Information Ratio", "30.268"},
            {"Tracking Error", "0.411"},
            {"Treynor Ratio", "107.13"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "NQX XLZL7QT89Z7Y|NDX 31"},
            {"Portfolio Turnover", "0.30%"},
            {"OrderListHash", "7bedffc0a9d66947510a794411b54dd9"}
        };
    }
}
