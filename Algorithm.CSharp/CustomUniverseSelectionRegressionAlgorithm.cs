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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm that verifies that securities added through
    /// <see cref="QCAlgorithm.AddEquity"/> API and universe selection
    /// both start sending data at the same time
    /// </summary>
    public class CustomUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            AddEquity("AAPL", Resolution.Daily);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse(SecurityType.Equity,
                "SecondUniverse",
                Resolution.Daily,
                Market.USA,
                UniverseSettings,
                time => new[] { "SPY" });
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.Count != 2)
            {
                throw new Exception($"Unexpected data count: {data.Count}");
            }
            if (ActiveSecurities.Count != 2)
            {
                throw new Exception($"Unexpected ActiveSecurities count: {ActiveSecurities.Count}");
            }
            if (!Portfolio.Invested)
            {
                SetHoldings(Securities.Keys.First(symbol => symbol.Value == "SPY"), 1);
                Debug("Purchased Stock");
            }
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
        public long DataPoints => 58;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "240.939%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101694.38"},
            {"Net Profit", "1.694%"},
            {"Sharpe Ratio", "8.637"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.159%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.053"},
            {"Beta", "1.003"},
            {"Annual Standard Deviation", "0.223"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "-35.82"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.922"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$970000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "20.19%"},
            {"OrderListHash", "39a84b9f15bb4e8ead0f0ecb59f28562"}
        };
    }
}
