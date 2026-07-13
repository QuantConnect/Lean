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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a margin call requiring a partial reduction of a single-lot option
    /// strategy position group fully liquidates the group instead of failing. Since a single lot cannot be
    /// partially reduced, the margin call order quantity calculation probes a zero-quantity position group,
    /// which used to make the option strategy margin models throw
    /// "InvalidOperationException: Sequence contains no matching element".
    /// </summary>
    public class SingleLotOptionStrategyMarginCallRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly DateTime _expiry = new DateTime(2021, 1, 15);
        private Symbol _shortPut;
        private Symbol _longPut;
        private bool _ordered;
        private bool _cashDropped;
        private int _onMarginCallCount;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 4);
            SetCash(200000);

            var spx = AddIndex("SPX", Resolution.Minute).Symbol;

            _shortPut = AddIndexOptionContract(
                QuantConnect.Symbol.CreateOption(spx, Market.USA, OptionStyle.European, OptionRight.Put, 4200m, _expiry),
                Resolution.Minute).Symbol;
            _longPut = AddIndexOptionContract(
                QuantConnect.Symbol.CreateOption(spx, Market.USA, OptionStyle.European, OptionRight.Put, 3200m, _expiry),
                Resolution.Minute).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!_ordered)
            {
                if (Securities[_shortPut].HasData && Securities[_longPut].HasData)
                {
                    // 1 lot: sell the 4200 put, buy the 3200 put. Strike difference margin: (4200 - 3200) * 100 = $100,000
                    var bullPutSpread = OptionStrategies.BullPutSpread(_shortPut.Canonical, 4200m, 3200m, _expiry);
                    Buy(bullPutSpread, 1);
                    _ordered = true;
                }
                return;
            }

            if (!_cashDropped && Portfolio.Invested)
            {
                // Simulate a drawdown: equity drops below the margin used by the spread so that the
                // margin call model requests a partial reduction of the 1-lot position group
                Portfolio.CashBook[Currencies.USD].SetAmount(100000);
                _cashDropped = true;
            }
        }

        public override void OnMarginCall(List<SubmitOrderRequest> requests)
        {
            _onMarginCallCount++;

            if (requests.Count != 2)
            {
                throw new RegressionTestException($"Expected 2 margin call order requests, one per leg, but found {requests.Count}");
            }

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

            if (Portfolio.Invested)
            {
                throw new RegressionTestException("The margin call should have liquidated the whole position group, " +
                    $"but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Count != 4)
            {
                throw new RegressionTestException($"Expected 4 orders, the strategy entry and margin call liquidation legs, but found {orders.Count}");
            }

            if (orders.Any(order => !order.Status.IsFill()))
            {
                throw new RegressionTestException("All orders should be filled");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2745;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "55475"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$34000000.00"},
            {"Lowest Capacity Asset", "SPX 31KC0UJFOS3N2|SPX 31"},
            {"Portfolio Turnover", "159.43%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "e69ca5560c0621ec31adcc1bc1f7a932"}
        };
    }
}
