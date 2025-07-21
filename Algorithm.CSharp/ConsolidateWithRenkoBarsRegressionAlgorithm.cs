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
    public class ConsolidateWithRenkoBarsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private List<SimpleMovingAverage> _smaIndicators;
        private IDataConsolidator _renkoConsolidator;
        private IDataConsolidator _volumeRenkoConsolidator;
        private IDataConsolidator _genericRenkoConsolidator;
        private IDataConsolidator _genericVolumeRenkoConsolidator;

        /// <summary>
        /// Initializes the algorithm.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 8);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;

            _smaIndicators = new List<SimpleMovingAverage>()
            {
                new SimpleMovingAverage("RenkoBarSMA", 2),
                new SimpleMovingAverage("GenericRenkoBarSMA", 2),
                new SimpleMovingAverage("VolumeRenkoBarSMA", 2),
                new SimpleMovingAverage("GenericVolumeRenkoBarSMA", 2)
            };
            InitializeConsolidators();
        }

        /// <summary>
        /// Initializes and configures all consolidators.
        /// </summary>
        private void InitializeConsolidators()
        {
            // Specific Renko consolidator
            _renkoConsolidator = Consolidate<RenkoBar>(
                _spy,
                barSize: 0.1m,
                handler: renkoBar => UpdateRenkoBarSma(renkoBar)
            );

            // Generic consolidator producing Renko bars
            _genericRenkoConsolidator = Consolidate(
                _spy,
                barSize: 0.1m,
                handler: data => UpdateGenericRenkoBarSma(data)
            );

            // Specific VolumeRenko consolidator
            _volumeRenkoConsolidator = Consolidate<VolumeRenkoBar>(
                _spy,
                10000m,
                handler: volumeRenkoBar => UpdateVolumeRenkoBarSma(volumeRenkoBar)
            );

            // Generic consolidator producing VolumeRenko bars
            _genericVolumeRenkoConsolidator = Consolidate(
                _spy,
                10000m,
                handler: data => UpdateGenericVolumeRenkoBarSma(data)
            );
        }

        /// <summary>
        /// Updates the SMA for specific RenkoBar consolidator.
        /// </summary>
        private void UpdateRenkoBarSma(RenkoBar renkoBar)
        {
            _smaIndicators[0].Update(renkoBar.EndTime, renkoBar.High);
        }

        /// <summary>
        /// Updates the SMA for generic RenkoBar consolidator.
        /// </summary>
        private void UpdateGenericRenkoBarSma(RenkoBar renkoBar)
        {
            _smaIndicators[1].Update(renkoBar.EndTime, renkoBar.High);
        }

        /// <summary>
        /// Updates the SMA for specific VolumeRenkoBar consolidator.
        /// </summary>
        private void UpdateVolumeRenkoBarSma(VolumeRenkoBar volumeRenkoBar)
        {
            _smaIndicators[2].Update(volumeRenkoBar.EndTime, volumeRenkoBar.High);
        }

        /// <summary>
        /// Updates the SMA for generic VolumeRenkoBar consolidator.
        /// </summary>
        private void UpdateGenericVolumeRenkoBarSma(VolumeRenkoBar volumeRenkoBar)
        {
            _smaIndicators[3].Update(volumeRenkoBar.EndTime, volumeRenkoBar.High);
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var sma in _smaIndicators)
            {
                if (!sma.IsReady)
                {
                    throw new RegressionTestException($"{sma.Name} is not ready");
                }
                if (sma.Samples == 0)
                {
                    throw new RegressionTestException($"{sma.Name} was never updated");
                }
            }
            if (_smaIndicators[0].Current.Value != _smaIndicators[1].Current.Value)
            {
                throw new RegressionTestException($"RenkoBarSMA and GenericRenkoBarSMA should have the same value");
            }
            if (_smaIndicators[2].Current.Value != _smaIndicators[3].Current.Value)
            {
                throw new RegressionTestException($"VolumeRenkoBarSMA and GenericVolumeRenkoBarSMA should have the same value");
            }
            if (!(_renkoConsolidator is RenkoConsolidator))
            {
                throw new RegressionTestException($"RenkoConsolidator should be of type RenkoConsolidator");
            }
            if (!(_genericRenkoConsolidator is RenkoConsolidator))
            {
                throw new RegressionTestException($"GenericRenkoConsolidator should be of type RenkoConsolidator");
            }
            if (!(_volumeRenkoConsolidator is VolumeRenkoConsolidator))
            {
                throw new RegressionTestException($"VolumeRenkoConsolidator should be of type VolumeRenkoConsolidator");
            }
            if (!(_genericVolumeRenkoConsolidator is VolumeRenkoConsolidator))
            {
                throw new RegressionTestException($"GenericVolumeRenkoConsolidator should be of type VolumeRenkoConsolidator");
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
