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
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="limit orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="updating orders" />
    /// <meta name="tag" content="regression test" />
    public class LimitFillRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Second);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice data)
        {
            if (data.ContainsKey("SPY"))
            {
                if (Time.Second == 0 && Time.Minute == 0)
                {
                    var goLong = Time < StartDate.AddDays(2);
                    var negative = goLong ? 1 : -1;
                    LimitOrder("SPY", negative*10, data["SPY"].Price);
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
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
            {"Total Trades", "34"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "8.974%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0.499"},
            {"Net Profit", "0.110%"},
            {"Sharpe Ratio", "2.894"},
            {"Probabilistic Sharpe Ratio", "58.789%"},
            {"Loss Rate", "25%"},
            {"Win Rate", "75%"},
            {"Profit-Loss Ratio", "1.00"},
            {"Alpha", "-0.063"},
            {"Beta", "0.148"},
            {"Annual Standard Deviation", "0.033"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-5.233"},
            {"Tracking Error", "0.187"},
            {"Treynor Ratio", "0.648"},
            {"Total Fees", "$34.00"},
            {"Fitness Score", "0.1"},
            {"Kelly Criterion Estimate", "62.773"},
            {"Kelly Criterion Probability Value", "0.176"},
            {"Sortino Ratio", "6.475"},
            {"Return Over Maximum Drawdown", "40.158"},
            {"Portfolio Turnover", "0.103"},
            {"Total Insights Generated", "34"},
            {"Total Insights Closed", "33"},
            {"Total Insights Analysis Completed", "33"},
            {"Long Insight Count", "31"},
            {"Short Insight Count", "2"},
            {"Long/Short Ratio", "1550.0%"},
            {"Estimated Monthly Alpha Value", "$8825.2328"},
            {"Total Accumulated Estimated Alpha Value", "$1421.8431"},
            {"Mean Population Estimated Insight Value", "$43.08615"},
            {"Mean Population Direction", "59.3353%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "67.2903%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "-1580763158"}
        };
    }
}
