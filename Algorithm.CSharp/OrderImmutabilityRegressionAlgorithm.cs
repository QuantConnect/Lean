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
    /// This regression algorithm tests that orders are unchangeable from the QCAlgorithm Layer
    /// Orders should only be modifiable via their ticket and only in permitted ways
    /// </summary>
    /// <meta name="tag" content="backtesting brokerage" />
    /// <meta name="tag" content="regression test" />
    /// <meta name="tag" content="options" />
    public class OrderImmutabilityRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 17);    //Set End Date
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
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
            }
        }

        /// <summary>
        /// All order events get pushed through this function
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

            //Try and manipulate the orderV1 using UpdateOrderRequest
            //NOTICE: Orders should only be updated through their tickets!
            var updateFields = new UpdateOrderFields { Quantity = 99, Tag = "Pepe" };
            var updateRequest = new UpdateOrderRequest(DateTime.Now, orderEvent.OrderId, updateFields);
            orderV1.ApplyUpdateOrderRequest(updateRequest);

            //Get another copy of the order and compare
            var orderV3 = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderV1.Quantity == orderV3.Quantity)
            {
                Error("Quantities should not be changed!");
                throw new Exception("Quantities should not be changed!");
            }
            
            if (orderV1.Tag == orderV3.Tag)
            {
                Error("Tag should not be changed!");
                throw new Exception("Tag should not be changed!");
            }

            //Try and manipulate orderV2 using the only external accessor BrokerID
            orderV2.BrokerId.Add("FAKE BROKER ID");

            //Get another copy of the order and compare
            var orderV4 = Transactions.GetOrderById(orderEvent.OrderId);

            if (orderV2.BrokerId == orderV4.BrokerId)
            {
                Error("BrokerIDs should not be the same!");
                throw new Exception("BrokerIDs should not be the same!");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "246.000%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "3.459%"},
            {"Sharpe Ratio", "10.11"},
            {"Probabilistic Sharpe Ratio", "83.150%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.935"},
            {"Beta", "-0.119"},
            {"Annual Standard Deviation", "0.16"},
            {"Annual Variance", "0.026"},
            {"Information Ratio", "-4.556"},
            {"Tracking Error", "0.221"},
            {"Treynor Ratio", "-13.568"},
            {"Total Fees", "$3.26"},
            {"Fitness Score", "0.111"},
            {"OrderListHash", "1268340653"}
        };
    }
}
