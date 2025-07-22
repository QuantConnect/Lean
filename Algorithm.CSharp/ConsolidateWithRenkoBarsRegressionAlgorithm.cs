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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests the different overloads of the Consolidate method
    /// using both <see cref="RenkoBar"/> and <see cref="VolumeRenkoBar"/> types in LEAN.
    /// It verifies that each overload functions correctly when applied to these bar types,
    /// </summary>
    public class ConsolidateWithRenkoBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private List<SimpleMovingAverage> _smaIndicators;
        private IDataConsolidator _renkoConsolidator;
        private IDataConsolidator _genericRenkoConsolidator;
        private IDataConsolidator _volumeRenkoConsolidator;
        private IDataConsolidator _genericVolumeRenkoConsolidator;

        /// <summary>
        /// Initializes the algorithm.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 7);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Tick).Symbol;

            _smaIndicators = new List<SimpleMovingAverage>()
            {
                new SimpleMovingAverage("RenkoBarSMA", 2),
                new SimpleMovingAverage("GenericRenkoBarSMA", 2),

                new SimpleMovingAverage("VolumeRenkoBarSMA", 2),
                new SimpleMovingAverage("GenericVolumeRenkoBarSMA", 2),
            };
            _renkoConsolidator = Consolidate<RenkoBar>(_spy, 0.1m, renkoBar => UpdateRenkoBar(renkoBar, 0));
            _genericRenkoConsolidator = Consolidate(_spy, 0.1m, data => UpdateRenkoBar(data, 1));
            _volumeRenkoConsolidator = Consolidate<VolumeRenkoBar>(_spy, 10000m, volumeRenkoBar => UpdateVolumeRenkoBar(volumeRenkoBar, 2));
            _genericVolumeRenkoConsolidator = Consolidate(_spy, 10000m, data => UpdateVolumeRenkoBar(data, 3));
        }

        /// <summary>
        /// Updates the RenkoBar SMA indicator with the bar's high price.
        /// </summary>
        private void UpdateRenkoBar(RenkoBar renkoBar, int position)
        {
            _smaIndicators[position].Update(renkoBar.EndTime, renkoBar.High);
        }

        /// <summary>
        /// Updates the VolumeRenkoBar SMA indicator with the bar's high price.
        /// </summary>
        private void UpdateVolumeRenkoBar(VolumeRenkoBar volumeRenkoBar, int position)
        {
            _smaIndicators[position].Update(volumeRenkoBar.EndTime, volumeRenkoBar.High);
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var sma in _smaIndicators)
            {
                if (sma.Samples == 0)
                {
                    throw new RegressionTestException($"{sma.Name} was never updated");
                }
                if (!sma.IsReady)
                {
                    throw new RegressionTestException($"{sma.Name} is not ready");
                }
            }

            // Ensure RenkoBar SMAs are consistent and created with proper consolidators
            if (_renkoConsolidator is not RenkoConsolidator || _genericRenkoConsolidator is not RenkoConsolidator)
            {
                throw new RegressionTestException("RenkoConsolidator and GenericRenkoConsolidator should both be of type RenkoConsolidator");
            }
            if (_smaIndicators[0].Current.Value != _smaIndicators[1].Current.Value)
            {
                throw new RegressionTestException($"SMAs updated with RenkoBar data should have identical values.");
            }

            // Ensure VolumeRenkoBar SMAs are consistent and created with proper consolidators
            if (_volumeRenkoConsolidator is not VolumeRenkoConsolidator || _genericVolumeRenkoConsolidator is not VolumeRenkoConsolidator)
            {
                throw new RegressionTestException("VolumeRenkoConsolidator and GenericVolumeRenkoConsolidator should both be of type VolumeRenkoConsolidator");
            }

            if (_smaIndicators[2].Current.Value != _smaIndicators[3].Current.Value)
            {
                throw new RegressionTestException("SMAs updated with VolumeRenkoBar data should have identical current values.");
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
        public long DataPoints => 2857175;

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
