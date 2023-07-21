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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This base algorithm demonstrates how to use OptionStrategies helper class to batch send orders for common strategies.
    /// </summary>
    public abstract class OptionStrategyFactoryMethodsBaseAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Symbol _optionSymbol;

        protected abstract int ExpectedOrdersCount { get; }

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
                    TradeStrategy(chain);
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

                AssertStrategyPositionGroup(positionGroup);

                // Now we should be able to close the position
                LiquidateStrategy();

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
            if (ordersCount != ExpectedOrdersCount)
            {
                throw new Exception($@"Expected {ExpectedOrdersCount
                    } orders to have been submitted and filled, half for buying the strategy and the other half for the liquidation. Actual {
                    ordersCount}");
            }
        }

        protected abstract void TradeStrategy(OptionChain chain);

        protected abstract void AssertStrategyPositionGroup(IPositionGroup positionGroup);

        protected abstract void LiquidateStrategy();

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public abstract bool CanRunLocally { get; }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public abstract Language[] Languages { get; }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public abstract long DataPoints { get; }

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public abstract int AlgorithmHistoryDataPoints { get; }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public abstract Dictionary<string, string> ExpectedStatistics { get; }
    }
}
