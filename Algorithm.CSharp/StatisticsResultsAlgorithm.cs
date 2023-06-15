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
using System.Linq;

using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to access the statistics results from within an algorithm through the <see cref="Statistics"/> property.
    /// </summary>
    public class StatisticsResultsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        private Symbol _ibm;

        private ExponentialMovingAverage _fastSpyEma;

        private ExponentialMovingAverage _slowSpyEma;

        private ExponentialMovingAverage _fastIbmEma;

        private ExponentialMovingAverage _slowIbmEma;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _ibm = AddEquity("IBM", Resolution.Minute).Symbol;

            _fastSpyEma = EMA(_spy, 30, Resolution.Minute);
            _slowSpyEma = EMA(_spy, 60, Resolution.Minute);

            _fastIbmEma = EMA(_spy, 10, Resolution.Minute);
            _slowIbmEma = EMA(_spy, 30, Resolution.Minute);
        }

        public override void OnData(Slice data)
        {
            if (!_slowSpyEma.IsReady) return;

            if (_fastSpyEma > _slowSpyEma)
            {
                SetHoldings(_spy, 0.5);
            }
            else if (Securities[_spy].Invested)
            {
                Liquidate(_spy);
            }

            if (_fastIbmEma > _slowIbmEma)
            {
                SetHoldings(_ibm, 0.2);
            }
            else if (Securities[_ibm].Invested)
            {
                Liquidate(_ibm);
            }
        }

        public override void OnOrderEvent(Orders.OrderEvent orderEvent)
        {
            if (orderEvent.Status == Orders.OrderStatus.Filled)
            {
                // We can access the statistics summary at runtime
                var statistics = Statistics.Summary;
                var statisticsStr = string.Join("\n\t", statistics.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                Debug($"\nStatistics after fill:\n\t{statisticsStr}");

                // Access a single statistic
                Log($"Total trades so far: {statistics[PerformanceMetrics.TotalTrades]}");
                Log($"Sharpe Ratio: {statistics[PerformanceMetrics.SharpeRatio]}");
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
        public long DataPoints => 7843;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "93"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "18.903%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0.135"},
            {"Net Profit", "0.222%"},
            {"Sharpe Ratio", "6.533"},
            {"Probabilistic Sharpe Ratio", "69.072%"},
            {"Loss Rate", "70%"},
            {"Win Rate", "30%"},
            {"Profit-Loss Ratio", "2.73"},
            {"Alpha", "-0.138"},
            {"Beta", "0.264"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-9.751"},
            {"Tracking Error", "0.164"},
            {"Treynor Ratio", "1.459"},
            {"Total Fees", "$114.39"},
            {"Estimated Strategy Capacity", "$1100000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "549.26%"},
            {"OrderListHash", "8eba5008b53540153317baffe4083c6d"}
        };
    }
}
