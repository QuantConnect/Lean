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
    /// In this case, the algorithm tests the Iron Condor strategy.
    /// </summary>
    public class IronCondorStrategyAlgorithm : OptionStrategyFactoryMethodsBaseAlgorithm
    {
        protected override int ExpectedOrdersCount { get; } = 8;

        private OptionStrategy _ironCondor;

        protected override void TradeStrategy(OptionChain chain)
        {
            foreach (var group in chain.GroupBy(x => x.Expiry))
            {
                var expiry = group.Key;
                var contracts = group.OrderBy(x => x.Strike).ToList();
                if (contracts.Count < 4) continue;

                var putContracts = contracts.Where(x => x.Right == OptionRight.Put).ToList();
                if (putContracts.Count < 2) continue;
                var longPutStrike = putContracts[0].Strike;
                var shortPutStrike = putContracts[1].Strike;

                var callContracts = contracts.Where(x => x.Right == OptionRight.Call && x.Strike > shortPutStrike).ToList();
                if (callContracts.Count < 2) continue;
                var shortCallStrike = callContracts[0].Strike;
                var longCallStrike = callContracts[1].Strike;

                _ironCondor = OptionStrategies.IronCondor(_optionSymbol, longPutStrike, shortPutStrike, shortCallStrike, longCallStrike, expiry);
                Buy(_ironCondor, 2);
                break;
            }
        }

        protected override void AssertStrategyPositionGroup(IPositionGroup positionGroup)
        {
            if (positionGroup.Positions.Count() != 4)
            {
                throw new Exception($"Expected position group to have 4 positions. Actual: {positionGroup.Positions.Count()}");
            }

            var orderedStrikes = _ironCondor.OptionLegs.Select(leg => leg.Strike).OrderBy(x => x).ToArray();

            var longPutStrike = orderedStrikes[0];
            var longPutPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Put && x.Symbol.ID.StrikePrice == longPutStrike);
            if (longPutPosition.Quantity != 2)
            {
                throw new Exception($"Expected long put position quantity to be 2. Actual: {longPutPosition.Quantity}");
            }

            var shortPutStrike = orderedStrikes[1];
            var shortPutPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Put && x.Symbol.ID.StrikePrice == shortPutStrike);
            if (shortPutPosition.Quantity != -2)
            {
                throw new Exception($"Expected short put position quantity to be -2. Actual: {shortPutPosition.Quantity}");
            }

            var shortCallStrike = orderedStrikes[2];
            var shortCallPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Call && x.Symbol.ID.StrikePrice == shortCallStrike);
            if (shortCallPosition.Quantity != -2)
            {
                throw new Exception($"Expected short call position quantity to be -2. Actual: {shortCallPosition.Quantity}");
            }

            var longCallStrike = orderedStrikes[3];
            var longCallPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Call && x.Symbol.ID.StrikePrice == longCallStrike);
            if (longCallPosition.Quantity != 2)
            {
                throw new Exception($"Expected long call position quantity to be 2. Actual: {longCallPosition.Quantity}");
            }
        }

        protected override void LiquidateStrategy()
        {
            // We should be able to close the position by selling the strategy
            Sell(_ironCondor, 2);
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
            {"Total Orders", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999149.6"},
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
            {"Portfolio Turnover", "2.00%"},
            {"OrderListHash", "293b5b1c428514fc9d7bb069be75e5e9"}
        };
    }
}
