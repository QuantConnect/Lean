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
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the volatility models don't have big jumps due to price discontinuities on splits and dividends when using raw data
    /// </summary>
    public class VolatilityModelsWithRawDataAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;

        private int _splitsCount;
        private int _dividendsCount;

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(100000);

            var equity = AddEquity("AAPL", Resolution.Daily, dataNormalizationMode: DataNormalizationMode.Raw);
            equity.SetVolatilityModel(new StandardDeviationOfReturnsVolatilityModel(7));

            _aapl = equity.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (slice.Splits.ContainsKey(_aapl))
            {
                _splitsCount++;
            }

            if (slice.Dividends.ContainsKey(_aapl))
            {
                _dividendsCount++;
            }
        }

        public override void OnEndOfDay(Symbol symbol)
        {
            if (symbol != _aapl)
            {
                return;
            }

            // This is expected only in this case, 0.6 is not a magical number of any kind.
            // Just making sure we don't get big jumps on volatility
            if (Securities[_aapl].VolatilityModel.Volatility > 0.6m)
            {
                throw new Exception(
                    "Expected volatility to stay less than 0.6 (not big jumps due to price discontinuities on splits and dividends), " +
                    $"but got {Securities[_aapl].VolatilityModel.Volatility}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_splitsCount == 0 || _dividendsCount == 0)
            {
                throw new Exception($"Expected to receive at least one split and one dividend, but got {_splitsCount} splits and {_dividendsCount} dividends");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2022;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 40;

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
            {"Information Ratio", "-1.025"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
