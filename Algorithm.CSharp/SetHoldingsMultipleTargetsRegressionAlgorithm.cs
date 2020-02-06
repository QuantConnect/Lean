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
    /// Regression algorithm testing GH feature 3790, using SetHoldings with a collection of targets
    /// which will be ordered by margin impact before being executed, with the objective of avoiding any
    /// margin errors
    /// </summary>
    public class SetHoldingsMultipleTargetsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _ibm;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // use leverage 1 so we test the margin impact ordering
            _spy = AddEquity("SPY", Resolution.Minute, Market.USA, false, 1).Symbol;
            _ibm = AddEquity("IBM", Resolution.Minute, Market.USA, false, 1).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(new List<PortfolioTarget> { new PortfolioTarget(_spy, 0.8m), new PortfolioTarget(_ibm, 0.2m) });
            }
            else
            {
                SetHoldings(new List<PortfolioTarget> { new PortfolioTarget(_ibm, 0.8m), new PortfolioTarget(_spy, 0.2m) });
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "359.596%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "1.969%"},
            {"Sharpe Ratio", "5.655"},
            {"Probabilistic Sharpe Ratio", "65.889%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.303"},
            {"Beta", "1.001"},
            {"Annual Standard Deviation", "0.244"},
            {"Annual Variance", "0.06"},
            {"Information Ratio", "2.874"},
            {"Tracking Error", "0.106"},
            {"Treynor Ratio", "1.379"},
            {"Total Fees", "$11.48"},
            {"Fitness Score", "0.548"},
            {"Kelly Criterion Estimate", "-0.273"},
            {"Kelly Criterion Probability Value", "0.501"},
            {"Sortino Ratio", "17.178"},
            {"Return Over Maximum Drawdown", "125.323"},
            {"Portfolio Turnover", "0.55"},
            {"Total Insights Generated", "8"},
            {"Total Insights Closed", "6"},
            {"Total Insights Analysis Completed", "6"},
            {"Long Insight Count", "8"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$34124.9305"},
            {"Total Accumulated Estimated Alpha Value", "$5497.9055"},
            {"Mean Population Estimated Insight Value", "$916.3176"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "50.0196%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "-787031797"}
        };
    }
}
