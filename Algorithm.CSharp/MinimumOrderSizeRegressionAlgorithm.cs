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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm asserts that the minimum order size is respected at the moment of
    /// place an order or update an order
    /// </summary>
    public class MinimumOrderSizeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _sentOrders;
        public override void Initialize()
        {
            SetStartDate(2013, 10, 1);
            SetEndDate(2013, 10, 1);
            SetBrokerageModel(Brokerages.BrokerageName.Bitfinex, AccountType.Cash);
            AddCrypto("BTCUSD", Resolution.Hour);
        }

        public override void OnData(Slice slice)
        {
            if (!_sentOrders)
            {
                _sentOrders = true;

                // Place an order that will fail because of the size
                var invalidOrder = MarketOrder("BTCUSD", 0.00002);
                if (invalidOrder.Status != OrderStatus.Invalid)
                {
                    throw new Exception("Invalid order expected, order size is less than allowed");
                }

                // Update an order that fails because of the size
                var validOrderOne = LimitOrder("BTCUSD", 0.0002, Securities["BTCUSD"].Price - 0.1m,  "NotUpdated");
                validOrderOne.Update(new UpdateOrderFields()
                {
                    Quantity = 0.00002m,
                    Tag = "Updated"
                });

                // Place and update an order that will succeed
                var validOrderTwo = LimitOrder("BTCUSD", 0.0002, Securities["BTCUSD"].Price - 0.1m, "NotUpdated");
                validOrderTwo.Update(new UpdateOrderFields()
                {
                    Quantity = 0.002m,
                    Tag = "Updated"
                });
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);

            // Update of validOrderOne is expected to fail
            if( (order.Id == 2) && (order.LastUpdateTime != null) && (order.Tag == "Updated"))
            {
                throw new Exception("Order update expected to fail");
            }

            // Update of validOrdertwo is expected to succeed
            if ((order.Id == 3) && (order.LastUpdateTime != null) && (order.Tag == "NotUpdated"))
            {
                throw new Exception("Order update expected to succeed");
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
        public long DataPoints => 54;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.0"},
            {"End Equity", "100000.00"},
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
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "BTCUSD E3"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "c4eb9c8722ee647ec2925cf7b936ce69"}
        };
    }
}
