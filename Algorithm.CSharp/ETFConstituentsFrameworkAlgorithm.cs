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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of using ETFConstituentsUniverseSelectionModel
    /// </summary>
    public class ETFConstituentsFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2020, 12, 7);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            var symbol = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            AddUniverseSelection(new ETFConstituentsUniverseSelectionModel(symbol, UniverseSettings, ETFConstituentsFilter));

            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));

            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }

        private protected IEnumerable<Symbol> ETFConstituentsFilter(IEnumerable<ETFConstituentData> constituents)
        {
            // Get the 10 securities with the largest weight in the index
            return constituents.OrderByDescending(c => c.Weight).Take(8).Select(c => c.Symbol);
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
        public long DataPoints => 565;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "7"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "61.105%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0"},
            {"Starting Equity", "100000"},
            {"Ending Equity", "100918.771"},
            {"Net Profit", "0.919%"},
            {"Sharpe Ratio", "4.7"},
            {"Sortino Ratio", "14.706"},
            {"Probabilistic Sharpe Ratio", "67.449%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.618"},
            {"Beta", "-0.348"},
            {"Annual Standard Deviation", "0.1"},
            {"Annual Variance", "0.01"},
            {"Information Ratio", "0.41"},
            {"Tracking Error", "0.127"},
            {"Treynor Ratio", "-1.358"},
            {"Total Fees", "$7.02"},
            {"Estimated Strategy Capacity", "$350000000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "13.71%"},
            {"OrderListHash", "908b83ae2348045279404245da2c80fb"}
        };
    }
}
