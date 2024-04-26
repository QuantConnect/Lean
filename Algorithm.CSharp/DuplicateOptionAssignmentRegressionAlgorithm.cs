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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that open orders are canceled when the option is assigned and delisted,
    /// also making sure the assignment happens and its processed regardless of the existing of an open order for said option.
    /// </summary>
    public class DuplicateOptionAssignmentRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _stock;
        private Symbol _option;

        private bool _optionSold;
        private bool _optionAssigned;
        private bool _optionDelisted;
        private bool _optionDelistedWarningReceived;
        private bool _orderCanceled;
        private bool _stockAssigned;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 17);
            SetEndDate(2015, 12, 28);
            SetCash(100000);

            _stock = AddEquity("GOOG").Symbol;

            _option = QuantConnect.Symbol.CreateOption(_stock, Market.USA, OptionStyle.American, OptionRight.Put, 800m, new DateTime(2015, 12, 24));

            AddOptionContract(_option);
        }

        public override void OnData(Slice data)
        {
            // We are done
            if (_optionSold)
            {
                return;
            }

            if (!Portfolio.Invested)
            {
                Sell(_option, 1);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                // This is the fill for the option sell order
                if (!_optionSold)
                {
                    // Let's close the position but with a limit order that won't ever fill (limit price too low)
                    // just so we keep it open until the brokerage tries to assign it
                    LimitOrder(_option, 1, Securities[_option].Price * 0.1m);

                    _optionSold = true;
                }
                // This is the assignment
                else if (!_optionAssigned)
                {
                    if (orderEvent.Ticket.OrderType != OrderType.OptionExercise || !orderEvent.IsAssignment)
                    {
                        throw new Exception($"Expected option assignment but got: {orderEvent}");
                    }

                    _optionAssigned = true;
                }
                else if (!_stockAssigned)
                {
                    if (orderEvent.Ticket.OrderType != OrderType.OptionExercise || orderEvent.IsAssignment || orderEvent.Symbol != _stock)
                    {
                        throw new Exception($"Expected stock assignment but got: {orderEvent}");
                    }

                    _stockAssigned = true;
                }
                else
                {
                    throw new Exception($"Unexpected order fill event: {orderEvent}");
                }
            }
            else if (orderEvent.Status == OrderStatus.CancelPending)
            {
                // We receive the delisting warning before the order cancel is requested
                if (!_optionSold || !_optionAssigned || !_stockAssigned || !_optionDelistedWarningReceived)
                {
                    throw new Exception($"Unexpected cancel pending event: {orderEvent}");
                }
            }
            else if (orderEvent.Status == OrderStatus.Canceled)
            {
                // The delisted event is received before the order is canceled
                if (!_optionSold || !_optionAssigned || !_stockAssigned || !_optionDelistedWarningReceived || !_optionDelisted)
                {
                    throw new Exception($"Unexpected cancel event: {orderEvent}");
                }

                _orderCanceled = true;
            }
        }

        public override void OnDelistings(Delistings delistings)
        {
            if (!delistings.TryGetValue(_option, out var delisting))
            {
                throw new Exception($"Unexpected delisting events");
            }

            if (delisting.Type == DelistingType.Warning)
            {
                if (!_optionSold || !_optionAssigned || !_stockAssigned || _optionDelistedWarningReceived)
                {
                    throw new Exception($"Unexpected delisting warning event: {delisting}");
                }

                _optionDelistedWarningReceived = true;
            }
            else
            {
                if (!_optionSold || !_optionAssigned || !_stockAssigned || !_optionDelistedWarningReceived || _optionDelisted)
                {
                    throw new Exception($"Unexpected delisting event: {delisting}");
                }

                _optionDelisted = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_optionSold)
            {
                throw new Exception("Option was not sold");
            }

            if (!_optionAssigned)
            {
                throw new Exception("Option was not assigned");
            }

            if (!_stockAssigned)
            {
                throw new Exception("Stock was not assigned");
            }

            if (!_optionDelistedWarningReceived)
            {
                throw new Exception("Option delisting warning was not received");
            }

            if (!_optionDelisted)
            {
                throw new Exception("Option was not delisted");
            }

            if (!_orderCanceled)
            {
                throw new Exception("Order was not canceled");
            }

            var openOrders = Transactions.GetOpenOrders();
            if (openOrders.Count != 0)
            {
                throw new Exception("There should be no open orders");
            }

            if (!Portfolio.Invested)
            {
                throw new Exception("Portfolio should be invested");
            }

            // We should have the stock since the option was assigned
            if (Portfolio.Positions.Groups.Single().Single().Symbol != _stock)
            {
                throw new Exception("Portfolio should have the stock");
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
        public long DataPoints => 2849;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "4.48%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-19.248%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99319"},
            {"Net Profit", "-0.681%"},
            {"Sharpe Ratio", "-6.361"},
            {"Sortino Ratio", "-4.623"},
            {"Probabilistic Sharpe Ratio", "0.018%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.139"},
            {"Beta", "-0.082"},
            {"Annual Standard Deviation", "0.024"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-2.525"},
            {"Tracking Error", "0.137"},
            {"Treynor Ratio", "1.883"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$1300000.00"},
            {"Lowest Capacity Asset", "GOOCV 305RBQ20WHPNQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "7.07%"},
            {"OrderListHash", "f0ce9bade48d8eb13a1dbd77aeeb485c"}
        };
    }
}
