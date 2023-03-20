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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp 
{
    /// <summary>
    /// Demonstrates the use of <see cref="RangeBarConsolidator"/> for creating range bars
    /// </summary>
    /// <meta name="tag" content="range bar" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="consolidating data" />
    public class RangeBarConsolidatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int BarLengthPips = 10;
        private RangeBarConsolidator _rangeBarConsolidator;
        private Symbol _ibm;
        private decimal _length;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);

            var security = AddEquity("IBM", Resolution.Tick, dataNormalizationMode: DataNormalizationMode.Raw);
            var priceVariation = security.SymbolProperties.MinimumPriceVariation;
            _ibm = security.Symbol;

            _length = BarLengthPips * priceVariation;
            _rangeBarConsolidator = new RangeBarConsolidator(_length);

            _rangeBarConsolidator.DataConsolidated += RangeBarConsolidatorOnDataConsolidated;
        }

        private void RangeBarConsolidatorOnDataConsolidated(object sender, RangeBar rangeBar)
        {
            var barLength = rangeBar.High - rangeBar.Low;
            if (barLength > _length)
            {
                throw new Exception(
                    $"Unexpected range bar length. Expected: {_length}. Current: {barLength} pips");
            }
        }

        public override void OnData(Slice slice)
        {
            if (slice.Ticks.ContainsKey(_ibm))
            {
                foreach (var tick in slice.Ticks[_ibm])
                {
                    _rangeBarConsolidator.Update(tick);
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints { get; }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
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
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
