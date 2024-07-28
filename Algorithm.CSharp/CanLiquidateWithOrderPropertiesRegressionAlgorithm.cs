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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test we can liquidate our portfolio holdings using order properties
    /// </summary>
    public class CanLiquidateWithOrderPropertiesRegressionAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly DateTime _openExchange = new (2014, 6, 6, 10, 0, 0);
        private readonly DateTime _closeExchange = new(2014, 6, 6, 16, 0, 0);

        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 6);

            AddEquity("AAPL", Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            if (Time > _openExchange && Time < _closeExchange)
            {
                if (!Portfolio.Invested)
                {
                    MarketOrder("AAPL", 10);
                }
                else
                {
                    var orderProperties = new OrderProperties() { TimeInForce = TimeInForce.Day };
                    var tickets = Liquidate(asynchronous: true, orderProperties: orderProperties);
                    foreach (var ticket in tickets)
                    {
                        if (ticket.SubmitRequest.OrderProperties.TimeInForce != TimeInForce.Day)
                        {
                            throw new RegressionTestException("The TimeInForce for all orders should be daily, but it was {ticket.SubmitRequest.OrderProperties.TimeInForce}");
                        }
                    }
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
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1583;

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
            {"Total Orders", "359"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99637.08"},
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
            {"Total Fees", "$359.00"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "37.56%"},
            {"OrderListHash", "e9e8a07dc58bff7198181f9fafb58834"}
        };
    }
}
