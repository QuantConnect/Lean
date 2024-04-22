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
    /// This regression algorithm tests that orders are unchangeable from the QCAlgorithm Layer
    /// Orders should only be modifiable via their ticket and only in permitted ways
    /// </summary>
    /// <meta name="tag" content="backtesting brokerage" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    public class OrderImmutabilityRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private OrderTicket _ticket;
        private Order _originalOrder;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 09);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            AddEquity("SPY", Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                _ticket = LimitOrder(_spy, 10, 100);
                Debug("Purchased Stock");

                // Here we will show how to correctly change an order, we will then verify at End of Algorithm!
                // First get the order as it is now, should be a copy, so it wont be updated!
                _originalOrder = Transactions.GetOrderById(_ticket.OrderId);

                // Create an UpdateOrderRequest and send it to the ticket
                var updateFields = new UpdateOrderFields { Quantity = 20, Tag = "Pepe", LimitPrice = data[_spy].Low};
                var response = _ticket.Update(updateFields);

                // Test order time
                if (_originalOrder.Time != UtcTime)
                {
                    Error("Order Time should be UtcTime!");
                    throw new Exception("Order Time should be UtcTime!");
                }
            }
        }

        /// <summary>
        /// All order events get pushed through this function
        /// This function will test that what we get from Transactions is indeed a clone
        /// The only authentic way to change the order is to change through the order ticket!
        /// </summary>
        /// <param name="orderEvent">OrderEvent object that contains all the information about the event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {

            // Get the order twice, since they are clones they should NOT be the same
            var orderV1 = Transactions.GetOrderById(orderEvent.OrderId);
            var orderV2 = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderV1 == orderV2)
            {
                Error("Orders should be clones, hence not equal!");
                throw new Exception("Orders should be clones, hence not equal!");
            }

            // Try and manipulate orderV2 using the only external accessor BrokerID, since we
            // are changing a clone the BrokerIDs should not be the same
            orderV2.BrokerId.Add("FAKE BROKER ID");
            var orderV3 = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderV2.BrokerId.SequenceEqual(orderV3.BrokerId))
            {
                Error("Broker IDs should not be the same!");
                throw new Exception("Broker IDs should not be the same!");
            }

            //Try and manipulate the orderV1 using UpdateOrderRequest
            //NOTICE: Orders should only be updated through their tickets!
            var updateFields = new UpdateOrderFields { Quantity = 99, Tag = "Pepe2!" };
            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderEvent.OrderId, updateFields);
            orderV1.ApplyUpdateOrderRequest(updateRequest);
            var orderV4 = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderV4.Quantity == orderV1.Quantity)
            {
                Error("Order quantity should not be the same!");
                throw new Exception("Order quantity should not be the same!");
            }

            if (orderV4.Tag == orderV1.Tag)
            {
                Error("Order tag should not be the same!");
                throw new Exception("Order tag should not be the same!");
            }
        }

        /// <summary>
        /// Will run at End of Algorithm
        /// We will be using this to check our order was updated!
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            //Get an updated copy of the order and compare to our original
            var updatedOrder = Transactions.GetOrderById(_ticket.OrderId);

            if (updatedOrder.Quantity == _originalOrder.Quantity)
            {
                Error("Quantities should have been updated!");
                throw new Exception("Quantities should have been updated!");
            }

            if (updatedOrder.Tag == _originalOrder.Tag)
            {
                Error("Tag should have been updated!");
                throw new Exception("Tag should have been updated!");
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
        public long DataPoints => 32;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-3.591%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99969.95"},
            {"Net Profit", "-0.030%"},
            {"Sharpe Ratio", "-11.996"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.024"},
            {"Beta", "0.027"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "5.399"},
            {"Tracking Error", "0.132"},
            {"Treynor Ratio", "-1.634"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$34000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.96%"},
            {"OrderListHash", "ef870dda527c0e929592a17fb1027c87"}
        };
    }
}
