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
    /// This regression test algorithm reproduces GH issue 3239, where the stopLoss order
    /// place on <see cref="OnOrderEvent"/> was not being filled.
    /// </summary>
    public class StopLossOnOrderEventRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            _spy = AddEquity("SPY").Symbol;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            if (order.Tag == "Entry" && orderEvent.Status == OrderStatus.Filled)
            {
                Debug("Enter short at " + orderEvent.FillPrice + " set STOPLOSS at 151.0m");
                StopMarketOrder(order.Symbol, order.Quantity, 151.0m, "StopLoss");
            }
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                MarketOrder(_spy, -100, false, "Entry");
                Debug("Purchased Stock");
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
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-43.065%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.718%"},
            {"Sharpe Ratio", "-7.226"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "-32.327"},
            {"Annual Standard Deviation", "0.05"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-7.431"},
            {"Tracking Error", "0.05"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$2.00"}
        };
    }
}
