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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class MarketOnCloseOrderBufferRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _validOrderTicket;
        private OrderTicket _invalidOrderTicket;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 4); //Set Start Date
            SetEndDate(2013, 10, 4); //Set End Date

            var ticker = "SPY";
            AddEquity(ticker, Resolution.Minute);

            // Modify our submission buffer time to 10 minutes
            Orders.MarketOnCloseOrder.SubmissionTimeBuffer = TimeSpan.FromMinutes(10);
        }

        public override void OnData(Slice slice)
        {
            // Test our ability to submit MarketOnCloseOrders
            // Because we set our buffer to 10 minutes, any order placed
            // before 3:50PM should be accepted, any after marked invalid

            if (Time.Hour == 15 && Time.Minute == 49)
            {
                // Will not throw an order error and execute
                _validOrderTicket = MarketOnCloseOrder("SPY", 2);
            }

            if (Time.Hour == 15 && Time.Minute == 51)
            {
                // Will throw an order error and be marked invalid
                _invalidOrderTicket = MarketOnCloseOrder("SPY", 2);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // Set it back to default for other regressions
            Orders.MarketOnCloseOrder.SubmissionTimeBuffer = Orders.MarketOnCloseOrder.DefaultSubmissionTimeBuffer;

            // Verify that our good order filled
            if (_validOrderTicket.Status != OrderStatus.Filled)
            {
                throw new Exception("Valid order failed to fill");
            }

            // Verify our order was marked invalid
            if (_invalidOrderTicket.Status != OrderStatus.Invalid)
            {
                throw new Exception("Invalid order was not rejected");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$23000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "0"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "9fd6d48c807420293f903a8d8fdefd60"}
        };
    }
}
