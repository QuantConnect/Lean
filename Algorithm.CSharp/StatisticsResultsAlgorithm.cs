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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to access the statistics results from within an algorithm through the <see cref="Statistics"/> property.
    /// </summary>
    public class StatisticsResultsAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string MostTradedSecurityStatistic = "Most Traded Security";
        private const string MostTradedSecurityTradeCountStatistic = "Most Traded Security Trade Count";

        private Symbol _spy;

        private Symbol _ibm;

        private ExponentialMovingAverage _fastSpyEma;

        private ExponentialMovingAverage _slowSpyEma;

        private ExponentialMovingAverage _fastIbmEma;

        private ExponentialMovingAverage _slowIbmEma;

        private Dictionary<Symbol, int> _tradeCounts = new();

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

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                // We can access the statistics summary at runtime
                var statistics = Statistics.Summary;
                var statisticsStr = string.Join("\n\t", statistics.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                Debug($"\nStatistics after fill:\n\t{statisticsStr}");

                // Access a single statistic
                Log($"Total trades so far: {statistics[PerformanceMetrics.TotalOrders]}");
                Log($"Sharpe Ratio: {statistics[PerformanceMetrics.SharpeRatio]}");

                // --------

                // We can also set custom summary statistics:

                KeyValuePair<Symbol, int> mostTradeSecurityKvp;

                // Before the first fill event, our custom statistics should not be set in the summary
                if (_tradeCounts.All(kvp => kvp.Value == 0))
                {
                    if (statistics.ContainsKey(MostTradedSecurityStatistic))
                    {
                        throw new Exception($"Statistic {MostTradedSecurityStatistic} should not be set yet");
                    }
                    if (statistics.ContainsKey(MostTradedSecurityTradeCountStatistic))
                    {
                        throw new Exception($"Statistic {MostTradedSecurityTradeCountStatistic} should not be set yet");
                    }
                }
                else
                {
                    // The current most traded security should be set in the summary
                    mostTradeSecurityKvp = _tradeCounts.MaxBy(kvp => kvp.Value);
                    CheckMostTradedSecurityStatistic(statistics, mostTradeSecurityKvp.Key, mostTradeSecurityKvp.Value);
                }

                // Update the trade count
                var tradeCount = _tradeCounts.GetValueOrDefault(orderEvent.Symbol);
                _tradeCounts[orderEvent.Symbol] = tradeCount + 1;

                // Set the most traded security
                mostTradeSecurityKvp = _tradeCounts.MaxBy(kvp => kvp.Value);
                SetSummaryStatistic(MostTradedSecurityStatistic, mostTradeSecurityKvp.Key);
                SetSummaryStatistic(MostTradedSecurityTradeCountStatistic, mostTradeSecurityKvp.Value);

                // Re-calculate statistics:
                statistics = Statistics.Summary;

                // Let's keep track of our custom summary statistics after the update
                CheckMostTradedSecurityStatistic(statistics, mostTradeSecurityKvp.Key, mostTradeSecurityKvp.Value);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var statistics = Statistics.Summary;
            if (!statistics.ContainsKey(MostTradedSecurityStatistic))
            {
                throw new Exception($"Statistic {MostTradedSecurityStatistic} should be in the summary statistics");
            }
            if (!statistics.ContainsKey(MostTradedSecurityTradeCountStatistic))
            {
                throw new Exception($"Statistic {MostTradedSecurityTradeCountStatistic} should be in the summary statistics");
            }
            var mostTradeSecurityKvp = _tradeCounts.MaxBy(kvp => kvp.Value);
            CheckMostTradedSecurityStatistic(statistics, mostTradeSecurityKvp.Key, mostTradeSecurityKvp.Value);
        }

        private void CheckMostTradedSecurityStatistic(Dictionary<string, string> statistics, Symbol mostTradedSecurity, int tradeCount)
        {
            var mostTradedSecurityStatistic = statistics[MostTradedSecurityStatistic];
            var mostTradedSecurityTradeCountStatistic = statistics[MostTradedSecurityTradeCountStatistic];
            Log($"Most traded security: {mostTradedSecurityStatistic}");
            Log($"Most traded security trade count: {mostTradedSecurityTradeCountStatistic}");

            if (mostTradedSecurityStatistic != mostTradedSecurity)
            {
                throw new Exception($"Most traded security should be {mostTradedSecurity} but it is {mostTradedSecurityStatistic}");
            }
            if (mostTradedSecurityTradeCountStatistic != tradeCount.ToStringInvariant())
            {
                throw new Exception($"Most traded security trade count should be {tradeCount} but it is {mostTradedSecurityTradeCountStatistic}");
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
            {"Total Orders", "94"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "18.903%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0.135"},
            {"Start Equity", "100000"},
            {"End Equity", "100221.61"},
            {"Net Profit", "0.222%"},
            {"Sharpe Ratio", "6.406"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "69.072%"},
            {"Loss Rate", "70%"},
            {"Win Rate", "30%"},
            {"Profit-Loss Ratio", "2.73"},
            {"Alpha", "-0.144"},
            {"Beta", "0.264"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-9.751"},
            {"Tracking Error", "0.164"},
            {"Treynor Ratio", "1.43"},
            {"Total Fees", "$114.39"},
            {"Estimated Strategy Capacity", "$1100000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "549.26%"},
            {"Most Traded Security", "IBM"},
            {"Most Traded Security Trade Count", "63"},
            {"OrderListHash", "8dd77e35338a81410a5b68dc8345f402"}
        };
    }
}
