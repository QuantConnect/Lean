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
        private OrderTicket _validOrderTicketExtendedMarketHours;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7); //Set Start Date
            SetEndDate(2013, 10, 8); //Set End Date

            var ticker = "SPY";
            AddEquity(ticker, Resolution.Minute);

            Schedule.On(DateRules.Today, TimeRules.At(17,00), () =>
            {
                _validOrderTicketExtendedMarketHours = MarketOnCloseOrder("SPY", 2);
            });

            // Modify our submission buffer time to 10 minutes
            Orders.MarketOnCloseOrder.SubmissionTimeBuffer = TimeSpan.FromMinutes(10);
        }

        public override void OnData(Slice slice)
        {
            // Test our ability to submit MarketOnCloseOrders
            // Because we set our buffer to 10 minutes, any order placed
            // before 3:50PM should be accepted, any after marked invalid
            if (Time.Hour == 15 && Time.Minute == 49 && _validOrderTicket == null)
            {
                // Will not throw an order error and execute
                _validOrderTicket = MarketOnCloseOrder("SPY", 2);
            }

            if (Time.Hour == 15 && Time.Minute == 51 && _invalidOrderTicket == null)
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

            // Verify that our second good order filled
            if (_validOrderTicketExtendedMarketHours.Status != OrderStatus.Filled)
            {
                throw new Exception("Valid order during extended market hours failed to fill");
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
        public long DataPoints => 1582;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99994.65"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$18000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.29%"},
            {"OrderListHash", "105d3eb1a9b673ce88238a93a63d6d08"}
        };
    }
}
