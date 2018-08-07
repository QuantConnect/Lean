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
using QuantConnect.Interfaces;
using System.Linq;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class MeanVarianceOptimizationFrameworkAlgorithm : QCAlgorithmFramework, IRegressionAlgorithmDefinition
    {
        private IEnumerable<Symbol> _symbols = (new string[] { "AIG", "BAC", "IBM", "SPY" }).Select(s => QuantConnect.Symbol.Create(s, SecurityType.Equity, Market.USA));

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.

            // set algorithm framework models
            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
            SetAlpha(new HistoricalReturnsAlphaModel(resolution: Resolution.Daily));
            SetPortfolioConstruction(new MeanVarianceOptimizationPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public IEnumerable<Symbol> CoarseSelector(IEnumerable<CoarseFundamental> coarse)
        {
            int last = Time.Day > 8 ? 3 : _symbols.Count();
            return _symbols.Take(last);
        }

        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "10"},
            {"Average Win", "0.50%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "573.292%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "1.248"},
            {"Net Profit", "2.647%"},
            {"Sharpe Ratio", "8.544"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "3.50"},
            {"Alpha", "0"},
            {"Beta", "96.224"},
            {"Annual Standard Deviation", "0.129"},
            {"Annual Variance", "0.017"},
            {"Information Ratio", "8.462"},
            {"Tracking Error", "0.129"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$23.04"},
            {"Total Insights Generated", "14"},
            {"Total Insights Closed", "11"},
            {"Total Insights Analysis Completed", "11"},
            {"Long Insight Count", "6"},
            {"Short Insight Count", "4"},
            {"Long/Short Ratio", "150.0%"},
            {"Estimated Monthly Alpha Value", "$-85612.32"},
            {"Total Accumulated Estimated Alpha Value", "$-14744.34"},
            {"Mean Population Estimated Insight Value", "$-1340.395"},
            {"Mean Population Direction", "27.2727%"},
            {"Mean Population Magnitude", "27.2727%"},
            {"Rolling Averaged Population Direction", "5.8237%"},
            {"Rolling Averaged Population Magnitude", "5.8237%"}
        };
    }
}
