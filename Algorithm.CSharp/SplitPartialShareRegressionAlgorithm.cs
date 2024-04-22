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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the split is handled correctly. Specifically GH issue #5765, where cash
    /// difference applied due to share count difference was using the split reference price instead of the new price,
    /// increasing cash holdings by a higher amount than it should have
    /// </summary>
    public class SplitPartialShareRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private decimal _cash;
        private SplitType? _splitType;
        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 09);

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            AddEquity("AAPL");
        }

        public override void OnData(Slice data)
        {
            foreach (var dataSplit in data.Splits)
            {
                if (_splitType == null || _splitType < dataSplit.Value.Type)
                {
                    _splitType = dataSplit.Value.Type;

                    if (_splitType == SplitType.Warning && _cash != Portfolio.CashBook[Currencies.USD].Amount)
                    {
                        throw new Exception("Unexpected cash amount change before split");
                    }

                    if (_splitType == SplitType.SplitOccurred)
                    {
                        var newCash = Portfolio.CashBook[Currencies.USD].Amount;
                        if (_cash == newCash || newCash - _cash >= dataSplit.Value.SplitFactor * dataSplit.Value.ReferencePrice)
                        {
                            throw new Exception("Unexpected cash amount change after split");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unexpected split event {dataSplit.Value.Type}");
                }
            }

            if (!Portfolio.Invested)
            {
                Buy("AAPL", 1);
                _cash = Portfolio.CashBook[Currencies.USD].Amount;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_splitType == null)
            {
                throw new Exception("No split was emitted!");
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
        public long DataPoints => 2371;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.562%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100007.16"},
            {"Net Profit", "0.007%"},
            {"Sharpe Ratio", "-3.983"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "79.393%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0"},
            {"Beta", "-0.007"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-11.436"},
            {"Tracking Error", "0.037"},
            {"Treynor Ratio", "0.431"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$4200000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.13%"},
            {"OrderListHash", "87f55de4577d35a6ff70a7fd335e14a4"}
        };
    }
}
