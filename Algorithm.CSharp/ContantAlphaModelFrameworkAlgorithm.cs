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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Framework algorithm that uses the <see cref="ConstantAlphaModel"/>.
    /// </summary>
    public class ConstantAlphaModelFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            var symbols = new[] { "SPY", "AIG", "BAC", "IBM" }
                .Select(ticker => QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA))
                .ToList();

            // Manually add SPY and AIG when the algorithm starts
            SetUniverseSelection(new ManualUniverseSelectionModel(symbols.Take(2)));

            // At midnight, add all securities every day except on the last data
            // With this procedure, the Alpha Model will experience multiple universe changes
            AddUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(), TimeRules.Midnight,
                dt => dt < EndDate.AddDays(-1) ? symbols : Enumerable.Empty<Symbol>()));

            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public override void OnEndOfAlgorithm()
        {
            // We have removed all securities from the universe. The Alpha Model should remove the consolidator
            var consolidatorCount = SubscriptionManager.Subscriptions.Sum(s => s.Consolidators.Count);
            if (consolidatorCount > 0)
            {
                throw new Exception($"The number of consolidator is should be zero. Actual: {consolidatorCount}");
            }
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
        public long DataPoints => 15649;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "20"},
            {"Average Win", "0.36%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "181.661%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "62.938"},
            {"Net Profit", "1.429%"},
            {"Sharpe Ratio", "7.952"},
            {"Probabilistic Sharpe Ratio", "64.640%"},
            {"Loss Rate", "60%"},
            {"Win Rate", "40%"},
            {"Profit-Loss Ratio", "158.84"},
            {"Alpha", "-0.132"},
            {"Beta", "1.169"},
            {"Annual Standard Deviation", "0.275"},
            {"Annual Variance", "0.076"},
            {"Information Ratio", "2.112"},
            {"Tracking Error", "0.096"},
            {"Treynor Ratio", "1.87"},
            {"Total Fees", "$42.24"},
            {"Estimated Strategy Capacity", "$5400000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "40.16%"},
            {"OrderListHash", "3d917da3d75a5a1a3f9532ab93dd37cf"}
        };
    }
}
