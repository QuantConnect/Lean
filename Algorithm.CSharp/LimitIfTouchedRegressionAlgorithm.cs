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
    /// Basic algorithm demonstrating how to place LimitIfTouched orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />`
    /// <meta name="tag" content="limit if touched order"/>
    public class LimitIfTouchedRegressionAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SubmitOrderRequest request;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 15);    //Set End Date
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
                if (Time.Second == 0 && Time.Minute == 0 && Transactions.GetOpenOrders().Count < 1)
                {
                    var goLong = Time < StartDate.AddDays(2);
                    var negative = goLong ? 1 : -1;
                    request = new SubmitOrderRequest(OrderType.LimitIfTouched, SecurityType.Equity, "SPY",
                        negative * 10, 0, data["SPY"].Price - (decimal) 0.5 * negative,
                        data["SPY"].Price - (decimal) negative, UtcTime, $"LIT - {UtcTime}");
                    Transactions.AddOrder(request);
                    Debug($"Submitted: {request.Tag}");
                }
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages => new[] { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "5"},
            {"Average Win", "0.02%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1.451%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.034%"},
            {"Sharpe Ratio", "4.704"},
            {"Probabilistic Sharpe Ratio", "77.396%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.003"},
            {"Beta", "0.015"},
            {"Annual Standard Deviation", "0.003"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-4.187"},
            {"Tracking Error", "0.183"},
            {"Treynor Ratio", "0.989"},
            {"Total Fees", "$5.00"},
            {"Fitness Score", "0.012"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "17.591"},
            {"Return Over Maximum Drawdown", "172.635"},
            {"Portfolio Turnover", "0.012"},
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
            {"OrderListHash", "291160226"}
        };
    }
}
