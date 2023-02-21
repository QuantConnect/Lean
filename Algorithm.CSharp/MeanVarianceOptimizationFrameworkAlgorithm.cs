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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14082;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 256;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "13"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.26%"},
            {"Compounding Annual Return", "507.566%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "-0.326"},
            {"Net Profit", "2.502%"},
            {"Sharpe Ratio", "13.438"},
            {"Probabilistic Sharpe Ratio", "68.675%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "0.01"},
            {"Alpha", "1.408"},
            {"Beta", "1.254"},
            {"Annual Standard Deviation", "0.29"},
            {"Annual Variance", "0.084"},
            {"Information Ratio", "19.833"},
            {"Tracking Error", "0.096"},
            {"Treynor Ratio", "3.105"},
            {"Total Fees", "$26.57"},
            {"Estimated Strategy Capacity", "$4900000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Fitness Score", "0.303"},
            {"Kelly Criterion Estimate", "13.787"},
            {"Kelly Criterion Probability Value", "0.231"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "316.732"},
            {"Portfolio Turnover", "0.303"},
            {"Total Insights Generated", "13"},
            {"Total Insights Closed", "10"},
            {"Total Insights Analysis Completed", "10"},
            {"Long Insight Count", "6"},
            {"Short Insight Count", "7"},
            {"Long/Short Ratio", "85.71%"},
            {"Estimated Monthly Alpha Value", "$52003.0716"},
            {"Total Accumulated Estimated Alpha Value", "$8956.0846"},
            {"Mean Population Estimated Insight Value", "$895.6085"},
            {"Mean Population Direction", "70%"},
            {"Mean Population Magnitude", "70%"},
            {"Rolling Averaged Population Direction", "94.5154%"},
            {"Rolling Averaged Population Magnitude", "94.5154%"},
            {"OrderListHash", "a5d805772046067fa17715aeb03d2d52"}
        };
    }
}
