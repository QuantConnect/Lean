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
using QuantConnect.Data;

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
        public override void OnData(Slice data)
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 9643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "10.774%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100135.59"},
            {"Net Profit", "0.136%"},
            {"Sharpe Ratio", "8.723"},
            {"Sortino Ratio", "41.728"},
            {"Probabilistic Sharpe Ratio", "90.001%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.005"},
            {"Beta", "0.039"},
            {"Annual Standard Deviation", "0.009"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.852"},
            {"Tracking Error", "0.214"},
            {"Treynor Ratio", "2.102"},
            {"Total Fees", "$5.00"},
            {"Estimated Strategy Capacity", "$14000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.44%"},
            {"OrderListHash", "ee17a1434ec69d64c82c0953b0a50a71"}
        };
    }
}
