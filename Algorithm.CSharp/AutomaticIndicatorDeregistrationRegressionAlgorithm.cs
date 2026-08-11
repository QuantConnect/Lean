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
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example and regression algorithm asserting the behavior of <see cref="IAlgorithmSettings.AutomaticIndicatorDeregistration"/>:
    /// when enabled, helper-created indicators are automatically deregistered when their security is removed from the
    /// algorithm, without any explicit cleanup call
    /// </summary>
    public class AutomaticIndicatorDeregistrationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private Symbol _ibm;
        private RelativeStrengthIndex _ibmRsi;
        private SimpleMovingAverage _ibmSma;
        private bool _removed;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            Settings.AutomaticIndicatorDeregistration = true;

            _spy = AddEquity("SPY").Symbol;
            _ibm = AddEquity("IBM").Symbol;

            _ibmRsi = RSI(_ibm, 14, resolution: Resolution.Minute);
            _ibmSma = SMA(_ibm, 10, Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!_removed && Time.Day == 9)
            {
                _removed = true;
                RemoveSecurity(_ibm);
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 0.5m);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_removed)
            {
                throw new RegressionTestException("The security should have been removed");
            }
            // the indicators were deregistered by the engine when the security was completely removed,
            // no explicit cleanup call needed
            if (_ibmRsi.Consolidators.Count != 0 || _ibmSma.Consolidators.Count != 0)
            {
                throw new RegressionTestException("The removed security indicators should have been automatically deregistered");
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
        public long DataPoints => 5506;

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
