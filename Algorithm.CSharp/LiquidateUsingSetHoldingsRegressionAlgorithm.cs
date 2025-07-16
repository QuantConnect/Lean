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
using System.Linq;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.RegressionTests
{
    /// <summary>
    /// A regression test algorithm that uses SetHoldings to liquidate the portfolio by setting holdings to zero.
    /// </summary>
    public class LiquidateUsingSetHoldingsRegressionAlgorithm : LiquidateRegressionAlgorithm
    {
        public override void PerformLiquidation()
        {
            var properties = new OrderProperties { TimeInForce = TimeInForce.GoodTilCanceled };
            OrderTickets.AddRange(SetHoldings(new List<PortfolioTarget>(), true, "LiquidatedTest", properties));
            var orders = Transactions.GetOrders().ToList();
            var orderTags = orders.Where(e => e.Tag == "LiquidatedTest").ToList();
            if (orderTags.Count != orders.Count)
            {
                throw new RegressionTestException("The tag was not set on all orders");
            }
            var orderProperties = orders.Where(e => e.Properties.TimeInForce == TimeInForce.GoodTilCanceled).ToList();
            if (orderProperties.Count != orders.Count)
            {
                throw new RegressionTestException("The properties were not set on all orders");
            }
        }

        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"OrderListHash", "2cdbee112f22755f26f640c97c305aae"}
        };
    }
}
