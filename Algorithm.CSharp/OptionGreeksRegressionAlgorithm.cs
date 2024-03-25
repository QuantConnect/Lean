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
    /// Regression algorithm reproducing issue #7408
    /// </summary>
    public class OptionGreeksRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _itmCallSymbol, _otmCallSymbol, _itmPutSymbol, _otmPutSymbol;
        private const decimal error = 0.05m;

        public override void Initialize()
        {
            SetStartDate(2023, 8, 2);
            SetEndDate(2023, 8, 4);
            SetCash(1000000);

            var equity = AddEquity("SPY", Resolution.Minute);
            equity.VolatilityModel = new StandardDeviationOfReturnsVolatilityModel(30);

            _itmCallSymbol = QuantConnect.Symbol.CreateOption(equity.Symbol, Market.USA, OptionStyle.American, OptionRight.Call, 430, new DateTime(2023, 9, 1));
            _otmCallSymbol = QuantConnect.Symbol.CreateOption(equity.Symbol, Market.USA, OptionStyle.American, OptionRight.Call, 470, new DateTime(2023, 9, 1));
            _itmPutSymbol = QuantConnect.Symbol.CreateOption(equity.Symbol, Market.USA, OptionStyle.American, OptionRight.Put, 430, new DateTime(2023, 9, 1));
            _otmPutSymbol = QuantConnect.Symbol.CreateOption(equity.Symbol, Market.USA, OptionStyle.American, OptionRight.Put, 470, new DateTime(2023, 9, 1));

            AddOptionContract(_itmCallSymbol, Resolution.Minute);
            AddOptionContract(_otmCallSymbol, Resolution.Minute);
            AddOptionContract(_itmPutSymbol, Resolution.Minute);
            AddOptionContract(_otmPutSymbol, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            foreach (var kvp in slice.OptionChains)
            {
                var chain = kvp.Value;
                if (chain == null)
                {
                    continue;
                }

                foreach (var contractKvp in chain.Contracts)
                {
                    var symbol = contractKvp.Key;
                    var contract = contractKvp.Value;
                    var delta = contract.Greeks.Delta;
                    decimal expected;

                    // Values from CBOE
                    if (symbol == _itmCallSymbol)
                    {
                        expected = 0.78901m;
                    }
                    else if (symbol == _otmCallSymbol)
                    {
                        expected = 0.09627m;
                    }
                    else if (symbol == _itmPutSymbol)
                    {
                        expected = -0.18395m;
                    }
                    else
                    {
                        expected = -0.99989m;
                    }

                    if (delta >= expected + error || delta <= expected - error)
                    {
                        throw new Exception($"{symbol.Value} greeks not calculated accurately! Expected: {expected}, Estimation: {delta}");
                    }
                }

                Quit();
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
        public long DataPoints => 10;

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
            {"Start Equity", "1000000"},
            {"End Equity", "1000000"},
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
