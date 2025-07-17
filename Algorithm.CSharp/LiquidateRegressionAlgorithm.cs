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
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// A regression test algorithm that places market and limit orders, then liquidates all holdings,
    /// ensuring orders are canceled and the portfolio is empty.
    /// </summary>
    public class LiquidateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected List<OrderTicket> OrderTickets { get; private set; }
        protected Symbol Spy { get; private set; }
        protected Symbol Ibm { get; private set; }
        public override void Initialize()
        {
            SetStartDate(2018, 1, 4);
            SetEndDate(2018, 1, 10);
            Spy = AddEquity("SPY", Resolution.Daily).Symbol;
            Ibm = AddEquity("IBM", Resolution.Daily).Symbol;
            OrderTickets = new List<OrderTicket>();

            // Schedule Rebalance method to be called on specific dates
            Schedule.On(DateRules.On(2018, 1, 5), TimeRules.Midnight, Rebalance);
            Schedule.On(DateRules.On(2018, 1, 8), TimeRules.Midnight, Rebalance);
        }

        public virtual void Rebalance()
        {
            // Place a MarketOrder
            MarketOrder(Ibm, 10);

            // Place a LimitOrder to sell 1 share at a price below the current market price
            LimitOrder(Ibm, 1, Securities[Ibm].Price - 5);

            LimitOrder(Spy, 1, Securities[Spy].Price - 5);

            // Liquidate all remaining holdings immediately
            PerformLiquidation();
        }

        public virtual void PerformLiquidation()
        {
            Liquidate();
        }

        public override void OnEndOfAlgorithm()
        {
            // Check if there are any orders that should have been canceled
            var orders = Transactions.GetOrders().ToList();
            var nonCanceledOrdersCount = orders.Where(e => e.Status != OrderStatus.Canceled).Count();
            if (nonCanceledOrdersCount > 0)
            {
                throw new RegressionTestException($"There are {nonCanceledOrdersCount} orders that should have been cancelled");
            }

            if (OrderTickets.Count > 0)
            {
                throw new RegressionTestException("The number of order tickets must be zero because all orders were cancelled");
            }

            // Check if there are any holdings left in the portfolio
            foreach (var kvp in Portfolio)
            {
                var symbol = kvp.Key;
                var holdings = kvp.Value;
                if (holdings.Quantity != 0)
                {
                    throw new RegressionTestException($"There are {holdings.Quantity} holdings of {symbol} in the portfolio");
                }
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
        public long DataPoints => 53;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-10.398"},
            {"Tracking Error", "0.045"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "9423c872a626fb856b7c377686c28d85"}
        };
    }
}
