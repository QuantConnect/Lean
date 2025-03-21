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
    /// Framework algorithm that uses the <see cref="PearsonCorrelationPairsTradingAlphaModel"/>.
    /// This model extendes <see cref="BasePairsTradingAlphaModel"/> and uses Pearson correlation
    /// to rank the pairs trading candidates and use the best candidate to trade.
    /// </summary>
    public class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            SetAlpha(new PearsonCorrelationPairsTradingAlphaModel(252, Resolution.Daily));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        public override void OnEndOfAlgorithm()
        {
            // We have removed all securities from the universe. The Alpha Model should remove the consolidator
            var consolidatorCount = SubscriptionManager.Subscriptions.Sum(s => s.Consolidators.Count);
            // Expect 2 consolidators for SPY and AIG, which where manually added at the start
            if (consolidatorCount != 2)
            {
                throw new RegressionTestException($"The number of consolidator is should be 2. Actual: {consolidatorCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 14089;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1512;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "6"},
            {"Average Win", "0.82%"},
            {"Average Loss", "-0.39%"},
            {"Compounding Annual Return", "43.360%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0.546"},
            {"Start Equity", "100000"},
            {"End Equity", "100494.63"},
            {"Net Profit", "0.495%"},
            {"Sharpe Ratio", "10.477"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "85.907%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "2.09"},
            {"Alpha", "0.227"},
            {"Beta", "0.066"},
            {"Annual Standard Deviation", "0.034"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-7.7"},
            {"Tracking Error", "0.21"},
            {"Treynor Ratio", "5.41"},
            {"Total Fees", "$25.78"},
            {"Estimated Strategy Capacity", "$3300000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "60.55%"},
            {"OrderListHash", "0eb251234d0fa772130bb341457091b4"}
        };
    }
}
