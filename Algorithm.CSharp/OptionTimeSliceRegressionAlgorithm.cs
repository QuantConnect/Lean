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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test time slice irregularities when adding options
    /// after algorithm initialization
    /// </summary>
    public class OptionTimeSliceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private Symbol _optionSymbol;
        private DateTime _lastSliceTime = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 9);

            var aapl = AddEquity("aapl", Resolution.Minute);
            aapl.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _symbol = aapl.Symbol;
        }

        public override void OnData(Slice data)
        {
            // Compare our previous slice time to this slice
            // Because of issues with Delisting data we have to let Auxiliary data pass through GH #5207
            if (Time.Ticks - _lastSliceTime.Ticks < 1000 && data.Values.Any(x => x.DataType != MarketDataType.Auxiliary))
            {
                throw new Exception($"Emitted two slices within 1000 ticks of each other.");
            }

            // Store our slice time
            _lastSliceTime = Time;

            var underlyingPrice = Securities[_symbol].Price;
            var contractSymbol = OptionChainProvider.GetOptionContractList(_symbol, Time)
                .Where(x => x.ID.StrikePrice - underlyingPrice > 0)
                .OrderBy(x => x.ID.Date)
                .FirstOrDefault();

            if (contractSymbol != null)
            {
                _optionSymbol = AddOptionContract(contractSymbol).Symbol;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_optionSymbol == null)
            {
                throw new Exception("No option symbol was added!");
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
        public long DataPoints => 7437;

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
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}

