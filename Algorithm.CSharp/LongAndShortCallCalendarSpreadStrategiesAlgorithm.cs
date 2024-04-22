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
    /// In this case, the algorithm tests the Call Calendar Spread and Short Call Calendar Spread strategies.
    /// </summary>
    public class LongAndShortCallCalendarSpreadStrategiesAlgorithm : OptionStrategyFactoryMethodsBaseAlgorithm
    {
        protected override int ExpectedOrdersCount { get; } = 4;

        private OptionStrategy _callCalendarSpread;
        private OptionStrategy _shortCallCalendarSpread;

        protected override void TradeStrategy(OptionChain chain)
        {
            var contractsByStrike = chain
                .Where(x => x.Right == OptionRight.Call)
                .OrderBy(x => Math.Abs(x.Strike - chain.Underlying.Value))
                .GroupBy(x => x.Strike);
            foreach (var group in contractsByStrike)
            {
                var strike = group.Key;
                var contracts = group.OrderBy(x => x.Expiry).ToList();
                if (contracts.Count < 2) continue;

                var nearExpiration = contracts[0].Expiry;
                var farExpiration = contracts[1].Expiry;

                _callCalendarSpread = OptionStrategies.CallCalendarSpread(_optionSymbol, strike, nearExpiration, farExpiration);
                _shortCallCalendarSpread = OptionStrategies.ShortCallCalendarSpread(_optionSymbol, strike, nearExpiration, farExpiration);
                Buy(_callCalendarSpread, 2);
                break;
            }
        }

        protected override void AssertStrategyPositionGroup(IPositionGroup positionGroup)
        {
            if (positionGroup.Positions.Count() != 2)
            {
                throw new Exception($"Expected position group to have 2 positions. Actual: {positionGroup.Positions.Count()}");
            }

            var nearExpiration = _callCalendarSpread.OptionLegs.Min(leg => leg.Expiration);
            var nearExpirationPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Call && x.Symbol.ID.Date == nearExpiration);

            if (nearExpirationPosition.Quantity != -2)
            {
                throw new Exception($"Expected near expiration position quantity to be -2. Actual: {nearExpirationPosition.Quantity}");
            }

            var farExpiration = _callCalendarSpread.OptionLegs.Max(leg => leg.Expiration);
            var farExpirationPosition = positionGroup.Positions
                .Single(x => x.Symbol.ID.OptionRight == OptionRight.Call && x.Symbol.ID.Date == farExpiration);

            if (farExpirationPosition.Quantity != 2)
            {
                throw new Exception($"Expected far expiration position quantity to be 2. Actual: {farExpirationPosition.Quantity}");
            }
        }

        protected override void LiquidateStrategy()
        {
            // We should be able to close the position using the inverse strategy (a short call calendar spread)
            Buy(_shortCallCalendarSpread, 2);
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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999494.8"},
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
            {"Total Fees", "$5.20"},
            {"Estimated Strategy Capacity", "$7000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZEOEHQRYE|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.85%"},
            {"OrderListHash", "fde9204f63cc0d6676155381fc7537ff"}
        };
    }
}
