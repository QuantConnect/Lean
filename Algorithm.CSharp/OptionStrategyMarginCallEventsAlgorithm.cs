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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that the <see cref="QCAlgorithm.OnMarginCallWarning"/> event is fired when trading options strategies
    /// </summary>
    public class OptionStrategyMarginCallWarningAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly int _strategyQuantity = -50;
        private Symbol _optionSymbol;
        private OptionStrategy _optionStrategy;

        private int _onMarginCallWarningCount;
        private int _onMarginCallCount;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 30);
            SetCash(1640000);

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
                    Order(_optionStrategy, _strategyQuantity);
                }
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            Debug($"OnMarginCall at {Time}");
            _onMarginCallCount++;
        }

        public override void OnMarginCallWarning()
        {
            Debug($"OnMarginCallWarning at {Time}");
            _onMarginCallWarningCount++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new Exception("Portfolio should be invested");
            }

            if (_onMarginCallCount != 1)
            {
                throw new Exception($"OnMarginCall was called {_onMarginCallCount} times, expected 1");
            }

            if (_onMarginCallWarningCount == 0)
            {
                throw new Exception("OnMarginCallWarning was not called");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Count != 4)
            {
                throw new Exception($"Expected 4 orders, found {orders.Count}");
            }

            if (orders.Any(order => !order.Status.IsFill()))
            {
                throw new Exception("All orders should be filled");
            }

            var finalStrategyQuantity = Portfolio.PositionGroups.First().Quantity;
            if (finalStrategyQuantity <= _strategyQuantity)
            {
                throw new Exception($@"Strategy position group quantity should have been decreased from the original quantity {_strategyQuantity
                    }, but was {finalStrategyQuantity}");
            }
        }

        private class CustomMarginCallModel : DefaultMarginCallModel
        {
            // Setting margin buffer to 0 so we make sure the margin call orders are generated. Otherwise, they will only
            // be generated if the used margin is > 110%TVP, which is unlikely for this case
            public CustomMarginCallModel(SecurityPortfolioManager portfolio, IOrderProperties defaultOrderProperties)
                : base(portfolio, defaultOrderProperties, 0m)
            {
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
        public long DataPoints => 3132879;

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
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-5.684%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.107%"},
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
            {"Portfolio Turnover", "1.20%"},
            {"OrderListHash", "583b89f10ce6ca6fa842b21a35fbf0f2"}
        };
    }
}
