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
    /// Regression algorithm for one-cancels-the-other (OCO) order groups. It shows that both leg types can win.
    ///
    /// Buy 100 SPY, then place two groups one after the other:
    /// - sell 200: the limit leg wins, so we go from long 100 to short 100
    /// - buy 100: the stop leg wins, so we end flat
    ///
    /// Holdings go 0 -> 100 -> -100 -> 0. In each group the losing leg must be canceled in the same event batch
    /// as the winning fill. The second group matters because stop legs are checked before limit legs, so a
    /// winning stop leg takes a different path than a winning limit leg
    /// </summary>
    public class OneCancelsTheOtherOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        // counts every order event we get, so we can tell if two events arrived one after the other
        private int _orderEventCount;

        private bool _positionOpened;
        private OrderGroupTracker _reversalGroup;
        private OrderGroupTracker _coverGroup;

        /// <summary>
        /// What each order group is for
        /// </summary>
        private enum GroupRole
        {
            /// <summary>Sells 200, so the winning leg turns long 100 into short 100</summary>
            Reversal,

            /// <summary>Buys 100 back, so the winning leg leaves us flat</summary>
            Cover
        }

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 20);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_spy))
            {
                return;
            }

            // open the position on its own bar, so the groups below start from a position that is already there
            if (!_positionOpened)
            {
                MarketOrder(_spy, 100);
                _positionOpened = true;
                return;
            }

            // no rounding here: Lean rounds order prices to the brokerage's precision before it sends them
            var price = Securities[_spy].Price;

            if (_reversalGroup == null)
            {
                // sell 200. The January rally reaches the limit +1%, the stop -30% never fills, so the limit wins
                _reversalGroup = new OrderGroupTracker(OneCancelsTheOtherOrder(_spy, -200,
                    limitPrice: price * 1.01m,
                    stopPrice: price * 0.70m));
                return;
            }

            if (_coverGroup == null && _reversalGroup.HasWinner)
            {
                // now short 100, so buy it back with the prices swapped: the rally reaches the stop +1% and the
                // limit -30% never fills, so this time the stop wins. We wait for the first group to have a
                // winner instead of checking Portfolio.Invested, which is also false while an order is working
                _coverGroup = new OrderGroupTracker(OneCancelsTheOtherOrder(_spy, 100,
                    limitPrice: price * 0.70m,
                    stopPrice: price * 1.01m));
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            _orderEventCount++;

            // events that belong to no group are skipped, for example the opening market order
            FindGroup(orderEvent.OrderId)?.Track(orderEvent, _orderEventCount);
        }

        private OrderGroupTracker FindGroup(int orderId)
        {
            if (_reversalGroup != null && _reversalGroup.Contains(orderId))
            {
                return _reversalGroup;
            }

            return _coverGroup != null && _coverGroup.Contains(orderId) ? _coverGroup : null;
        }

        public override void OnEndOfAlgorithm()
        {
            AssertGroupResolved(_reversalGroup, GroupRole.Reversal, winningOrderType: OrderType.Limit);
            AssertGroupResolved(_coverGroup, GroupRole.Cover, winningOrderType: OrderType.StopMarket);

            // bought 100, sold 200, bought 100 back, so we end with nothing
            var holdings = Portfolio[_spy].Quantity;
            if (holdings != 0m)
            {
                throw new RegressionTestException(
                    $"Expected to end flat after the cover group's stop leg bought the short back, but SPY holdings are {holdings}.");
            }
        }

        /// <summary>
        /// Checks one group: the leg of the given type filled, the other leg was canceled, and the cancel came
        /// in the same event batch as the fill
        /// </summary>
        /// <param name="group">The group to check</param>
        /// <param name="role">What this group was for, used in the error messages</param>
        /// <param name="winningOrderType">The type of the leg that should have filled</param>
        private static void AssertGroupResolved(OrderGroupTracker group, GroupRole role, OrderType winningOrderType)
        {
            if (group == null || group.Tickets.Count != 2)
            {
                throw new RegressionTestException(
                    $"Expected the {role} one-cancels-the-other group to have been placed with 2 legs.");
            }

            var winner = group.Tickets.Single(ticket => ticket.OrderType == winningOrderType);
            if (winner.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException(
                    $"Expected the {role} group's {winner.OrderType} leg to be filled, but it was {winner.Status}.");
            }

            var loser = group.Tickets.Single(ticket => ticket.OrderType != winningOrderType);
            if (loser.Status != OrderStatus.Canceled)
            {
                throw new RegressionTestException(
                    $"Expected the {role} group's {loser.OrderType} leg to be canceled by the group, but it was {loser.Status}.");
            }

            if (!group.SiblingCanceledInSameBatch)
            {
                throw new RegressionTestException(
                    $"Expected the {role} group's losing leg Canceled event to have arrived in the same order-event batch as the winning fill.");
            }
        }

        /// <summary>
        /// Watches one group: only one leg may fill, and the other leg must be canceled in the same event batch
        /// </summary>
        private sealed class OrderGroupTracker
        {
            private int? _winnerOrderId;
            private DateTime _winnerFillUtcTime;
            private int _winnerFillEventCount;

            public OrderGroupTracker(List<OrderTicket> tickets)
            {
                Tickets = tickets;
            }

            public List<OrderTicket> Tickets { get; }

            public bool HasWinner => _winnerOrderId.HasValue;

            public bool SiblingCanceledInSameBatch { get; private set; }

            public bool Contains(int orderId) => Tickets.Any(ticket => ticket.OrderId == orderId);

            public void Track(OrderEvent orderEvent, int orderEventCount)
            {
                if (orderEvent.Status == OrderStatus.Filled)
                {
                    if (_winnerOrderId.HasValue)
                    {
                        throw new RegressionTestException(
                            $"Order {orderEvent.OrderId} filled after order {_winnerOrderId.Value} had already won the group. " +
                            "Only one leg of a one-cancels-the-other group should ever fill.");
                    }

                    _winnerOrderId = orderEvent.OrderId;
                    _winnerFillUtcTime = orderEvent.UtcTime;
                    _winnerFillEventCount = orderEventCount;
                }
                else if (orderEvent.Status == OrderStatus.Canceled)
                {
                    if (!_winnerOrderId.HasValue)
                    {
                        throw new RegressionTestException(
                            $"Order {orderEvent.OrderId} was canceled before any leg of the group had filled.");
                    }

                    // same batch means same timestamp, and the very next event we get after the fill
                    if (orderEvent.UtcTime != _winnerFillUtcTime || orderEventCount != _winnerFillEventCount + 1)
                    {
                        throw new RegressionTestException(
                            "Expected the losing leg's Canceled event to arrive in the same order-event batch as the winning Filled event.");
                    }

                    SiblingCanceledInSameBatch = true;
                }
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
        public long DataPoints => 190;

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
            {"Total Orders", "5"},
            {"Average Win", "0.40%"},
            {"Average Loss", "-0.23%"},
            {"Compounding Annual Return", "3.683%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0.389"},
            {"Start Equity", "100000"},
            {"End Equity", "100175.21"},
            {"Net Profit", "0.175%"},
            {"Sharpe Ratio", "-0.089"},
            {"Sortino Ratio", "-0.063"},
            {"Probabilistic Sharpe Ratio", "39.090%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.78"},
            {"Alpha", "-0.254"},
            {"Beta", "0.158"},
            {"Annual Standard Deviation", "0.035"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-10.492"},
            {"Tracking Error", "0.152"},
            {"Treynor Ratio", "-0.02"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$170000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.34%"},
            {"Drawdown Recovery", "1"},
            {"OrderListHash", "ee50ff86401969f4159066ba8fceee62"}
        };
    }
}
