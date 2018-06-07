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

using System.Collections.Generic;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Framework algorithm that uses the <see cref="PairsTradingAlphaModel"/> to detect
    /// divergences between correllated assets. Detection of asset correlation is not
    /// performed and is expected to be handled outside of the alpha model.
    /// </summary>
    public class PairsTradingAlphaModelFrameworkAlgorithm : QCAlgorithmFramework, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var bac = AddEquity("BAC");
            var aig = AddEquity("AIG");

            SetUniverseSelection(new ManualUniverseSelectionModel(Securities.Keys));
            SetAlpha(new PairsTradingAlphaModel(bac.Symbol, aig.Symbol));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "2.18%"},
            {"Average Loss", "-1.38%"},
            {"Compounding Annual Return", "75.075%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0.288"},
            {"Net Profit", "0.719%"},
            {"Sharpe Ratio", "6.982"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.58"},
            {"Alpha", "0"},
            {"Beta", "32.812"},
            {"Annual Standard Deviation", "0.052"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "6.782"},
            {"Tracking Error", "0.052"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$74.09"},
            {"Total Insights Generated", "4"},
            {"Total Insights Closed", "4"},
            {"Total Insights Analysis Completed", "4"},
            {"Long Insight Count", "2"},
            {"Short Insight Count", "2"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-1148.429"},
            {"Total Accumulated Estimated Alpha Value", "$-185.0247"},
            {"Mean Population Estimated Insight Value", "$-46.25617"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "3.8827%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
