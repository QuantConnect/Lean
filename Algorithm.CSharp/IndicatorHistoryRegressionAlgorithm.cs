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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of the indicator history api
    /// </summary>
    public class IndicatorHistoryRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialize the data and resolution you require for your strategy
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;
        }

        public void OnData(Slice slice)
        {
            var bollingerBands = new BollingerBands("BB", 20, 2.0m, MovingAverageType.Simple);

            if (bollingerBands.Window.IsReady)
            {
                throw new RegressionTestException("Unexpected ready bollinger bands state");
            }

            var indicatorHistory = IndicatorHistory(bollingerBands, _symbol, 50);

            if (!bollingerBands.Window.IsReady)
            {
                throw new RegressionTestException("Unexpected not ready bollinger bands state");
            }

            // we ask for 50 data points
            if (indicatorHistory.Count != 50)
            {
                throw new RegressionTestException($"Unexpected indicators values {indicatorHistory.Count}");
            }

            foreach (var indicatorDataPoints in indicatorHistory)
            {
                var upperBand = ((dynamic)indicatorDataPoints).UpperBand;
                Debug($"BB @{indicatorDataPoints.Current}: middleband: {indicatorDataPoints["middleband"]} upperBand {upperBand}");

                if (indicatorDataPoints == 0)
                {
                    throw new RegressionTestException($"Unexpected indicators point {indicatorDataPoints}");
                }
            }

            var currentValues = indicatorHistory.Current;
            if (currentValues.Count != 50 || currentValues.Any(x => x.Value == 0))
            {
                throw new RegressionTestException($"Unexpected indicators current values {currentValues.Count}");
            }
            var upperBandPoints = indicatorHistory["UpperBand"];
            if (upperBandPoints.Count != 50 || upperBandPoints.Any(x => x.Value == 0))
            {
                throw new RegressionTestException($"Unexpected indicators upperBandPoints values {upperBandPoints.Count}");
            }

            // We are done now!
            Quit();
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
        public long DataPoints => 16;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 70;

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
