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

using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrate how to use OptionStrategies helper class to batch send orders for common strategies.
    /// In this case, the algorithm tests the Butterfly Put and Short Butterfly Put strategies.
    /// </summary>
    public class LongAndShortButterflyPutStrategiesAlgorithm : OptionStrategyFactoryMethodsBaseAlgorithm
    {
        protected override int ExpectedOrdersCount { get; } = 6;

        private OptionStrategy _butterflyPut;
        private OptionStrategy _shortButterflyPut;

        protected override void TradeStrategy(OptionChain chain)
        {
            var contractsByExpiry = chain.Where(x => x.Right == OptionRight.Put).GroupBy(x => x.Expiry);
            foreach (var group in contractsByExpiry)
            {
                var expiry = group.Key;
                var contracts = group.ToList();

                if (contracts.Count < 3)
                {
                    continue;
                }

                var strikes = contracts.Select(x => x.Strike).OrderBy(x => x).ToArray();
                var atmStrike = contracts.MinBy(x => Math.Abs(x.Strike - chain.Underlying.Value)).Strike;
                var spread = Math.Min(atmStrike - strikes[0], strikes[^1] - atmStrike);
                var itmStrike = atmStrike + spread;
                var otmStrike = atmStrike - spread;

                if (strikes.Contains(otmStrike) && strikes.Contains(itmStrike))
                {
                    // Ready to trade
                    _butterflyPut = OptionStrategies.ButterflyPut(_optionSymbol, itmStrike, atmStrike, otmStrike, expiry);
                    _shortButterflyPut = OptionStrategies.ShortButterflyPut(_optionSymbol, itmStrike, atmStrike, otmStrike, expiry);
                    Buy(_butterflyPut, 2);
                    break;
                }
            }
        }

        protected override void AssertStrategyPositionGroup(IPositionGroup positionGroup)
        {
            if (positionGroup.Positions.Count() != 3)
            {
                throw new Exception($"Expected position group to have 3 positions. Actual: {positionGroup.Positions.Count()}");
            }

            var higherStrike = _butterflyPut.OptionLegs.Max(leg => leg.Strike);
            var higherStrikePosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Put && x.Symbol.ID.StrikePrice == higherStrike);

            if (higherStrikePosition.Quantity != 2)
            {
                throw new Exception($"Expected higher strike position quantity to be 2. Actual: {higherStrikePosition.Quantity}");
            }

            var lowerStrike = _butterflyPut.OptionLegs.Min(leg => leg.Strike);
            var lowerStrikePosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Put && x.Symbol.ID.StrikePrice == lowerStrike);

            if (lowerStrikePosition.Quantity != 2)
            {
                throw new Exception($"Expected lower strike position quantity to be 2. Actual: {lowerStrikePosition.Quantity}");
            }

            var middleStrike = _butterflyPut.OptionLegs.Single(leg => leg.Strike < higherStrike && leg.Strike > lowerStrike).Strike;
            var middleStrikePosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Put && x.Symbol.ID.StrikePrice == middleStrike);

            if (middleStrikePosition.Quantity != -4)
            {
                throw new Exception($"Expected middle strike position quantity to be -4. Actual: {middleStrikePosition.Quantity}");
            }
        }

        protected override void LiquidateStrategy()
        {
            // We should be able to close the position using the inverse strategy (a short butterfly put)
            Buy(_shortButterflyPut, 2);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 4490;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999309.6"},
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
            {"Total Fees", "$10.40"},
            {"Estimated Strategy Capacity", "$4000.00"},
            {"Lowest Capacity Asset", "GOOCV 306CZL2DIL4G6|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "2.23%"},
            {"OrderListHash", "5edf17765f812f4b6114c03bb69a3bc2"}
        };
    }
}
