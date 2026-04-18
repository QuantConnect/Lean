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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Validates that SetHoldings returns the correct number of order tickets on each execution.
    /// </summary>
    public class SetHoldingReturnsOrderTicketsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _ibm;
        public override void Initialize()
        {
            SetStartDate(2018, 1, 4);
            SetEndDate(2018, 1, 10);
            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            _ibm = AddEquity("IBM", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice slice)
        {
            var tickets = SetHoldings(new List<PortfolioTarget> { new(_spy, 0.8m), new(_ibm, 0.2m) });

            if (!Portfolio.Invested)
            {
                // Ensure exactly 2 tickets are created when the portfolio is not yet invested
                if (tickets.Count != 2)
                {
                    throw new RegressionTestException("Expected 2 tickets, got " + tickets.Count);
                }
            }
            else if (tickets.Count != 0)
            {
                // Ensure no tickets are created when the portfolio is already invested
                throw new RegressionTestException("Expected 0 tickets, got " + tickets.Count);
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 53;

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
            {"Compounding Annual Return", "43.490%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100661.71"},
            {"Net Profit", "0.662%"},
            {"Sharpe Ratio", "12.329"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "97.100%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.108"},
            {"Beta", "0.424"},
            {"Annual Standard Deviation", "0.024"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-5.097"},
            {"Tracking Error", "0.03"},
            {"Treynor Ratio", "0.707"},
            {"Total Fees", "$2.56"},
            {"Estimated Strategy Capacity", "$170000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.24%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "587e1a69d3c83cbd9907f9f9586697e1"}
        };
    }
}
