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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Rebalances between SPY and BND. Tests capacity of the weakest link, which in this
    /// case is BND, dragging down the capacity estimate.
    /// </summary>
    public class SpyBondPortfolioRebalance : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 3, 31);
            SetCash(10000);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            var bnd = AddEquity("BND", Resolution.Hour).Symbol;

            Schedule.On(DateRules.EveryDay(_spy), TimeRules.AfterMarketOpen(_spy, 1, false), () =>
            {
                SetHoldings(_spy, 0.5m);
                SetHoldings(bnd, 0.5m);
            });
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// </summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "21"},
            {"Average Win", "0.02%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "-33.564%"},
            {"Drawdown", "19.700%"},
            {"Expectancy", "-0.140"},
            {"Net Profit", "-9.655%"},
            {"Sharpe Ratio", "-0.99"},
            {"Probabilistic Sharpe Ratio", "13.754%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.72"},
            {"Alpha", "-0.022"},
            {"Beta", "0.538"},
            {"Annual Standard Deviation", "0.309"},
            {"Annual Variance", "0.096"},
            {"Information Ratio", "0.826"},
            {"Tracking Error", "0.269"},
            {"Treynor Ratio", "-0.569"},
            {"Total Fees", "$21.00"},
            {"Estimated Strategy Capacity", "$1100000.00"},
            {"Fitness Score", "0.005"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-1.524"},
            {"Return Over Maximum Drawdown", "-1.688"},
            {"Portfolio Turnover", "0.02"},
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
            {"OrderListHash", "95a130426900aaf227a08a5d1c617b2b"}
        };
    }
}
