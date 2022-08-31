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
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm of using ETFConstituentsUniverseSelectionModel
    /// </summary>
    class ETFConstituentsFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 2, 1);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverseSelection(new ETFConstituentsUniverseSelectionModel("SPY"));

            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));

            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
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
        public long DataPoints => 11307;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "805"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-19.852%"},
            {"Drawdown", "3.300%"},
            {"Expectancy", "0.035"},
            {"Net Profit", "-1.862%"},
            {"Sharpe Ratio", "-2.13"},
            {"Probabilistic Sharpe Ratio", "10.434%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "2.11"},
            {"Alpha", "-00145"},
            {"Beta", "0.612"},
            {"Annual Standard Deviation", "0.068"},
            {"Annual Variance", "0.005"},
            {"Information Ratio", "-3.049"},
            {"Tracking Error", "0.048"},
            {"Treynor Ratio", "-0.235"},
            {"Total Fees", "$805.00"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "NWSVV VHJF6S7EZRL1"},
            {"Fitness Score", "0.004"},
            {"Kelly Criterion Estimate", "3.787"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "-2.588"},
            {"Return Over Maximum Drawdown", "-6.087"},
            {"Portfolio Turnover", "0.034"},
            {"Total Insights Generated", "12089"},
            {"Total Insights Closed", "11083"},
            {"Total Insights Analysis Completed", "11083"},
            {"Long Insight Count", "12089"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$33132939.6302"},
            {"Total Accumulated Estimated Alpha Value", "$34467460.8097"},
            {"Mean Population Estimated Insight Value", "$3109.9396"},
            {"Mean Population Direction", "44.8525%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "55.6756%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1a86bc38078d24a9f5616d807ab66413"}
        };
    }
}
