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
    /// Regression algorithm asserting that in backtesting, orders are submitted in the same time step even when asynchronous
    /// </summary>
    public class BacktestingAsynchronousOrdersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);
            SetCash(100000);

            _symbol = AddEquity("SPY").Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                var marketOrderTicket = MarketOrder(_symbol, 100, asynchronous: false);
                AssertMarketOrderStatus(marketOrderTicket);

                var asyncMarketOrderTicket = MarketOrder(_symbol, -100, asynchronous: true);
                AssertMarketOrderStatus(asyncMarketOrderTicket);

                var limitPrice = Securities[_symbol].Price * 0.95m;
                var limitOrderTicket = LimitOrder(_symbol, 100, limitPrice, asynchronous: false);
                AssertLimitOrderStatus(limitOrderTicket);

                var asyncLimitOrderTicket = LimitOrder(_symbol, -100, limitPrice, asynchronous: true);
                AssertLimitOrderStatus(asyncLimitOrderTicket);
            }
        }

        private static void AssertMarketOrderStatus(OrderTicket ticket)
        {
            // In backtesting the order should be submitted and filled right away.
            // Note that OrderSet event will not be fired if there is an error when processing the order submission,
            // but this is a happy case
            if (!ticket.OrderSet.WaitOne(0))
            {
                throw new RegressionTestException("Order was not submitted immediately in backtesting mode");
            }
            if (!ticket.OrderClosed.WaitOne(0))
            {
                throw new RegressionTestException("Order was not filled immediately in backtesting mode");
            }
            if (ticket.Status != OrderStatus.Filled)
            {
                throw new RegressionTestException($"Order status is not filled: {ticket.Status}");
            }
        }

        private static void AssertLimitOrderStatus(OrderTicket ticket)
        {
            // In backtesting the order should be submitted right away but not filled since price hasn't moved even when asynchronous
            // Note that OrderSet event will not be fired if there is an error when processing the order submission,
            // but this is a happy case
            if (!ticket.OrderSet.WaitOne(0))
            {
                throw new RegressionTestException("Asynchronous limit order was not submitted immediately in backtesting mode");
            }
            if (ticket.OrderClosed.WaitOne(0))
            {
                throw new RegressionTestException("Asynchronous limit order was filled immediately in backtesting mode when it shouldn't");
            }
            if (ticket.Status != OrderStatus.Submitted)
            {
                throw new RegressionTestException($"Order status is not submitted: {ticket.Status}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1582;

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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100168.20"},
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
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$22000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "21.72%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "65f010e904a929e5383f0920a3c5b797"}
        };
    }
}
