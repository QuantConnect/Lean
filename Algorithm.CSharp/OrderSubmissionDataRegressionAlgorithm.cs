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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class OrderSubmissionDataRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<string, OrderSubmissionData> _orderSubmissionData = new Dictionary<string, OrderSubmissionData>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            
            AddEquity("SPY");
            AddForex("EURUSD", Resolution.Hour);

            Schedule.On(DateRules.EveryDay(), TimeRules.Noon, () =>
            {
                Liquidate();
                foreach (var ticker in new[] {"SPY", "EURUSD"})
                {
                    PlaceTrade(ticker);
                }
            });
        }
        private void PlaceTrade(string ticker)
        {
            var ticket = MarketOrder(ticker, 1000);
            var order = Transactions.GetOrderById(ticket.OrderId);
            var data = order.OrderSubmissionData;
            if (data == null || data.AskPrice == 0 || data.BidPrice == 0 || data.LastPrice == 0)
            {
                throw new Exception("Invalid Order Submission data detected");
            }

            if (_orderSubmissionData.ContainsKey(ticker))
            {
                var previous = _orderSubmissionData[ticker];
                if (previous.AskPrice == data.AskPrice || previous.BidPrice == data.BidPrice || previous.LastPrice == data.LastPrice)
                {
                    throw new Exception("Order Submission data didn't change");
                }
            }
            _orderSubmissionData[ticker] = data;
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
            {"Total Trades", "18"},
            {"Average Win", "0.88%"},
            {"Average Loss", "-0.95%"},
            {"Compounding Annual Return", "292.522%"},
            {"Drawdown", "3.400%"},
            {"Expectancy", "0.204"},
            {"Net Profit", "1.780%"},
            {"Sharpe Ratio", "11.817"},
            {"Probabilistic Sharpe Ratio", "66.756%"},
            {"Loss Rate", "38%"},
            {"Win Rate", "62%"},
            {"Profit-Loss Ratio", "0.93"},
            {"Alpha", "1.037"},
            {"Beta", "1.548"},
            {"Annual Standard Deviation", "0.34"},
            {"Annual Variance", "0.116"},
            {"Information Ratio", "17.38"},
            {"Tracking Error", "0.12"},
            {"Treynor Ratio", "2.596"},
            {"Total Fees", "$45.00"},
            {"Fitness Score", "0.986"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "9.326"},
            {"Return Over Maximum Drawdown", "45.056"},
            {"Portfolio Turnover", "2.728"},
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
            {"OrderListHash", "-46935513"}
        };
    }
}
