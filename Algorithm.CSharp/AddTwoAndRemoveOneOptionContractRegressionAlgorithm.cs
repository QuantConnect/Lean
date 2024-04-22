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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing issue where underlying option contract would be removed with the first call
    /// too RemoveOptionContract
    /// </summary>
    public class AddTwoAndRemoveOneOptionContractRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contract1;
        private Symbol _contract2;
        private bool _hasRemoved;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 06);
            SetEndDate(2014, 06, 06);

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;
            UniverseSettings.MinimumTimeInUniverse = TimeSpan.Zero;

            var aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            var contracts = OptionChainProvider.GetOptionContractList(aapl, Time)
                .OrderBy(symbol => symbol.ID.Symbol)
                .Where(optionContract => optionContract.ID.OptionRight == OptionRight.Call
                    && optionContract.ID.OptionStyle == OptionStyle.American)
                .Take(2)
                .ToList();

            _contract1 = contracts[0];
            _contract2 = contracts[1];
            AddOptionContract(_contract1);
            AddOptionContract(_contract2);
        }

        public override void OnData(Slice slice)
        {
            if (slice.HasData)
            {
                if (!_hasRemoved)
                {
                    RemoveOptionContract(_contract1);
                    _hasRemoved = true;
                }
                else
                {
                    var subscriptions =
                        SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs("AAPL");
                    if (subscriptions.Count == 0)
                    {
                        throw new Exception("No configuration for underlying was found!");
                    }

                    if (!Portfolio.Invested)
                    {
                        Buy(_contract2, 1);
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_hasRemoved)
            {
                throw new Exception("Expect a single call to OnData where we removed the option and underlying");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1578;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99930"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$230000.00"},
            {"Lowest Capacity Asset", "AAPL VXBK4QQIRLZA|AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.25%"},
            {"OrderListHash", "5906f39bc46c238374cb8c7245dd66f8"}
        };
    }
}
