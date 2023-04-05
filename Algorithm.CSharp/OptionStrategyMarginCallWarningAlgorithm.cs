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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCallWarning"/> event is fired when trading options strategies
    /// </summary>
    public class OptionStrategyMarginCallWarningAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private OptionStrategy _optionStrategy;
        private bool _onMarginCallWarningWasCalled;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(163000);

            var equity = AddEquity("GOOG");
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                .Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                        .GroupBy(x => x.Expiry)
                        .OrderBy(grouping => grouping.Key)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    var expiry = callContracts[0].Expiry;
                    var lowerStrike = callContracts[0].Strike;
                    var middleStrike = callContracts[1].Strike;
                    var higherStrike = callContracts[2].Strike;

                    _optionStrategy = OptionStrategies.Straddle(_optionSymbol, higherStrike, expiry);
                    Order(_optionStrategy, -5);
                }
            }
        }

        public override void OnMarginCallWarning()
        {
            _onMarginCallWarningWasCalled = true;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new Exception("Portfolio should be invested");
            }

            if (!_onMarginCallWarningWasCalled)
            {
                throw new Exception("Expected OnMarginCallWarning to be invoked");
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
        public long DataPoints => 475788;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$12.50"},
            {"Estimated Strategy Capacity", "$490000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "8.10%"},
            {"OrderListHash", "4a7e52cd0db1a02674128d743aa0768c"}
        };
    }
}
