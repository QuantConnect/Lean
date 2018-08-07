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

using System.Linq;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class BlackLittermanPortfolioOptimizationFrameworkAlgorithm : QCAlgorithmFramework, IRegressionAlgorithmDefinition
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
            SetPortfolioConstruction(new BlackLittermanOptimizationPortfolioConstructionModel());
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
            {"Total Trades", "11"},
            {"Average Win", "0.12%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1366.273%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "0"},
            {"Net Profit", "3.747%"},
            {"Sharpe Ratio", "9.273"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "135.779"},
            {"Annual Standard Deviation", "0.168"},
            {"Annual Variance", "0.028"},
            {"Information Ratio", "9.21"},
            {"Tracking Error", "0.168"},
            {"Treynor Ratio", "0.011"},
            {"Total Fees", "$23.61"},
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
