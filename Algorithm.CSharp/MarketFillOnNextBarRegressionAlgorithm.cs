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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm showing how to disable fill on stale prices for market orders
    /// <meta name="tag" content="trading and orders" />
    /// </summary>
    public class MarketFillOnNextBarRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(10000);

            // Do not allow fill on stale prices
            Settings.FillOnStalePrices = false;

            _symbol = AddEquity("AAPL", Resolution.Hour, Market.USA, true, 1).Symbol;
            AddEquity("SPY", Resolution.Minute, Market.USA, true, 1);
        }

        public override void OnData(Slice data)
        {
            if (Time.Day != 8) return;

            if (Time.Hour == 9 && Time.Minute == 45)
            {
                var lastData = Securities[_symbol].GetLastData();

                // log price from market close of 10/7/2013 at 4 PM
                Log($"{Time} - latest price: {lastData.EndTime} - {lastData.Price}");

                // this market order will be filled at the open of the next hourly bar (10:00 AM)
                SetHoldings(_symbol, 0.85);

                if (Portfolio.Invested)
                {
                    // order filled at price of last market close
                    throw new Exception("Unexpected fill on fill-forward bar with stale price.");
                }
            }
            if (Time.Hour == 9 && Time.Minute == 50)
            {
                var openOrders = Transactions.GetOpenOrders(_symbol);
                if (openOrders.Count == 0)
                {
                    throw new Exception("Pending market order was expected on current bar.");
                }
            }
            else if (Time.Hour == 10 && Time.Minute == 0)
            {
                if (!Portfolio.Invested)
                {
                    throw new Exception("Order fill was expected on current bar.");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5864;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new ()
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "59.593%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.599%"},
            {"Sharpe Ratio", "2.685"},
            {"Probabilistic Sharpe Ratio", "54.624%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.617"},
            {"Beta", "0.551"},
            {"Annual Standard Deviation", "0.177"},
            {"Annual Variance", "0.031"},
            {"Information Ratio", "-9.317"},
            {"Tracking Error", "0.162"},
            {"Treynor Ratio", "0.862"},
            {"Total Fees", "$2.75"},
            {"Estimated Strategy Capacity", "$44000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "17.25%"},
            {"OrderListHash", "fc4ade727a8bcd957cea4f8f129376f7"}
        };
    }
}
