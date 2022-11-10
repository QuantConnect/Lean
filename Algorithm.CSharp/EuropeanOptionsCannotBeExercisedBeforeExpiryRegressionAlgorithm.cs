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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that European options cannot be exercised before expiry
    /// </summary>
    public class EuropeanOptionsCannotBeExercisedBeforeExpiryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        private bool _done;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 2, 1);
            SetCash(200000);

            var index = AddIndex("SPX", Resolution.Hour, fillDataForward: true);
            var indexOption = AddIndexOption(index.Symbol, Resolution.Hour, fillDataForward: true);
            indexOption.SetFilter(filterFunc => filterFunc);

            _optionSymbol = indexOption.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (_done || !slice.OptionChains.ContainsKey(_optionSymbol))
            {
                return;
            }

            var contracts = slice.OptionChains[_optionSymbol];

            if (!contracts.Any())
            {
                return;
            }

            var contractSymbol = contracts.First().Symbol;

            // Make sure  we test this before the options expiry
            if (contractSymbol.ID.Date < Time)
            {
                return;
            }

            if (MarketOrder(contractSymbol, 1).Status != OrderStatus.Filled)
            {
                throw new Exception("Expected market order to fill immediately");
            }

            if (ExerciseOption(contractSymbol, 1).Status == OrderStatus.Filled)
            {
                throw new Exception($"Expected European option to not be exercisable beefore its expiration date. " +
                                    $"Time: {Time}. Expiry: {_optionSymbol.ID.Date}");
            }

            _done = true;
            // We already tested, so we can stop the algorithm
            Quit();
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_done)
            {
                throw new Exception("Expected to test the option exercise before the option expiry");
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
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 51;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$45000000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3HB5O6M|SPX 31"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "9412c086aeb15bd443bc9d453996e19e"}
        };
    }
}
