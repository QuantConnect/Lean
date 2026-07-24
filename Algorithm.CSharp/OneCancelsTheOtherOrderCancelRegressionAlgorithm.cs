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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the cancel path of a one-cancels-the-other (OCO) order group: the group is
    /// placed with both legs far from the market, so neither can fill inside the test window, then one of the
    /// two tickets is explicitly canceled. Asserts that canceling one leg cancels the whole group, not just
    /// the leg that was canceled
    /// </summary>
    public class OneCancelsTheOtherOrderCancelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private List<OrderTicket> _tickets;
        private bool _canceled;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 31);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                MarketOrder(_spy, 100);

                // both legs sit far from the market: limit sell +30% and stop sell -30% should never be
                // reachable in this test window, so only the explicit cancel below can close the group
                _tickets = OneCancelsTheOtherOrder(new List<Order>
                {
                    new LimitOrder(_spy, -100, Math.Round(Securities[_spy].Price * 1.30m, 2), UtcTime),
                    new StopMarketOrder(_spy, -100, Math.Round(Securities[_spy].Price * 0.70m, 2), UtcTime)
                });
            }
            else if (!_canceled && Time.Day > 5)
            {
                // cancel only one leg: the whole OCO group must cancel with it
                _tickets[0].Cancel();
                _canceled = true;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_tickets == null || orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // neither OCO leg's price should ever be reachable in this test window; a fill here means the
            // regression scenario itself is broken, not just the cancellation behavior being tested
            if (_tickets.Any(ticket => ticket.OrderId == orderEvent.OrderId))
            {
                throw new RegressionTestException(
                    $"Unexpected fill for OCO leg {orderEvent.OrderId}: prices were set far from the market so the group should only close through the explicit cancel");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_canceled)
            {
                throw new RegressionTestException("Expected to have canceled one of the OCO legs before the end of the algorithm");
            }

            if (_tickets == null || _tickets.Count != 2)
            {
                throw new RegressionTestException("Expected the OCO group to have exactly 2 legs");
            }

            foreach (var ticket in _tickets)
            {
                if (ticket.Status != OrderStatus.Canceled)
                {
                    throw new RegressionTestException(
                        $"Expected every OCO leg to be Canceled, including the leg that was not explicitly canceled. Leg {ticket.OrderId} has status {ticket.Status}");
                }
            }

            // canceling the OCO exit group must not touch the original market order fill
            if (!Portfolio.Invested)
            {
                throw new RegressionTestException("Expected the algorithm to still be invested: the market order fill is independent from the canceled OCO group");
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
        public long DataPoints => 302;

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
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "29.303%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "102182.68"},
            {"Net Profit", "2.183%"},
            {"Sharpe Ratio", "4.501"},
            {"Sortino Ratio", "5.158"},
            {"Probabilistic Sharpe Ratio", "85.073%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.047"},
            {"Beta", "0.24"},
            {"Annual Standard Deviation", "0.038"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-6.241"},
            {"Tracking Error", "0.117"},
            {"Treynor Ratio", "0.708"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$470000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.76%"},
            {"Drawdown Recovery", "12"},
            {"OrderListHash", "eed60d3f37058f4f436ee819cb6228d7"}
        };
    }
}
