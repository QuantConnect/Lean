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

        private protected IEnumerable<Symbol> ETFConstituentsFilter(IEnumerable<ETFConstituentUniverse> constituents)
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
        public long DataPoints => 1072;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "9"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "250.805%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "102436.17"},
            {"Net Profit", "2.436%"},
            {"Sharpe Ratio", "3.837"},
            {"Sortino Ratio", "10.614"},
            {"Probabilistic Sharpe Ratio", "63.620%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.5"},
            {"Beta", "-0.357"},
            {"Annual Standard Deviation", "0.091"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "-0.581"},
            {"Tracking Error", "0.12"},
            {"Treynor Ratio", "-0.981"},
            {"Total Fees", "$9.05"},
            {"Estimated Strategy Capacity", "$400000000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "14.29%"},
            {"OrderListHash", "a11cb12dabe993c7989036e299f3f028"}
        };
    }
}
