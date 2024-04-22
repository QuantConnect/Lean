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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm using <see cref="AccumulativeInsightPortfolioConstructionModel"/> and <see cref="ConstantAlphaModel"/>
    /// generating a constant <see cref="Insight"/> with a 0.25 confidence
    /// </summary>
    public class AccumulativeInsightPortfolioRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, 0.25));
            SetPortfolioConstruction(new AccumulativeInsightPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnEndOfAlgorithm()
        {
            if (// holdings value should be 0.03 - to avoid price fluctuation issue we compare with 0.06 and 0.01
                Portfolio.TotalHoldingsValue > Portfolio.TotalPortfolioValue * 0.06m
                ||
                Portfolio.TotalHoldingsValue < Portfolio.TotalPortfolioValue * 0.01m)
            {
                throw new Exception($"Unexpected Total Holdings Value: {Portfolio.TotalHoldingsValue}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "199"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-12.611%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-0.585"},
            {"Start Equity", "100000"},
            {"End Equity", "99827.80"},
            {"Net Profit", "-0.172%"},
            {"Sharpe Ratio", "-11.13"},
            {"Sortino Ratio", "-16.704"},
            {"Probabilistic Sharpe Ratio", "12.075%"},
            {"Loss Rate", "78%"},
            {"Win Rate", "22%"},
            {"Profit-Loss Ratio", "0.87"},
            {"Alpha", "-0.156"},
            {"Beta", "0.035"},
            {"Annual Standard Deviation", "0.008"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-9.603"},
            {"Tracking Error", "0.215"},
            {"Treynor Ratio", "-2.478"},
            {"Total Fees", "$199.00"},
            {"Estimated Strategy Capacity", "$26000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "119.89%"},
            {"OrderListHash", "d06c26f557b83d8d42ac808fe2815a1e"}
        };
    }
}
