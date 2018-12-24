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
    /// This regression test algorithm reproduces issue reported in GB issue https://github.com/QuantConnect/Lean/issues/2655
    /// fixed in PR https://github.com/QuantConnect/Lean/pull/2659
    /// </summary>
    public class DailyResolutionSplitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2018, 2, 13);  //Set Start Date
            SetEndDate(2018, 06, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            _symbol = AddEquity("UPRO", Resolution.Daily).Symbol;
        }

        public override void OnData(Slice data)
        {
            if (Time.Date == new DateTime(2018, 05, 22).Date)
            {
                MarketOrder(_symbol, 100);
            }

            if (Time.Date == new DateTime(2018, 05, 23).Date)
            {
                MarketOrder(_symbol, 100);
            }

            if (Time.Date == new DateTime(2018, 05, 24).Date)
            {
                MarketOrder(_symbol, 100);
            }

            if (Time.Date == new DateTime(2018, 05, 25).Date)
            {
                MarketOrder(_symbol, 100);
            }

            if (Time.Date == new DateTime(2018, 05, 29).Date)
            {
                Liquidate();
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log($"{orderEvent}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0.520%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.155%"},
            {"Sharpe Ratio", "0.242"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.118"},
            {"Beta", "-5.794"},
            {"Annual Standard Deviation", "0.022"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.644"},
            {"Tracking Error", "0.022"},
            {"Treynor Ratio", "-0.001"},
            {"Total Fees", "$4.00"}
        };
    }
}
