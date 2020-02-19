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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class MeanVarianceOptimizationFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private IEnumerable<Symbol> _symbols = (new[] { "AIG", "BAC", "IBM", "SPY" }).Select(s => QuantConnect.Symbol.Create(s, SecurityType.Equity, Market.USA));

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Minute;

            Settings.RebalancePortfolioOnInsightChanges = false;
            Settings.RebalancePortfolioOnSecurityChanges = false;

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

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"{orderEvent}");
            }
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
            {"Total Trades", "13"},
            {"Average Win", "0.13%"},
            {"Average Loss", "-0.69%"},
            {"Compounding Annual Return", "574.998%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "-0.403"},
            {"Net Profit", "2.650%"},
            {"Sharpe Ratio", "7.204"},
            {"Probabilistic Sharpe Ratio", "75.346%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.19"},
            {"Alpha", "0.649"},
            {"Beta", "0.809"},
            {"Annual Standard Deviation", "0.185"},
            {"Annual Variance", "0.034"},
            {"Information Ratio", "4.551"},
            {"Tracking Error", "0.107"},
            {"Treynor Ratio", "1.652"},
            {"Total Fees", "$25.43"},
            {"Fitness Score", "0.68"},
            {"Kelly Criterion Estimate", "13.755"},
            {"Kelly Criterion Probability Value", "0.225"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "478.325"},
            {"Portfolio Turnover", "0.68"},
            {"Total Insights Generated", "17"},
            {"Total Insights Closed", "14"},
            {"Total Insights Analysis Completed", "14"},
            {"Long Insight Count", "6"},
            {"Short Insight Count", "7"},
            {"Long/Short Ratio", "85.71%"},
            {"Estimated Monthly Alpha Value", "$46431.9340"},
            {"Total Accumulated Estimated Alpha Value", "$7996.6108"},
            {"Mean Population Estimated Insight Value", "$571.1865"},
            {"Mean Population Direction", "50%"},
            {"Mean Population Magnitude", "50%"},
            {"Rolling Averaged Population Direction", "12.6429%"},
            {"Rolling Averaged Population Magnitude", "12.6429%"}
        };
    }
}
