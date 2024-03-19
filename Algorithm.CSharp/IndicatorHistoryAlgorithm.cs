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
    /// Demonstration algorithm of indicators history window usage
    /// </summary>
    public class IndicatorHistoryAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        private BollingerBands _bollingerBands;

        /// <summary>
        /// Initialize the data and resolution you require for your strategy
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(25000);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            _bollingerBands = BB(_symbol, 20, 2.0m, resolution: Resolution.Daily);
            // Let's keep BB values for a 20 day period
            _bollingerBands.Window.Size = 20;
            // Also keep the same period of data for the middle band
            _bollingerBands.MiddleBand.Window.Size = 20;
        }

        public void OnData(Slice slice)
        {
            // Let's wait for our indicator to fully initialize and have a full window of history data
            if (!_bollingerBands.Window.IsReady) return;

            // We can access the current and oldest (in our period) values of the indicator
            Log($"Current BB value: {_bollingerBands[0].EndTime} - {_bollingerBands[0].Value}");
            Log($@"Oldest BB value: {_bollingerBands[_bollingerBands.Window.Count - 1].EndTime} - {
                _bollingerBands[_bollingerBands.Window.Count - 1].Value}");

            // Let's log the BB values for the last 20 days, for demonstration purposes on how it can be enumerated
            foreach (var dataPoint in _bollingerBands)
            {
                Log($"BB @{dataPoint.EndTime}: {dataPoint.Value}");
            }

            // We can also do the same for internal indicators:
            var middleBand = _bollingerBands.MiddleBand;
            Log($"Current BB Middle Band value: {middleBand[0].EndTime} - {middleBand[0].Value}");
            Log($@"Oldest BB Middle Band value: {middleBand[middleBand.Window.Count - 1].EndTime} - {
                middleBand[middleBand.Window.Count - 1].Value}");
            foreach (var dataPoint in middleBand)
            {
                Log($"BB Middle Band @{dataPoint.EndTime}: {dataPoint.Value}");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 153;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Start Equity", "25000"},
            {"End Equity", "25000"},
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
            {"Information Ratio", "-7.209"},
            {"Tracking Error", "0.087"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
