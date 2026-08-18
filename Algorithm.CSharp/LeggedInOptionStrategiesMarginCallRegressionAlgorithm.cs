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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm legging into multiple single-lot option strategy position groups with sequential
    /// market orders and then going through a margin call that requires a partial reduction of the groups.
    /// The margin call order quantity calculation probes degenerate (zero-quantity) trial groups, which used to
    /// crash the algorithm with "Sequence contains no matching element" in OptionStrategyPositionGroupBuyingPowerModel.
    /// </summary>
    public class LeggedInOptionStrategiesMarginCallRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private bool _legged;
        private bool _cashDropped;
        private int _onMarginCallCount;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));
        }

        public override void OnData(Slice slice)
        {
            if (!_legged)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var contractsByExpiry = chain.GroupBy(x => x.Expiry).OrderBy(x => x.Key).ToList();

                    // A put spread at the nearest expiry: long the lowest strike put, short the next one
                    var puts = contractsByExpiry[0].Where(contract => contract.Right == OptionRight.Put)
                        .OrderBy(contract => contract.Strike)
                        .ToList();
                    var longPut = puts[0];
                    var shortPut = puts.First(contract => contract.Strike > longPut.Strike);

                    // And a call spread at another expiry so two separate strategy groups are resolved
                    var calls = contractsByExpiry
                        .Skip(1)
                        .Select(x => x.Where(contract => contract.Right == OptionRight.Call).OrderBy(contract => contract.Strike).ToList())
                        .First(x => x.Count > 1);
                    var shortCall = calls[0];
                    var longCall = calls.First(contract => contract.Strike > shortCall.Strike);

                    // Leg into the strategies with individual market orders instead of combo orders
                    MarketOrder(shortCall.Symbol, -1);
                    MarketOrder(longCall.Symbol, +1);
                    MarketOrder(shortPut.Symbol, -1);
                    MarketOrder(longPut.Symbol, +1);
                    _legged = true;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BearCallSpread.Name);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BullPutSpread.Name);
                }
                return;
            }

            if (!_cashDropped && Portfolio.Invested)
            {
                // Simulate a drawdown: equity drops below the margin used by the strategy groups so that the
                // margin call model requests a partial reduction of the single-lot position groups
                var cash = Portfolio.CashBook[Currencies.USD].Amount;
                Portfolio.CashBook[Currencies.USD].SetAmount(cash - Portfolio.TotalPortfolioValue + 0.6m * Portfolio.TotalMarginUsed);
                _cashDropped = true;
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            _onMarginCallCount++;

            foreach (var request in requests)
            {
                var holdingsQuantity = Securities[request.Symbol].Holdings.Quantity;
                if (request.Quantity != -holdingsQuantity)
                {
                    throw new RegressionTestException($@"Expected margin call order for {request.Symbol} to fully liquidate the {holdingsQuantity
                        } holdings, but its quantity was {request.Quantity}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_onMarginCallCount != 1)
            {
                throw new RegressionTestException($"OnMarginCall was called {_onMarginCallCount} times, expected 1");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Count <= 4)
            {
                throw new RegressionTestException(
                    $"Expected margin call orders in addition to the 4 strategy leg entries, but found {orders.Count} orders in total");
            }

            if (orders.Any(order => !order.Status.IsFill()))
            {
                throw new RegressionTestException("All orders should be filled");
            }
        }

        private void AssertOptionStrategyIsPresent(string name)
        {
            if (Portfolio.Positions.Groups.Count(group =>
                group.BuyingPowerModel is Securities.Option.OptionStrategyPositionGroupBuyingPowerModel model && model.ToString() == name) != 1)
            {
                throw new RegressionTestException($"Option strategy: '{name}' was not found!");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 15023;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "313"},
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
            {"Total Fees", "$6.00"},
            {"Estimated Strategy Capacity", "$250000.00"},
            {"Lowest Capacity Asset", "GOOCV W87G1Y7EJGW6|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "5146.96%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "60f3e2ec37bcb6c5ccdcce8fbb14fe22"}
        };
    }
}
