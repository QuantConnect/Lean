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
    /// In this case, the algorithm tests the Covered and Protective Put strategies.
    /// </summary>
    public class CoveredAndProtectivePutStrategiesAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private OptionStrategy _coveredPut;
        private OptionStrategy _protectivePut;

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
                        _coveredPut = OptionStrategies.CoveredPut(_optionSymbol, contract.Strike, contract.Expiry);
                        _protectivePut = OptionStrategies.ProtectivePut(_optionSymbol, contract.Strike, contract.Expiry);
                        Buy(_coveredPut, 2);
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

                if (positionGroup.Positions.Count() != 2)
                {
                    throw new Exception($"Expected position group to have 2 positions. Actual: {positionGroup.Positions.Count()}");
                }

                var optionPosition = positionGroup.Positions.Single(x => x.Symbol.SecurityType == SecurityType.Option);
                if (optionPosition.Symbol.ID.OptionRight != OptionRight.Put)
                {
                    throw new Exception($"Expected option position to be a put. Actual: {optionPosition.Symbol.ID.OptionRight}");
                }

                var underlyingPosition = positionGroup.Positions.Single(x => x.Symbol.SecurityType == SecurityType.Equity);
                var expectedOptionPositionQuantity = -2;
                var expectedUnderlyingPositionQuantity = -2 * Securities[_optionSymbol].SymbolProperties.ContractMultiplier;

                if (optionPosition.Quantity != expectedOptionPositionQuantity)
                {
                    throw new Exception($@"Expected option position quantity to be {expectedOptionPositionQuantity
                        }. Actual: {optionPosition.Quantity}");
                }

                if (underlyingPosition.Quantity != expectedUnderlyingPositionQuantity)
                {
                    throw new Exception($@"Expected underlying position quantity to be {expectedUnderlyingPositionQuantity
                        }. Actual: {underlyingPosition.Quantity}");
                }

                // Now we should be able to close the position using the inverse strategy (a protective put)
                Buy(_protectivePut, 2);

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
            if (ordersCount != 4)
            {
                throw new Exception("Expected 4 orders to have been submitted and filled, 2 for buying the covered put and 2 for the liquidation." +
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
            {"Total Trades", "4"},
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
            {"Estimated Strategy Capacity", "$160000.00"},
            {"Lowest Capacity Asset", "GOOCV 30AKMEIPOSS1Y|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "32.12%"},
            {"OrderListHash", "e9463ceca671e9ddd44ea5b1fa930ba8"}
        };
    }
}
