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
    /// Regression algorithm asserting that naked short option strategies with margin requirements that cannot be met result in invalid orders.
    /// Also, for valid naked short positions, the algorithm asserts that part of the position can be liquidated.
    /// </summary>
    public class NakedShortOptionStrategyOverMarginAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int _quantityOverMargin = 50;
        private const int _quantity = 5;
        private const int _quantityToLiquidate = 2;

        private Symbol _optionSymbol;

        private OptionStrategy _optionStrategy;

        private bool _done;

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
            if (_done)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
                {
                    var atmStraddle = chain
                        .OrderBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                        .ThenByDescending(x => x.Expiry)
                        .FirstOrDefault();

                    if (atmStraddle != null)
                    {
                        _optionStrategy = OptionStrategies.Straddle(_optionSymbol, atmStraddle.Strike, atmStraddle.Expiry);

                        // This is invalid, margin is not enough
                        Sell(_optionStrategy, _quantityOverMargin);

                        // Margin is enough for this one
                        Sell(_optionStrategy, _quantity);
                    }
                }
            }
            else
            {
                Buy(_optionStrategy, _quantityToLiquidate);
                _done = true;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());

            if (orderEvent.Quantity == _quantityOverMargin && orderEvent.Status != OrderStatus.Invalid)
            {
                throw new Exception($"Orders with quantity {_quantityOverMargin} should be invalid");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Make sure only 4 orders where placed, 2 for the strategy and 2 for the liquidation.
            // The first combo order should have been invalid.
            var filledOrdersCount = Transactions.GetOrders(o => o.Status.IsFill()).Count();
            var expectedFilledOrdersCount = 2 * _optionStrategy.OptionLegs.Count;
            if (filledOrdersCount != expectedFilledOrdersCount)
            {
                throw new Exception($"Expected {expectedFilledOrdersCount} filled orders, found {filledOrdersCount}");
            }

            var expectedQuantity = Math.Abs(_quantity - _quantityToLiquidate);
            var positionGroup = Portfolio.Positions.Groups.Single();
            if (positionGroup.Quantity != expectedQuantity)
            {
                throw new Exception($"Expected position quantity to be {expectedQuantity} but was {positionGroup.Quantity}");
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
        public long DataPoints => 471124;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Start Equity", "1000000"},
            {"End Equity", "998775.9"},
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
            {"Total Fees", "$9.10"},
            {"Estimated Strategy Capacity", "$1800000.00"},
            {"Lowest Capacity Asset", "GOOCV 30AKMEIPOSS1Y|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "7.50%"},
            {"OrderListHash", "70487a4231ef2237ca24642be28652c4"}
        };
    }
}
