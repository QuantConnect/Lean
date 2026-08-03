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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating how to place Pegged-to-Midpoint orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="pegged to midpoint order" />
    public class PeggedToMidpointRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _buyOrderTicket;
        private OrderTicket _sellOrderTicket;
        protected virtual bool AsynchronousOrders => false;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);
            _symbol = AddEquity("SPY").Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_symbol))
            {
                return;
            }

            if (_buyOrderTicket == null)
            {
                _buyOrderTicket = PeggedToMidpointOrder(_symbol, 1, limitPrice: 0m, limitPriceOffset: 0m,
                    asynchronous: AsynchronousOrders);
            }
            else if (_sellOrderTicket == null && Portfolio.Invested)
            {
                _sellOrderTicket = PeggedToMidpointOrder(_symbol, -1, limitPrice: 0m, limitPriceOffset: 0m,
                    asynchronous: AsynchronousOrders);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order is not Orders.PeggedToMidpointOrder)
            {
                throw new RegressionTestException($"Expected PeggedToMidpointOrder but got {order.GetType().Name}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_sellOrderTicket?.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException("Expected sell PeggedToMidpoint order to be filled by end of algorithm.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.198%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99997.46"},
            {"Net Profit", "-0.003%"},
            {"Sharpe Ratio", "-108.977"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "1.216%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.009"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.915"},
            {"Tracking Error", "0.222"},
            {"Treynor Ratio", "-32.005"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$17000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.06%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d310603dec0f507a6b9b8a9eb21c4717"}
        };
    }
}
