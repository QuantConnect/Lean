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
using QuantConnect.Orders;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of the default position group Not allowing us to fill orders above our margin available
    /// </summary>
    public class DefaultMarginMultipleOrdersRegressionAlgorithm : NullMarginMultipleOrdersRegressionAlgorithm
    {
        protected override void OverrideMarginModels()
        {
            // we use the default
        }

        protected override void AssertState(OrderTicket ticket, int expectedGroupCount, int expectedMarginUsed)
        {
            if (ticket.Status != OrderStatus.Invalid)
            {
                throw new Exception($"Unexpected order status {ticket.Status} for symbol {ticket.Symbol} and quantity {ticket.Quantity}");
            }
            if (Portfolio.Positions.Groups.Count != 0)
            {
                throw new Exception($"Unexpected position group count {Portfolio.Positions.Groups.Count} for symbol {ticket.Symbol} and quantity {ticket.Quantity}");
            }
            if (Portfolio.TotalMarginUsed != 0)
            {
                throw new Exception($"Unexpected margin used {Portfolio.TotalMarginUsed} for symbol {ticket.Symbol} and quantity {ticket.Quantity}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000"},
            {"End Equity", "10000"},
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
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d9133c55f97224d3fd291607d75a6aeb"}
        };
    }
}
