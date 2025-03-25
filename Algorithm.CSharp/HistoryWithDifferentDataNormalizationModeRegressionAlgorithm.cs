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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm illustrating how to request history data for different data normalization modes.
    /// </summary>
    public class HistoryWithDifferentDataNormalizationModeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aaplEquitySymbol;
        private Symbol _esFutureSymbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2014, 1, 1);

            _aaplEquitySymbol = AddEquity("AAPL", Resolution.Daily).Symbol;
            _esFutureSymbol = AddFuture(Futures.Indices.SP500EMini, Resolution.Daily).Symbol;
        }

        public override void OnEndOfAlgorithm()
        {
            var equityDataNormalizationModes = new DataNormalizationMode[]{
                DataNormalizationMode.Raw,
                DataNormalizationMode.Adjusted,
                DataNormalizationMode.SplitAdjusted
            };
            CheckHistoryResultsForDataNormalizationModes(_aaplEquitySymbol, StartDate, EndDate, Resolution.Daily, equityDataNormalizationModes);

            var futureDataNormalizationModes = new DataNormalizationMode[]{
                DataNormalizationMode.Raw,
                DataNormalizationMode.BackwardsRatio,
                DataNormalizationMode.BackwardsPanamaCanal,
                DataNormalizationMode.ForwardPanamaCanal
            };
            CheckHistoryResultsForDataNormalizationModes(_esFutureSymbol, StartDate, EndDate, Resolution.Daily, futureDataNormalizationModes);
        }

        private void CheckHistoryResultsForDataNormalizationModes(Symbol symbol, DateTime start, DateTime end, Resolution resolution,
            DataNormalizationMode[] dataNormalizationModes)
        {
            var historyResults = dataNormalizationModes
                .Select(x => History(new [] { symbol }, start, end, resolution, dataNormalizationMode: x).ToList())
                .ToList();

            if (historyResults.Any(x => x.Count == 0 || x.Count != historyResults.First().Count))
            {
                throw new RegressionTestException($"History results for {symbol} have different number of bars");
            }

            // Check that, for each history result, close prices at each time are different for these securities (AAPL and ES)
            for (int j = 0; j < historyResults[0].Count; j++)
            {
                var closePrices = historyResults.Select(hr => hr[j].Bars.First().Value.Close).ToHashSet();
                if (closePrices.Count != dataNormalizationModes.Length)
                {
                    throw new RegressionTestException($"History results for {symbol} have different close prices at the same time");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1026;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 668;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-4.244"},
            {"Tracking Error", "0.086"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
