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
    /// Framework algorithm that uses the <see cref="EmaCrossAlphaModel"/>.
    /// </summary>
    public class EmaCrossAlphaModelFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            SetAlpha(new EmaCrossAlphaModel());
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
        public long DataPoints => 14869;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 152;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "12"},
            {"Average Win", "0.37%"},
            {"Average Loss", "-0.15%"},
            {"Compounding Annual Return", "0.081%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0.001"},
            {"Net Profit", "0.001%"},
            {"Sharpe Ratio", "1.25"},
            {"Probabilistic Sharpe Ratio", "50.694%"},
            {"Loss Rate", "71%"},
            {"Win Rate", "29%"},
            {"Profit-Loss Ratio", "2.50"},
            {"Alpha", "-1.023"},
            {"Beta", "0.607"},
            {"Annual Standard Deviation", "0.144"},
            {"Annual Variance", "0.021"},
            {"Information Ratio", "-18.002"},
            {"Tracking Error", "0.1"},
            {"Treynor Ratio", "0.296"},
            {"Total Fees", "$20.20"},
            {"Estimated Strategy Capacity", "$4800000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "53.84%"},
            {"OrderListHash", "98ea2f4cdd1627b817fdba3f2087284b"}
        };
    }
}
