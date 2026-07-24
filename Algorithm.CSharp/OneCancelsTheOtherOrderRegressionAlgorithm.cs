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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for the winner path of a one-cancels-the-other (OCO) order group: buys SPY at
    /// market, then places a 2-leg OCO group (a take-profit limit leg 1% above the entry price and a stop-market
    /// leg 30% below it, which the January 2019 rally can never reach). The limit leg should fill and the group
    /// should cancel the stop leg in the same order-event batch
    /// </summary>
    public class OneCancelsTheOtherOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private List<OrderTicket> _tickets;

        // tracks every order event this algorithm receives, relevant or not, so we can tell whether two
        // particular events were delivered back to back (same batch) or with something else in between
        private int _orderEventCount;

        private int? _winnerOrderId;
        private DateTime _winnerFillUtcTime;
        private int _winnerFillEventCount;
        private bool _siblingCanceledInSameBatch;

        public override void Initialize()
        {
            SetStartDate(2019, 1, 1);
            SetEndDate(2019, 1, 20);

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
        }

        public override void OnData(Slice slice)
        {
            // trade exactly once: once the winning leg closes the position, Portfolio.Invested goes back to
            // false and this would otherwise place a second, independent OCO group on top of the first
            if (_tickets == null && !Portfolio.Invested)
            {
                MarketOrder(_spy, 100);

                // take profit +1% is reached by the January rally; the stop -30% can never fill
                _tickets = OneCancelsTheOtherOrder(new List<Order>
                {
                    new LimitOrder(_spy, -100, Math.Round(Securities[_spy].Price * 1.01m, 2), UtcTime),
                    new StopMarketOrder(_spy, -100, Math.Round(Securities[_spy].Price * 0.70m, 2), UtcTime)
                });
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            _orderEventCount++;

            if (_tickets == null || (orderEvent.OrderId != _tickets[0].OrderId && orderEvent.OrderId != _tickets[1].OrderId))
            {
                // not one of our OCO legs (for example the entry market order)
                return;
            }

            if (orderEvent.Status == OrderStatus.Filled)
            {
                if (_winnerOrderId.HasValue)
                {
                    throw new RegressionTestException(
                        $"Order {orderEvent.OrderId} filled after order {_winnerOrderId} had already won the OCO group. Only one leg should ever fill.");
                }

                _winnerOrderId = orderEvent.OrderId;
                _winnerFillUtcTime = orderEvent.UtcTime;
                _winnerFillEventCount = _orderEventCount;
            }
            else if (orderEvent.Status == OrderStatus.Canceled)
            {
                if (!_winnerOrderId.HasValue)
                {
                    throw new RegressionTestException($"Order {orderEvent.OrderId} was canceled before any leg of the group had filled.");
                }

                // the sibling cancel must land in the same order-event batch as the winning fill: same
                // timestamp, and delivered as the very next order event this algorithm receives after the fill
                if (orderEvent.UtcTime != _winnerFillUtcTime || _orderEventCount != _winnerFillEventCount + 1)
                {
                    throw new RegressionTestException(
                        "Expected the losing leg's Canceled event to arrive in the same order-event batch as the winning Filled event.");
                }

                _siblingCanceledInSameBatch = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tickets == null || _tickets.Count != 2)
            {
                throw new RegressionTestException("Expected the one-cancels-the-other order group to have been placed with 2 legs.");
            }

            // limit leg won, stop leg was canceled by the group
            if (_tickets[0].Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Expected the take-profit limit order to be filled, but it was {_tickets[0].Status}.");
            }

            if (_tickets[1].Status != OrderStatus.Canceled)
            {
                throw new RegressionTestException($"Expected the stop-loss order to be canceled by the group, but it was {_tickets[1].Status}.");
            }

            if (Portfolio.Invested)
            {
                throw new RegressionTestException("Expected no open position at the end of the algorithm: the winning limit leg should have closed it.");
            }

            if (!_siblingCanceledInSameBatch)
            {
                throw new RegressionTestException("Expected the stop-loss leg's Canceled event to have arrived in the same order-event batch as the winning fill.");
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
            {"Total Orders", "3"},
            {"Average Win", "0.24%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "4.986%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100235.79"},
            {"Net Profit", "0.236%"},
            {"Sharpe Ratio", "0.613"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "45.094%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.011"},
            {"Beta", "-0.003"},
            {"Annual Standard Deviation", "0.009"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.833"},
            {"Tracking Error", "0.18"},
            {"Treynor Ratio", "-1.72"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$140000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.64%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d0a9c717f35803df04a8ab7f31697c5c"}
        };
    }
}
