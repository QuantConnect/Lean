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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates extended market hours trading.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="assets" />
    /// <meta name="tag" content="regression test" />
    public class ExtendedMarketTradingRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _lastAction;
        private Symbol _spy;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            _spy = AddEquity("SPY", Resolution.Minute, Market.USA, true, 0m, true).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public void OnData(TradeBars data)
        {
            //Only take an action once a day.
            if (_lastAction.Date == Time.Date) return;
            TradeBar spyBar = data["SPY"];

            //If it isnt during market hours, go ahead and buy ten!
            if (!InMarketHours())
            {
                LimitOrder(_spy, 10, spyBar.Low);
                _lastAction = Time;
            }
        }

        /// <summary>
        /// Order events are triggered on order status changes. There are many order events including non-fill messages.
        /// </summary>
        /// <param name="orderEvent">OrderEvent object with details about the order status</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (InMarketHours())
            {
                throw new Exception("Order processed during market hours.");
            }

            Log($"{orderEvent}");
        }

        /// <summary>
        /// Check if we are in Market Hours, NYSE is open from (9:30 am to 4 pm)
        /// </summary>
        public bool InMarketHours()
        {
            TimeSpan now = Time.TimeOfDay;
            TimeSpan open = new TimeSpan(09, 30, 0);
            TimeSpan close = new TimeSpan(16, 0, 0);

            return (open < now) && (close > now);
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
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "8.332%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.106%"},
            {"Sharpe Ratio", "7.474"},
            {"Probabilistic Sharpe Ratio", "85.145%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.007"},
            {"Beta", "0.036"},
            {"Annual Standard Deviation", "0.007"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-7.03"},
            {"Tracking Error", "0.186"},
            {"Treynor Ratio", "1.557"},
            {"Total Fees", "$4.00"},
            {"Fitness Score", "0.012"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "38.663"},
            {"Return Over Maximum Drawdown", "238.773"},
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
            {"OrderListHash", "1717552327"}
        };
    }
}
