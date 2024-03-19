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
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm illustrating how to request history data for different data mapping modes.
    /// </summary>
    public class HistoryWithDifferentDataMappingModeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _continuousContractSymbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2014, 1, 1);
            _continuousContractSymbol = AddFuture(Futures.Indices.SP500EMini, Resolution.Daily).Symbol;
        }

        public override void OnEndOfAlgorithm()
        {
            var dataMappingModes = ((DataMappingMode[])Enum.GetValues(typeof(DataMappingMode))).ToList();
            var historyResults = dataMappingModes.Select(dataMappingMode =>
            {
                return History(new [] { _continuousContractSymbol }, StartDate, EndDate, Resolution.Daily, dataMappingMode: dataMappingMode).ToList();
            }).ToList();

            if (historyResults.Any(x => x.Count != historyResults[0].Count))
            {
                throw new Exception("History results bar count did not match");
            }

            // Check that all history results have a mapping date at some point in the history
            HashSet<DateTime> mappingDates = new HashSet<DateTime>();
            for (int i = 0; i < historyResults.Count; i++)
            {
                var underlying = historyResults[i].First().Bars.Keys.First().Underlying;
                int mappingsCount = 0;

                foreach (var slice in historyResults[i])
                {
                    var dataUnderlying = slice.Bars.Keys.First().Underlying;
                    if (dataUnderlying != underlying)
                    {
                        underlying = dataUnderlying;
                        mappingsCount++;
                        mappingDates.Add(slice.Time.Date);
                    }
                }

                if (mappingsCount == 0)
                {
                    throw new Exception($"History results for {dataMappingModes[i]} data mapping mode did not contain any mappings");
                }
            }

            if (mappingDates.Count < dataMappingModes.Count)
            {
                throw new Exception($"History results should have had different mapping dates for each data mapping mode");
            }

            // Check that close prices at each time are different for different data mapping modes
            for (int j = 0; j < historyResults[0].Count; j++)
            {
                var closePrices = historyResults.Select(hr => hr[j].Bars.First().Value.Close).ToHashSet();
                if (closePrices.Count != dataMappingModes.Count)
                {
                    throw new Exception($"History results close prices should have been different for each data mapping mode at each time");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1578;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 488;

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
            {"Information Ratio", "-3.681"},
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
