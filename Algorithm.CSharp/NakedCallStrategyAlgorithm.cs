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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrate how to use OptionStrategies helper class to batch send orders for common strategies.
    /// In this case, the algorithm tests the Naked Call strategy.
    /// </summary>
    public class NakedCallStrategyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private OptionStrategy _nakedCall;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(1000000);

            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            option.SetFilter(-2, +2, 0, 180);

            SetBenchmark("GOOG");
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    var contract = chain
                        .OrderBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                        .ThenByDescending(x => x.Expiry)
                        .FirstOrDefault();

                    if (contract != null)
                    {
                        _nakedCall = OptionStrategies.NakedCall(_optionSymbol, contract.Strike, contract.Expiry);
                        Buy(_nakedCall, 2);
                    }
                }
            }
            else
            {
                // Verify that the strategy was traded
                var positionGroup = Portfolio.Positions.Groups.Single();

                var buyingPowerModel = positionGroup.BuyingPowerModel as OptionStrategyPositionGroupBuyingPowerModel;
                if (buyingPowerModel == null)
                {
                    throw new Exception($@"Expected position group buying power model type: {nameof(OptionStrategyPositionGroupBuyingPowerModel)
                        }. Actual: {positionGroup.BuyingPowerModel.GetType()}");
                }

                if (positionGroup.Positions.Count() != 1)
                {
                    throw new Exception($"Expected position group to have 1 position. Actual: {positionGroup.Positions.Count()}");
                }

                var optionPosition = positionGroup.Positions.Single(x => x.Symbol.SecurityType == SecurityType.Option);
                if (optionPosition.Symbol.ID.OptionRight != OptionRight.Call)
                {
                    throw new Exception($"Expected option position to be a call. Actual: {optionPosition.Symbol.ID.OptionRight}");
                }

                var expectedOptionPositionQuantity = -2;

                if (optionPosition.Quantity != expectedOptionPositionQuantity)
                {
                    throw new Exception($@"Expected option position quantity to be {expectedOptionPositionQuantity
                        }. Actual: {optionPosition.Quantity}");
                }

                // Now we can liquidate by selling the strategy
                Sell(_nakedCall, 2);

                // We can quit now, no more testing required
                Quit();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception("Expected no holdings at end of algorithm");
            }

            var ordersCount = Transactions.GetOrders((order) => order.Status == OrderStatus.Filled).Count();
            if (ordersCount != 2)
            {
                throw new Exception("Expected 2 orders to have been submitted and filled, 1 for buying the naked call and 1 for the liquidation." +
                    $" Actual {ordersCount}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4494;

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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$8000.00"},
            {"Lowest Capacity Asset", "GOOCV WBGM92QHIYO6|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "2.19%"},
            {"OrderListHash", "abcbc8c5020452d0765cc29d2d7d3fe3"}
        };
    }
}
