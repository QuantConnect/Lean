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
            {"Total Trades", "59"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "32.831%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "-0.919"},
            {"Net Profit", "0.364%"},
            {"Sharpe Ratio", "4.969"},
            {"Probabilistic Sharpe Ratio", "65.344%"},
            {"Loss Rate", "93%"},
            {"Win Rate", "7%"},
            {"Profit-Loss Ratio", "0.22"},
            {"Alpha", "-0.213"},
            {"Beta", "0.243"},
            {"Annual Standard Deviation", "0.054"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-10.175"},
            {"Tracking Error", "0.168"},
            {"Treynor Ratio", "1.106"},
            {"Total Fees", "$59.00"},
            {"Estimated Strategy Capacity", "$13000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "6.66%"},
            {"OrderListHash", "74d06b3d2605e1c7796284fd8835b3a5"}
        };
    }
}
