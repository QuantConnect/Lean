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

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    /// <summary>
    /// Validates the <see cref="Correlation"/> indicator by ensuring no mismatch between the last computed value 
    /// and the expected value. Also verifies proper functionality across different time zones.
    /// </summary>
    public class CorrelationLastComputedValueRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Correlation _correlationPearson;
        private decimal _lastCorrelationValue;
        private decimal _totalCount;
        private decimal _matchingCount;

        public override void Initialize()
        {
            SetStartDate(2015, 05, 08);
            SetEndDate(2017, 06, 15);

            EnableAutomaticIndicatorWarmUp = true;
            AddCrypto("BTCUSD", Resolution.Daily);
            AddEquity("SPY", Resolution.Daily);

            _correlationPearson = C("BTCUSD", "SPY", 3, CorrelationType.Pearson, Resolution.Daily);
            if (!_correlationPearson.IsReady)
            {
                throw new RegressionTestException("Correlation indicator was expected to be ready");
            }
            _lastCorrelationValue = _correlationPearson.Current.Value;
            _totalCount = 0;
            _matchingCount = 0;
        }

        public override void OnData(Slice slice)
        {
            if (_lastCorrelationValue == _correlationPearson[1].Value)
            {
                _matchingCount++;
            }
            Debug($"CorrelationPearson between BTCUSD and SPY - Current: {_correlationPearson[0].Value}, Previous: {_correlationPearson[1].Value}");
            _lastCorrelationValue = _correlationPearson.Current.Value;
            _totalCount++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_totalCount == 0)
            {
                throw new RegressionTestException("No data points were processed.");
            }
            if (_totalCount != _matchingCount)
            {
                throw new RegressionTestException("Mismatch in the last computed CorrelationPearson values.");
            }
            Debug($"{_totalCount} data points were processed, {_matchingCount} matched the last computed value.");
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5798;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 21;

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
            {"Start Equity", "100000.00"},
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
            {"Information Ratio", "-0.616"},
            {"Tracking Error", "0.111"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
