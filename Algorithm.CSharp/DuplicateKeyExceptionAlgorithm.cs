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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using System;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstrate old duplicate key exception when combining QC500 and MeanVarianceOptimizationPortfolioConstructionModel
    /// </summary>
    public class DuplicateKeyExceptionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 11);
            SetCash(25000);

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse(Universe.Index.QC500);
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1), .1D, .1D));
            SetPortfolioConstruction(new MeanVarianceOptimizationPortfolioConstructionModel());
        }

        public bool CanRunLocally => false;

        public Language[] Languages { get; } = {Language.CSharp};

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "282"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-35.074%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.590%"},
            {"Sharpe Ratio", "-1.743"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "-20.891"},
            {"Annual Standard Deviation", "0.138"},
            {"Annual Variance", "0.019"},
            {"Information Ratio", "-1.826"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "0.012"},
            {"Total Fees", "$282.00"},
            {"Fitness Score", "0.011"},
            {"Total Insights Generated", "2500"},
            {"Total Insights Closed", "1500"},
            {"Total Insights Analysis Completed", "1500"},
            {"Long Insight Count", "2500"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$33851661.10343"},
            {"Total Accumulated Estimated Alpha Value", "$5830008.3011"},
            {"Mean Population Estimated Insight Value", "$3886.6722"},
            {"Mean Population Direction", "51%"},
            {"Mean Population Magnitude", "8.9355%"},
            {"Rolling Averaged Population Direction", "96.2016%"},
            {"Rolling Averaged Population Magnitude", "23.3226%"}
        };
    }
}