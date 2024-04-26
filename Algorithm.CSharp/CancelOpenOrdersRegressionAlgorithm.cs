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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm is a regression test case for CancelOpenOrders and rejected orders
    /// </summary>
    public class CancelOpenOrdersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2017, 9, 3);  //Set Start Date
            SetEndDate(2017, 9, 3);    //Set End Date
            SetCash(1000);             //Set Strategy Cash

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            AddCrypto("BTCUSD");
            AddCrypto("ETHUSD");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (UtcTime.Hour != 6) return;

            if (UtcTime.Minute == 0)
            {
                // this order will be rejected for insufficient funds
                LimitOrder("BTCUSD", 100m, 4734.64m);
                LimitOrder("ETHUSD", 1.35505027m, 368.8m);
            }
            else if (UtcTime.Minute == 6)
            {
                Transactions.CancelOpenOrders("BTCUSD");
                LimitOrder("BTCUSD", 0.10576312m, 4727.61m);
            }
            else if (UtcTime.Minute == 12)
            {
                Transactions.CancelOpenOrders("BTCUSD");
                LimitOrder("BTCUSD", 0.10576267m, 4727.63m);
            }
            else if (UtcTime.Minute == 18)
            {
                Transactions.CancelOpenOrders("BTCUSD");
                LimitOrder("BTCUSD", 0.10547724m, 4740.42m);
            }
            else if (UtcTime.Minute == 24)
            {
                Transactions.CancelOpenOrders("BTCUSD");
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(orderEvent.ToString());
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation.
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            const int expectedOrders = 5;
            var expectedStatus = new[] { OrderStatus.Invalid, OrderStatus.Filled, OrderStatus.Canceled, OrderStatus.Canceled, OrderStatus.Filled };

            var orders = Transactions.GetOrders(x => true).ToList();

            if (orders.Count != expectedOrders)
            {
                throw new Exception($"Expected orders: {expectedOrders}, actual orders: {orders.Count}");
            }

            for (var i = 0; i < expectedOrders; i++)
            {
                var order = orders[i];
                if (order.Status != expectedStatus[i])
                {
                    throw new Exception($"Invalid status for order {order.Id}, Expected: {expectedStatus[i]}, actual: {order.Status}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5765;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 120;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000.00"},
            {"End Equity", "955.69"},
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
            {"Estimated Strategy Capacity", "$370000.00"},
            {"Lowest Capacity Asset", "ETHUSD 2XR"},
            {"Portfolio Turnover", "104.59%"},
            {"OrderListHash", "5277847166fcd10cde634e3986e1d285"}
        };
    }
}
