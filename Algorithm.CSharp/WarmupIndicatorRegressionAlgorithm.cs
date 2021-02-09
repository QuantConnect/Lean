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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm reproduces GH issue 2404, exception: `This is a forward only indicator`
    /// </summary>
    public class WarmupIndicatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 11, 1);
            SetEndDate(2013, 12, 10);    //Set End Date
            SetWarmup(TimeSpan.FromDays(30));

            _spy = AddEquity("SPY", Resolution.Daily).Symbol;
            var renkoConsolidator = new RenkoConsolidator(2m);
            renkoConsolidator.DataConsolidated += (sender, consolidated) =>
            {
                if (IsWarmingUp) return;
                if (!Portfolio.Invested)
                {
                    SetHoldings(_spy, 1.0);
                }
                Log($"CLOSE - {consolidated.Time:o} - {consolidated.Open} {consolidated.Close}");
            };
            var sma = new SimpleMovingAverage("SMA", 3);
            RegisterIndicator(_spy, sma, renkoConsolidator);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "21.640%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "2.149%"},
            {"Sharpe Ratio", "3.2"},
            {"Probabilistic Sharpe Ratio", "79.148%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.16"},
            {"Beta", "0.057"},
            {"Annual Standard Deviation", "0.054"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-0.47"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "3.035"},
            {"Total Fees", "$3.08"},
            {"Fitness Score", "0.028"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "11.51"},
            {"Return Over Maximum Drawdown", "18.205"},
            {"Portfolio Turnover", "0.029"},
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
            {"OrderListHash", "d880f140093d5377a4335692898da3c3"}
        };
    }
}
