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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    public class BlackLittermanPortfolioOptimizationFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            var optimizer = new UnconstrainedMeanVariancePortfolioOptimizer();

            // set algorithm framework models
            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(CoarseSelector));
            SetAlpha(new HistoricalReturnsAlphaModel(resolution: Resolution.Daily));
            SetPortfolioConstruction(new BlackLittermanOptimizationPortfolioConstructionModel(optimizer: optimizer));
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
            {"Total Trades", "18"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.16%"},
            {"Compounding Annual Return", "72.164%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.747%"},
            {"Sharpe Ratio", "4.086"},
            {"Probabilistic Sharpe Ratio", "61.091%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.305"},
            {"Beta", "0.564"},
            {"Annual Standard Deviation", "0.113"},
            {"Annual Variance", "0.013"},
            {"Information Ratio", "-10.007"},
            {"Tracking Error", "0.09"},
            {"Treynor Ratio", "0.82"},
            {"Total Fees", "$41.70"},
            {"Fitness Score", "0.634"},
            {"Kelly Criterion Estimate", "13.656"},
            {"Kelly Criterion Probability Value", "0.228"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "80.05"},
            {"Portfolio Turnover", "0.634"},
            {"Total Insights Generated", "17"},
            {"Total Insights Closed", "14"},
            {"Total Insights Analysis Completed", "14"},
            {"Long Insight Count", "6"},
            {"Short Insight Count", "7"},
            {"Long/Short Ratio", "85.71%"},
            {"Estimated Monthly Alpha Value", "$72447.6813"},
            {"Total Accumulated Estimated Alpha Value", "$12477.1007"},
            {"Mean Population Estimated Insight Value", "$891.2215"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "50%"},
            {"Rolling Averaged Population Direction", "12.6429%"},
            {"Rolling Averaged Population Magnitude", "12.6429%"},
            {"OrderListHash", "3edd51956c7c97af4863aa6059c11f1a"}
        };
    }
}
