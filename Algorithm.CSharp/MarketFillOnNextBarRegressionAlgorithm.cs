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
    /// This algorithm is a test case for market orders not being filled on fill-forward bars (stale prices),
    /// but filled at the open price of the next bar.
    /// </summary>
    public class MarketFillOnNextBarRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(10000);

            _symbol = AddEquity("AAPL", Resolution.Hour, Market.USA, true, 1).Symbol;
            AddEquity("SPY", Resolution.Minute, Market.USA, true, 1);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
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
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the event</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{Time} - OnOrderEvent: {orderEvent}");
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
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "54.968%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.562%"},
            {"Sharpe Ratio", "1.894"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "26.43"},
            {"Annual Standard Deviation", "0.154"},
            {"Annual Variance", "0.024"},
            {"Information Ratio", "1.823"},
            {"Tracking Error", "0.154"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$1.00"}
        };
    }
}
