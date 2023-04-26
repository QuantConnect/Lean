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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;

using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that we can open a position on two option strategies for the same underlying and then liquidate both of them.
    /// This reproduces GH issue #7205.
    ///
    /// The algorithm works in two steps:
    ///     1. Buy a bull call and a bear put spread.
    ///     2. Liquidate both spreads bough in step 1.
    ///        - The issue was on this step, the algorithm failed with the following error when attempting to liquidate the first spread:
    ///            Unable to create group for orders: [5,6]
    /// </summary>
    public class LiquidatingMultipleOptionStrategiesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        OptionStrategy _bullCallSpread;
        OptionStrategy _bearPutSpread;
        private bool _done;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 23);
            SetEndDate(2015, 12, 25);
            SetCash(500000);

            var option = AddOption("GOOG");
            option.SetFilter(universe => universe.Strikes(-3, 3).Expiration(0, 180));

            _symbol = option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (_done || !slice.OptionChains.TryGetValue(_symbol, out var chain))
            {
                return;
            }

            var calls = chain
                .Where(x => x.Right == OptionRight.Call)
                .GroupBy(x => x.Expiry)
                .FirstOrDefault(x => x.Count() > 2)
                ?.OrderBy(x => x.Strike)
                ?.ToList();
            var puts = chain
                .Where(x => x.Right == OptionRight.Put)
                .GroupBy(x => x.Expiry)
                .FirstOrDefault(x => x.Count() > 2)
                ?.OrderByDescending(x => x.Strike)
                ?.ToList();
            if (calls == null || puts == null)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                // Step 1: buy spreads

                _bullCallSpread = OptionStrategies.BullCallSpread(_symbol, calls[0].Strike, calls[1].Strike, calls[0].Expiry);
                Buy(_bullCallSpread, 1);

                _bearPutSpread = OptionStrategies.BearPutSpread(_symbol, puts[0].Strike, puts[1].Strike, puts[0].Expiry);
                Buy(_bearPutSpread, 1);
            }
            else
            {
                // Let's check that we have the right position groups, just to make sure we are good.
                var positionGroups = Portfolio.PositionGroups;
                if (positionGroups.Count != 2)
                {
                    throw new Exception($"Expected 2 position groups, one for each spread, but found {positionGroups.Count}");
                }

                var positionGroupMatchesSpreadStrategy = (IPositionGroup positionGroup, OptionStrategy strategy) =>
                {
                    return strategy.OptionLegs.All(leg =>
                    {
                        var legSymbol = QuantConnect.Symbol.CreateOption(strategy.Underlying, strategy.CanonicalOption?.ID?.Symbol,
                            strategy.Underlying.ID.Market, _symbol.ID.OptionStyle, leg.Right, leg.Strike, leg.Expiration);
                        return positionGroup.Positions.Any(position => position.Symbol == legSymbol);
                    });
                };
                if (!positionGroups.All(group =>
                        positionGroupMatchesSpreadStrategy(group, _bullCallSpread) || positionGroupMatchesSpreadStrategy(group, _bearPutSpread)))
                {
                    throw new Exception("Expected both spreads to have a matching position group in the portfolio.");
                }

                // Step 2: liquidate spreads

                Sell(_bullCallSpread, 1);
                Sell(_bearPutSpread, 1);
                _done = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_done)
            {
                throw new Exception("Expected the algorithm to have bought and sold a Bull Call Spread and a Bear Put Spread.");
            }

            if (Portfolio.Invested)
            {
                throw new Exception("The spreads should have been liquidated by the end of the algorithm");
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
        public long DataPoints => 527613;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8"},
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
            {"Total Fees", "$8.00"},
            {"Estimated Strategy Capacity", "$13000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.31%"},
            {"OrderListHash", "49bb33ac7a6370ebda5cb516ce69ff31"}
        };
    }
}
