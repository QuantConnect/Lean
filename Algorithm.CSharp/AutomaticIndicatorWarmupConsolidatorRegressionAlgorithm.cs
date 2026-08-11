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
    /// Asserts that indicators registered with a consolidator are automatically warmed up when
    /// 'Settings.AutomaticIndicatorWarmUp' is enabled, without requiring a manual history replay
    /// </summary>
    public class AutomaticIndicatorWarmupConsolidatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private AverageTrueRange _atr;
        private RelativeStrengthIndex _rsi;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 09);

            Settings.AutomaticIndicatorWarmUp = true;
            _spy = AddEquity("SPY", Resolution.Minute).Symbol;

            // Test case 1: bar indicator registered with a consolidator, previously required a manual history replay
            _atr = new AverageTrueRange(10);
            RegisterIndicator(_spy, _atr, new TradeBarConsolidator(TimeSpan.FromMinutes(30)));
            AssertIsReady(_atr, expected: true);

            // Test case 2: data point indicator registered with a consolidator and a selector
            _rsi = new RelativeStrengthIndex(14);
            RegisterIndicator(_spy, _rsi, new TradeBarConsolidator(TimeSpan.FromMinutes(30)), Field.Close);
            AssertIsReady(_rsi, expected: true);

            // Test case 3: non time based consolidators cannot be automatically warmed up, the indicator is
            // registered but left cold
            var renkoRsi = new RelativeStrengthIndex(14);
            RegisterIndicator(_spy, renkoRsi, new RenkoConsolidator(1m), Field.Close);
            AssertIsReady(renkoRsi, expected: false);

            // Test case 4: with the setting disabled nothing is warmed up
            Settings.AutomaticIndicatorWarmUp = false;
            var notWarmed = new AverageTrueRange(10);
            RegisterIndicator(_spy, notWarmed, new TradeBarConsolidator(TimeSpan.FromMinutes(30)));
            AssertIsReady(notWarmed, expected: false);
            Settings.AutomaticIndicatorWarmUp = true;
        }

        private static void AssertIsReady(IIndicator indicator, bool expected)
        {
            if (indicator.IsReady != expected)
            {
                throw new RegressionTestException($"Expected {indicator.Name} IsReady to be {expected} but was {indicator.IsReady}");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
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
        public long DataPoints => 1582;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1500;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98848.47"},
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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$31000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "50.43%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "00636a25aed88acd2171c6221c747716"}
        };
    }
}
