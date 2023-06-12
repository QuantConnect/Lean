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

        private IndicatorBase<IndicatorDataPoint> _sma;

        /// <summary>
        /// Initialize the data and resolution you require for your strategy
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(25000);

            _symbol = AddEquity("SPY", Resolution.Daily).Symbol;

            _sma = SMA(_symbol, 14, Resolution.Daily);
            // Let's keep SMA values for a 14 day period
            _sma.Window = 14;
        }

        public void OnData(Slice slice)
        {
            if (!_sma.IsReady || Portfolio.Invested) return;

            // The window is filled up, we have 14 days worth of SMA values to use at our convenience
            if (_sma.WindowCount == _sma.Window)
            {
                // Let's say that hypothetically, we want to buy shares of the equity when the SMA is less than its 14 days old value
                if (_sma[0] < _sma[_sma.WindowCount - 1])
                {
                    Buy(_symbol, 100);

                    // Let's log the SMA values for the last 14 days, for demonstration purposes on how it can be enumerated
                    foreach (var dataPoint in _sma)
                    {
                        Log($"SMA @{dataPoint.EndTime}: {dataPoint.Value}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new Exception("Expected the portfolio to be invested at the end of the algorithm");
            }
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
        public long DataPoints => 4031;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "7.998%"},
            {"Drawdown", "4.500%"},
            {"Expectancy", "0"},
            {"Net Profit", "16.635%"},
            {"Sharpe Ratio", "1.144"},
            {"Probabilistic Sharpe Ratio", "57.499%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.015"},
            {"Beta", "0.455"},
            {"Annual Standard Deviation", "0.049"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-1.769"},
            {"Tracking Error", "0.056"},
            {"Treynor Ratio", "0.123"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$1500000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.08%"},
            {"OrderListHash", "337525763b81bead1e0ca6f4e40115f3"}
        };
    }
}
