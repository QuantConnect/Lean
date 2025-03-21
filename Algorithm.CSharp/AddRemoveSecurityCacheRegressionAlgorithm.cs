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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm making sure the securities cache is reset correctly once it's removed from the algorithm
    /// </summary>
    public class AddRemoveSecurityCacheRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            AddEquity("SPY", Resolution.Minute, extendedMarketHours: true);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
            }

            if (Time.Day == 11)
            {
                return;
            }
            if (!ActiveSecurities.ContainsKey("AIG"))
            {
                var aig = AddEquity("AIG", Resolution.Minute);

                var ticket = MarketOrder("AIG", 1);

                if (ticket.Status != OrderStatus.Invalid || aig.HasData || aig.Price != 0)
                {
                    throw new RegressionTestException("Expected order to always be invalid because there is no data yet!");
                }
            }
            else
            {
                RemoveSecurity("AIG");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 11202;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "19"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "271.720%"},
            {"Drawdown", "2.500%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "101753.84"},
            {"Net Profit", "1.754%"},
            {"Sharpe Ratio", "11.954"},
            {"Sortino Ratio", "29.606"},
            {"Probabilistic Sharpe Ratio", "74.160%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.616"},
            {"Beta", "0.81"},
            {"Annual Standard Deviation", "0.185"},
            {"Annual Variance", "0.034"},
            {"Information Ratio", "3.961"},
            {"Tracking Error", "0.061"},
            {"Treynor Ratio", "2.737"},
            {"Total Fees", "$21.45"},
            {"Estimated Strategy Capacity", "$830000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "20.49%"},
            {"OrderListHash", "6ebe462373e2ecc22de8eb2fe114d704"}
        };
    }
}
