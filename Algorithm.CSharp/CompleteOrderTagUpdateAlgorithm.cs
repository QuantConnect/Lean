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
    /// Algorithm asserting that closed orders can be updated with a new tag
    /// </summary>
    public class CompleteOrderTagUpdateAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static string TagAfterFill = "This is the tag set after order was filled.";
        private static string TagAfterCanceled = "This is the tag set after order was canceled.";

        private OrderTicket _marketOrderTicket;
        private OrderTicket _limitOrderTicket;

        private int _quantity = 100;

        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                // a limit order to test the tag update after order was canceled
                if (_limitOrderTicket == null)
                {
                    // low price, we don't want it to fill since we are canceling it
                    _limitOrderTicket = LimitOrder(_spy, 100, Securities[_spy].Price * 0.1m);
                    _limitOrderTicket.Cancel();
                }
                // a market order to test the tag update after order was filled
                else
                {
                    Buy(_spy, _quantity);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Canceled)
            {
                if (orderEvent.OrderId != _limitOrderTicket.OrderId)
                {
                    throw new Exception("The only canceled order should have been the limit order.");
                }

                // update canceled order tag
                UpdateOrderTag(_limitOrderTicket, TagAfterCanceled, "Error updating order tag after canceled");
            }
            else if (orderEvent.Status == OrderStatus.Filled)
            {
                _marketOrderTicket = Transactions.GetOrderTickets(x => x.OrderType == OrderType.Market).Single();
                if (orderEvent.OrderId != _marketOrderTicket.OrderId)
                {
                    throw new Exception("The only filled order should have been the market order.");
                }

                // try to update a field other than the tag
                var updateFields = new UpdateOrderFields();
                updateFields.Quantity = 50;
                var response = _marketOrderTicket.Update(updateFields);
                if (response.IsSuccess)
                {
                    throw new Exception("The market order quantity should not have been updated.");
                }

                // update filled order tag
                UpdateOrderTag(_marketOrderTicket, TagAfterFill, "Error updating order tag after fill");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // check the filled order
            AssertOrderTagUpdate(_marketOrderTicket, TagAfterFill, "filled");
            if (_marketOrderTicket.Quantity != _quantity || _marketOrderTicket.QuantityFilled != _quantity)
            {
                throw new Exception("The market order quantity should not have been updated.");
            }

            // check the canceled order
            AssertOrderTagUpdate(_limitOrderTicket, TagAfterCanceled, "canceled");
        }

        private void AssertOrderTagUpdate(OrderTicket ticket, string expectedTag, string orderAction)
        {
            if (ticket == null)
            {
                throw new Exception($"The order ticket was not set for the {orderAction} order");
            }

            if (ticket.Tag != expectedTag)
            {
                throw new Exception($"Order ticket tag was not updated after order was {orderAction}");
            }

            var order = Transactions.GetOrderById(ticket.OrderId);
            if (order.Tag != expectedTag)
            {
                throw new Exception($"Order tag was not updated after order was {orderAction}");
            }
        }

        private static void UpdateOrderTag(OrderTicket ticket, string tag, string errorMessagePrefix)
        {
            var updateFields = new UpdateOrderFields();
            updateFields.Tag = tag;
            var response = ticket.Update(updateFields);

            if (response.IsError)
            {
                throw new Exception($"{errorMessagePrefix}: {response.ErrorMessage}");
            }
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
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "21.706%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100251.47"},
            {"Net Profit", "0.251%"},
            {"Sharpe Ratio", "5.078"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.483%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.122"},
            {"Beta", "0.144"},
            {"Annual Standard Deviation", "0.032"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-9.515"},
            {"Tracking Error", "0.191"},
            {"Treynor Ratio", "1.13"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$210000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.89%"},
            {"OrderListHash", "8fba4f724843997ef421cf26ccabe51b"}
        };
    }
}
