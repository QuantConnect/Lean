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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that we can liquidate an existing option position with an option strategy.
    ///
    /// This specific case rolls out a front month put to a back month put using a calendar spread, working in two steps:
    ///     1. Short front month put
    ///     2. Roll out front month put to back month put using a calendar spread.
    /// </summary>
    public class RollOutFrontMonthToBackMonthOptionUsingCalendarSpreadRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private Symbol _frontMonthPutSymbol;
        private Symbol _backMonthPutSymbol;
        private decimal _atmStrike;
        private bool _done;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(500000);

            var option = AddOption("GOOG", Resolution.Minute);
            option.SetFilter(universe => universe.Strikes(-1, 1).Expiration(0, 62));

            _symbol = option.Symbol;
        }

        public override void OnData(Slice data)
        {
            if (_done || !data.OptionChains.TryGetValue(_symbol, out var chain) || !chain.Any())
            {
                return;
            }

            var isFirstStep = !Portfolio.Invested;
            if (isFirstStep)
            {
                _atmStrike = chain.MinBy(x => Math.Abs(x.Strike - chain.Underlying.Price)).Strike;
            }

            var puts = chain.Where(x => x.Strike == _atmStrike && x.Right == OptionRight.Put).ToList();

            if (isFirstStep)
            {
                if (puts.Count == 0)
                {
                    return;
                }

                // Step 1: short front month put
                _frontMonthPutSymbol = puts.MinBy(x => x.Expiry).Symbol;
                Sell(_frontMonthPutSymbol, 1);
            }
            else if (puts.Count > 1)
            {
                // Step 2: roll out front month put to back month put using a calendar spread.
                // Near expiry contract would be the same we shorted in step 1 (closets expiry, same strike),
                // which we want to roll out to the farther expiry
                var frontMonthExpiry = puts[0].Expiry;
                var backMonthExpiry = puts[puts.Count - 1].Expiry;
                var optionStrategy = OptionStrategies.PutCalendarSpread(_symbol, _atmStrike, frontMonthExpiry, backMonthExpiry);
                var tickets = Sell(optionStrategy, 1);

                if (!tickets.Any(ticket => ticket.Symbol == _frontMonthPutSymbol && ticket.Quantity == 1))
                {
                    throw new Exception($"Expected to find a ticket for {_frontMonthPutSymbol} with quantity {-Securities[_frontMonthPutSymbol].Holdings.Quantity}");
                }

                _backMonthPutSymbol = tickets.First(ticket => ticket.Symbol != _frontMonthPutSymbol).Symbol;
                _done = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_done)
            {
                throw new Exception("Expected the algorithm to have bought and sold a Bull Call Spread and a Bear Put Spread.");
            }

            if (Portfolio.Positions.Groups.Count != 1)
            {
                throw new Exception($"Expected 1 position group, found {Portfolio.Positions.Groups.Count}");
            }

            var positions = Portfolio.Positions.Groups.Single().Positions.ToList();
            if (positions.Count != 1)
            {
                throw new Exception($"Expected 1 position in the position group, found {positions.Count()}");
            }

            // The position should correspond to the far expiry contract
            var position = positions[0];
            if (position.Symbol != _backMonthPutSymbol)
            {
                throw new Exception($"Expected final portfolio position to be {_backMonthPutSymbol}, found {position.Symbol}");
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
        public long DataPoints => 464263;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "500000"},
            {"End Equity", "499792"},
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
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$190000.00"},
            {"Lowest Capacity Asset", "GOOCV 306CZK4DP0LC6|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.19%"},
            {"OrderListHash", "007124f0e2e4f0048f367782ef7fcd02"}
        };
    }
}
