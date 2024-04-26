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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing the effect of No <see cref="IAlgorithmSettings.MinimumOrderMarginPortfolioPercentage"/>
    /// causing multiple trades to be filled, see <see cref="MinimumOrderMarginRegressionAlgorithm"/> instead
    /// </summary>
    public class NoMinimumOrderMarginRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            AddEquity("SPY", Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            SetHoldings("SPY", 0.25);
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "38.897%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "-0.118"},
            {"Start Equity", "100000"},
            {"End Equity", "100420.97"},
            {"Net Profit", "0.421%"},
            {"Sharpe Ratio", "5.45"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.350%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.76"},
            {"Alpha", "-0.19"},
            {"Beta", "0.249"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-10.01"},
            {"Tracking Error", "0.167"},
            {"Treynor Ratio", "1.213"},
            {"Total Fees", "$5.00"},
            {"Estimated Strategy Capacity", "$63000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.15%"},
            {"OrderListHash", "72d5203e9911bad556de371750fe0278"}
        };
    }
}
