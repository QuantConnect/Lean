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
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the margin call events are fired when trading options strategies
    /// </summary>
    public class OptionStrategyMarginCallEventsAlgorithm : OptionsMarginCallEventsAlgorithmBase
    {
        private Symbol _optionSymbol;
        private OptionStrategy _optionStrategy;

        protected override int OriginalQuantity => -50;
        protected override int ExpectedOrdersCount => 4;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 30);
            // -50 of a straddle will use almost all of 900k, so will eventually trigger margin call
            SetCash(900000);

            var equity = AddEquity("GOOG");
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.Strikes(-2, +2)
                .Expiration(0, 180));

            Portfolio.MarginCallModel = new CustomMarginCallModel(Portfolio, DefaultOrderProperties);
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
                        .OrderByDescending(x => x.Strike)
                        .ToList();

                    var expiry = callContracts[0].Expiry;
                    var strike = callContracts[0].Strike;

                    _optionStrategy = OptionStrategies.Straddle(_optionSymbol, strike, expiry);
                    Order(_optionStrategy, OriginalQuantity);
                }
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            base.OnMarginCall(requests);

            var positionGroup = Portfolio.Positions.Groups.Single();
            foreach (var request in requests)
            {
                var position = positionGroup.GetPosition(request.Symbol);
                // We expect the margin call to be for one unit of the strategy in the opposite direction
                var expectedQuantity = -Math.Sign(position.Quantity) * 1;
                if (request.Quantity != expectedQuantity)
                {
                    throw new Exception($"Expected margin call order quantity to be {expectedQuantity} but was {request.Quantity}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 3132879;

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
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-4.893%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.092%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.681"},
            {"Tracking Error", "0.092"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1252.00"},
            {"Estimated Strategy Capacity", "$130000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMML01JA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.17%"},
            {"OrderListHash", "681be68373c2f38e51456d7f8010e7d3"}
        };
    }
}
