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
    /// Regression algorithm to test the behaviour of ARMA versus AR models at the same order of differencing.
    /// In particular, an ARIMA(1,1,1) and ARIMA(1,1,0) are instantiated while orders are placed if their difference
    /// is sufficiently large (which would be due to the inclusion of the MA(1) term).
    /// </summary>
    public class AutoRegressiveIntegratedMovingAverageRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private AutoRegressiveIntegratedMovingAverage _arima;
        private AutoRegressiveIntegratedMovingAverage _ar;
        private decimal _last;

        public override void Initialize()
        {
            SetStartDate(2013, 1, 07);
            SetEndDate(2013, 12, 11);

            EnableAutomaticIndicatorWarmUp = true;
            AddEquity("SPY", Resolution.Daily);
            _arima = ARIMA("SPY", 1, 1, 1, 50);
            _ar = ARIMA("SPY", 1, 1, 0, 50);
        }

        public override void OnData(Slice slice)
        {
            if (_arima.IsReady)
            {
                if (Math.Abs(_ar.Current.Value - _arima.Current.Value) > 1) // Difference due to MA(1) being included.
                {
                    if (_arima.Current.Value > _last)
                    {
                        MarketOrder("SPY", 1);
                    }
                    else
                    {
                        MarketOrder("SPY", -1);
                    }
                }

                _last = _arima.Current.Value;
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
        public long DataPoints => 1893;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 100;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "52"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "0.096%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "3.321"},
            {"Start Equity", "100000"},
            {"End Equity", "100089.09"},
            {"Net Profit", "0.089%"},
            {"Sharpe Ratio", "-8.214"},
            {"Sortino Ratio", "-9.025"},
            {"Probabilistic Sharpe Ratio", "40.893%"},
            {"Loss Rate", "24%"},
            {"Win Rate", "76%"},
            {"Profit-Loss Ratio", "4.67"},
            {"Alpha", "-0.008"},
            {"Beta", "0.008"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.961"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "-0.826"},
            {"Total Fees", "$52.00"},
            {"Estimated Strategy Capacity", "$32000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "fab920b5fc92a6e14d8128564249fbfa"}
        };
    }
}
