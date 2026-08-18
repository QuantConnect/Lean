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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example and regression algorithm asserting the behavior of <see cref="QCAlgorithm.DeregisterAll(Symbol)"/>:
    /// on security removal, a single call disposes all the indicators and consolidators created for it through the
    /// algorithm helper methods, so add/remove churn doesn't leak consolidators, and re-adding the security with
    /// fresh indicators keeps working
    /// </summary>
    public class DeregisterAllRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _ibm;
        private RelativeStrengthIndex _ibmRsi;
        private SimpleMovingAverage _ibmSma;
        private SimpleMovingAverage _newIbmSma;
        private int _ibmConsolidatedCount;
        private int _ibmConsolidatedCountAtRemoval;
        private long _ibmRsiSamplesAtRemoval;
        private bool _removed;
        private bool _readded;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            _spy = AddEquity("SPY").Symbol;
            _ibm = AddEquity("IBM").Symbol;

            // per symbol state created through the helper methods, tracked by the engine
            _ibmRsi = RSI(_ibm, 14, resolution: Resolution.Minute);
            _ibmSma = SMA(_ibm, 10, Resolution.Minute);
            Consolidate(_ibm, Resolution.Hour, (TradeBar bar) => _ibmConsolidatedCount++);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!_removed && Time.Day == 8)
            {
                _removed = true;
                RemoveSecurity(_ibm);
            }
            else if (_removed && !_readded && Time.Day == 10)
            {
                _readded = true;
                // re-adding the security after cleanup works: the helpers create fresh consolidators
                AddEquity("IBM");
                _newIbmSma = SMA(_ibm, 10, Resolution.Minute);
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 0.5m);
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Security additions/removals for this time step</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                // single call cleanup of every helper-created indicator and consolidator of the removed security
                DeregisterAll(security.Symbol);

                if (security.Symbol == _ibm)
                {
                    _ibmRsiSamplesAtRemoval = _ibmRsi.Samples;
                    _ibmConsolidatedCountAtRemoval = _ibmConsolidatedCount;

                    if (_ibmRsi.Consolidators.Count != 0 || _ibmSma.Consolidators.Count != 0)
                    {
                        throw new RegressionTestException("The removed security indicators should have no consolidators after DeregisterAll");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_removed || _ibmRsiSamplesAtRemoval == 0)
            {
                throw new RegressionTestException("The security should have been removed and its indicators deregistered");
            }
            if (_ibmRsi.Samples != _ibmRsiSamplesAtRemoval || _ibmConsolidatedCount != _ibmConsolidatedCountAtRemoval)
            {
                throw new RegressionTestException("Deregistered indicators and consolidators should have stopped getting updates");
            }
            if (_newIbmSma == null || !_newIbmSma.IsReady)
            {
                throw new RegressionTestException("Indicators created after re-adding the security should be getting updates");
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
        public long DataPoints => 6285;

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
            {"Compounding Annual Return", "93.262%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100845.96"},
            {"Net Profit", "0.846%"},
            {"Sharpe Ratio", "6.447"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.235%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.268"},
            {"Beta", "0.496"},
            {"Annual Standard Deviation", "0.11"},
            {"Annual Variance", "0.012"},
            {"Information Ratio", "-11.27"},
            {"Tracking Error", "0.112"},
            {"Treynor Ratio", "1.435"},
            {"Total Fees", "$1.72"},
            {"Estimated Strategy Capacity", "$87000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "9.96%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "d17dbd01fd291aab1eb04cf714ceba93"}
        };
    }
}
