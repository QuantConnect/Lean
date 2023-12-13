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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Custom.AlphaStreams;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsUniverseSelectionTemplateAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 04, 04);
            SetEndDate(2018, 04, 06);

            SetAlpha(new AlphaStreamAlphaModule());
            SetExecution(new ImmediateExecutionModel());
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;
            SetPortfolioConstruction(new EqualWeightingAlphaStreamsPortfolioConstructionModel());

            SetUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(),
                TimeRules.Midnight,
                SelectAlphas,
                new UniverseSettings(UniverseSettings)
                {
                    SubscriptionDataTypes = new List<Tuple<Type, TickType>>
                        {new(typeof(AlphaStreamsPortfolioState), TickType.Trade)},
                    FillForward = false,
                }
            ));
        }

        private IEnumerable<Symbol> SelectAlphas(DateTime dateTime)
        {
            Log($"SelectAlphas() {Time}");
            foreach (var alphaId in new[] {"623b06b231eb1cc1aa3643a46", "9fc8ef73792331b11dbd5429a"})
            {
                var alphaSymbol = new Symbol(SecurityIdentifier.GenerateBase(typeof(AlphaStreamsPortfolioState), alphaId, Market.USA),
                        alphaId);

                yield return alphaSymbol;
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
        public long DataPoints => 893;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 2;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.12%"},
            {"Compounding Annual Return", "-13.200%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.116%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "2.474"},
            {"Tracking Error", "0.339"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$83000.00"},
            {"Lowest Capacity Asset", "BTCUSD XJ"},
            {"Portfolio Turnover", "2.31%"},
            {"OrderListHash", "6912c537884a8c66542f24a2e4e2e6ec"}
        };
    }
}
