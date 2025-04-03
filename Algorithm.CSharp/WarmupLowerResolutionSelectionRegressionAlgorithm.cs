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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting coarse universe selection behaves correctly during warmup when <see cref="IAlgorithmSettings.WarmupResolution"/> is set
    /// </summary>
    public class WarmupLowerResolutionSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private Queue<DateTime> _selection = new Queue<DateTime>(new[]
        {
            new DateTime(2014, 03, 24),

            new DateTime(2014, 03, 25),
            new DateTime(2014, 03, 26),
            new DateTime(2014, 03, 27),
            new DateTime(2014, 03, 28),
            new DateTime(2014, 03, 29),

            new DateTime(2014, 04, 01),
            new DateTime(2014, 04, 02),
            new DateTime(2014, 04, 03),
            new DateTime(2014, 04, 04),
            new DateTime(2014, 04, 05),
        });

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Hour;

            SetStartDate(2014, 03, 26);
            SetEndDate(2014, 04, 07);

            AddUniverse(CoarseSelectionFunction);
            SetWarmup(2, Resolution.Daily);
        }

        // sort the data by daily dollar volume and take the top 'NumberOfSymbols'
        private IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            var expected = _selection.Dequeue();
            if (expected != Time && !LiveMode)
            {
                throw new RegressionTestException($"Unexpected selection time: {Time}. Expected {expected}");
            }

            Debug($"Coarse selection happening at {Time} {IsWarmingUp}");
            return new[] { _spy };
        }

        public override void OnData(Slice slice)
        {
            var expectedDataSpan = QuantConnect.Time.OneHour;
            if (Time <= StartDate)
            {
                expectedDataSpan = TimeSpan.FromHours(6.5);
            }

            foreach (var data in slice.Values)
            {
                var dataSpan = data.EndTime - data.Time;
                if (dataSpan != expectedDataSpan)
                {
                    throw new RegressionTestException($"Unexpected bar span! {data}: {dataSpan} Expected {expectedDataSpan}");
                }
            }

            Debug($"OnData({UtcTime:o}): {IsWarmingUp}. {string.Join(", ", slice.Values.OrderBy(x => x.Symbol))}");

            if (!Portfolio.Invested && !IsWarmingUp)
            {
                SetHoldings(_spy, 1m);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 78098;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Compounding Annual Return", "-32.091%"},
            {"Drawdown", "2.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98631.08"},
            {"Net Profit", "-1.369%"},
            {"Sharpe Ratio", "-0.749"},
            {"Sortino Ratio", "-0.822"},
            {"Probabilistic Sharpe Ratio", "35.938%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0"},
            {"Beta", "0.997"},
            {"Annual Standard Deviation", "0.097"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "0.644"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "-0.073"},
            {"Total Fees", "$3.06"},
            {"Estimated Strategy Capacity", "$120000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "7.75%"},
            {"OrderListHash", "520072ff9cd8239e46c8cf78b67ed4c7"}
        };
    }
}
