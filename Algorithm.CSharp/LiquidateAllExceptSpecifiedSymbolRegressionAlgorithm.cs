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
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    /// <summary>
    /// Tests liquidating all portfolio holdings except a specific symbol, verifying canceled orders and correct tags.
    /// </summary>
    public class LiquidateAllExceptSpecifiedSymbolRegressionAlgorithm : LiquidateRegressionAlgorithm
    {
        public override void Rebalance()
        {
            // Place a MarketOrder
            MarketOrder(Ibm, 10);

            // Place a LimitOrder to sell 1 share at a price below the current market price
            LimitOrder(Ibm, 1, Securities[Ibm].Price - 5);

            // Liquidate the remaining symbols in the portfolio, except for SPY
            var orderProperties = new OrderProperties { TimeInForce = TimeInForce.GoodTilCanceled };
            OrderTickets.AddRange(SetHoldings(Spy, 1, true, "LiquidatedTest", orderProperties));
        }

        public override void OnEndOfAlgorithm()
        {
            // Retrieve all orders from the Transactions for analysis
            var orders = Transactions.GetOrders().ToList();

            // Count orders that were canceled
            var canceledOrdersCount = orders.Where(order => order.Status == OrderStatus.Canceled).Count();

            // Expectation 1: There should be exactly 4 canceled orders.
            // This occurs because Rebalance is called twice, and each call to Rebalance
            // (e.g., LimitOrder or MarketOrder) that get canceled due to the Liquidate call in SetHoldings.
            if (canceledOrdersCount != 4)
            {
                throw new RegressionTestException($"Expected 4 canceled orders, but found {canceledOrdersCount}.");
            }

            // Count orders that were not canceled
            var nonCanceledOrdersCount = orders.Where(order => order.Status != OrderStatus.Canceled).Count();

            // Expectation 2: There should be exactly 1 non-canceled order after the Liquidate call.
            // This occurs because all holdings except SPY are liquidated, and a new order is placed for SPY.
            if (nonCanceledOrdersCount != 1)
            {
                throw new RegressionTestException($"Expected 1 non-canceled order, but found {nonCanceledOrdersCount}.");
            }

            if (nonCanceledOrdersCount != OrderTickets.Count)
            {
                throw new RegressionTestException($"Expected {OrderTickets.Count} non-canceled orders, but found {nonCanceledOrdersCount}.");
            }

            // Verify all tags are "LiquidatedTest"
            var invalidTags = orders.Where(order => order.Tag != "LiquidatedTest").ToList();
            if (invalidTags.Count != 0)
            {
                var invalidTagsDetails = string.Join(", ", invalidTags.Select(order => $"OrderID {order.Id}, Tag: {order.Tag}"));
                throw new RegressionTestException($"All orders should have the tag 'LiquidatedTest', but found invalid tags: {invalidTagsDetails}.");
            }
        }

        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "36.497%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100569.90"},
            {"Net Profit", "0.570%"},
            {"Sharpe Ratio", "9.031"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "86.638%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.003"},
            {"Beta", "0.559"},
            {"Annual Standard Deviation", "0.028"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-8.867"},
            {"Tracking Error", "0.023"},
            {"Treynor Ratio", "0.447"},
            {"Total Fees", "$1.95"},
            {"Estimated Strategy Capacity", "$850000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.23%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "611f320cf76c36e8cdcb1938e4154682"}
        };
    }
}
