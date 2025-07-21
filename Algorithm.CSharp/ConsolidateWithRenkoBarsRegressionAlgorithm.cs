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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    public class ConsolidateWithRenkoBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private IDataConsolidator _renkoConsolidator;
        private IDataConsolidator _volumeRenkoConsolidator;
        private IDataConsolidator _consolidator1;
        private IDataConsolidator _consolidator2;
        private Symbol _spy;
        private List<SimpleMovingAverage> _smas;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 8);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;

            _smas = new List<SimpleMovingAverage>
            {
                new SimpleMovingAverage("SMA1", 2),
                new SimpleMovingAverage("SMA2", 2),
                new SimpleMovingAverage("SMA3", 2),
                new SimpleMovingAverage("SMA4", 2)
            };

            _renkoConsolidator = Consolidate<RenkoBar>(_spy, 0.1m, bar => UpdateRenkoBar1(bar));
            _volumeRenkoConsolidator = Consolidate<VolumeRenkoBar>(_spy, 10000m, bar => UpdateVolumeRenkoBar1(bar));
            _consolidator1 = Consolidate(_spy, 0.1m, bar => UpdateRenkoBar2(bar));
            _consolidator2 = Consolidate(_spy, 10000m, bar => UpdateVolumeRenkoBar2(bar));
        }

        private void UpdateRenkoBar1(RenkoBar renkoBar)
        {
            _smas[0].Update(renkoBar.EndTime, renkoBar.High);
        }

        private void UpdateRenkoBar2(RenkoBar renkoBar)
        {
            _smas[1].Update(renkoBar.EndTime, renkoBar.High);
        }

        private void UpdateVolumeRenkoBar1(VolumeRenkoBar volumeRenkoBar)
        {
            _smas[2].Update(volumeRenkoBar.EndTime, volumeRenkoBar.High);
        }
        private void UpdateVolumeRenkoBar2(VolumeRenkoBar volumeRenkoBar)
        {
            _smas[3].Update(volumeRenkoBar.EndTime, volumeRenkoBar.High);
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var sma in _smas)
            {
                if (!sma.IsReady)
                {
                    throw new RegressionTestException($"{sma.Name} is not ready");
                }
                if (sma.Samples == 0)
                {
                    throw new RegressionTestException($"{sma.Name} was never called");
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
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1582;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
