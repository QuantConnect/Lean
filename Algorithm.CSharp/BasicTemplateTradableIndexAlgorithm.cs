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
 *
*/

using System;
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Orders;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add index asset types and change the tradable condition
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="indexes" />
    public class BasicTemplateTradableIndexAlgorithm : BasicTemplateIndexAlgorithm
    {
        private OrderTicket _ticket;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Securities[Spx].IsTradable = true;
        }

        /// <summary>
        /// Index EMA Cross trading underlying.
        /// </summary>
        public override void OnData(Slice slice)
        {
            base.OnData(slice);
            _ticket ??= MarketOrder(Spx, 1);
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_ticket.Status.IsFill())
            {
                throw new Exception("Index is tradable.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "5"},
            {"Average Win", "7.08%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "602.278%"},
            {"Drawdown", "3.400%"},
            {"Expectancy", "677.669"},
            {"Start Equity", "1000000"},
            {"End Equity", "1064342.82"},
            {"Net Profit", "6.434%"},
            {"Sharpe Ratio", "-4.563"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0.781%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1356.34"},
            {"Alpha", "-0.169"},
            {"Beta", "0.073"},
            {"Annual Standard Deviation", "0.028"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-6.684"},
            {"Tracking Error", "0.099"},
            {"Treynor Ratio", "-1.771"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Portfolio Turnover", "24.03%"},
            {"OrderListHash", "fcd6fddb0a315e21095c2b35eb633e2b"}
        };
    }
}
