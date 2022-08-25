/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http, //www.apache.org/licenses/LICENSE-2.0
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
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;

namespace QuantConnect.DataLibrary.Tests
{
    /// <summary>
    /// Example algorithm of using RiskParityPortfolioConstructionModel
    /// </summary>
    public class RiskParityPortfolioAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2021, 2, 21);
            SetEndDate(2021, 3, 31);
            SetCash(100000);
            SetSecurityInitializer(security => security.SetMarketPrice(GetLastKnownPrice(security)));

            AddEquity("SPY", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);
            
            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new RiskParityPortfolioConstructionModel());
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
        public long DataPoints => 605;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 867;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "43"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "0.660%"},
            {"Drawdown", "5.500%"},
            {"Expectancy", "0.274"},
            {"Net Profit", "0.993%"},
            {"Sharpe Ratio", "0.11"},
            {"Probabilistic Sharpe Ratio", "9.408%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.55"},
            {"Alpha", "-0.006"},
            {"Beta", "1.277"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-0.128"},
            {"Tracking Error", "0.028"},
            {"Treynor Ratio", "0.005"},
            {"Total Fees", "$43.98"},
            {"Estimated Strategy Capacity", "$630000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0.001"},
            {"Kelly Criterion Estimate", "-5609406551260240000"},
            {"Kelly Criterion Probability Value", "0.5"},
            {"Sortino Ratio", "0.044"},
            {"Return Over Maximum Drawdown", "0.119"},
            {"Portfolio Turnover", "0.002"},
            {"Total Insights Generated", "758"},
            {"Total Insights Closed", "756"},
            {"Total Insights Analysis Completed", "756"},
            {"Long Insight Count", "758"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "-$99650.9973"},
            {"Total Accumulated Estimated Alpha Value", "-$1820845.1678"},
            {"Mean Population Estimated Insight Value", "-$2408.5254"},
            {"Mean Population Direction", "2.5132%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "b6dca94ebb3d821f72457389a7cac298"}
        };

    }
}
