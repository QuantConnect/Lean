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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm used to test a fine and coarse selection methods
    /// returning <see cref="Universe.Unchanged"/>
    /// </summary>
    public class UniverseUnchangedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbolsFine = 2;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 04, 07);

            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
        }

        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            // the first and second selection
            if (Time.Date <= new DateTime(2014, 3, 26))
            {
                return new List<Symbol>
                {
                    QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                    QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                    QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA)
                };
            }
            // will skip fine selection
            return Universe.Unchanged;
        }

        public IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            // just the first selection
            if (Time.Date == new DateTime(2014, 3, 25))
            {
                var sortedByPeRatio = fine.OrderByDescending(x => x.ValuationRatios.PERatio);
                var topFine = sortedByPeRatio.Take(NumberOfSymbolsFine);
                return topFine.Select(x => x.Symbol);
            }
            // the second selection will return unchanged, in the following fine selection will be skipped
            return Universe.Unchanged;
        }

        // assert security changes, throw if called more than once
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Count != 2
                || Time != new DateTime(2014, 3, 25)
                || changes.AddedSecurities.All(security => security.Symbol != QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA))
                || changes.AddedSecurities.All(security => security.Symbol != QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA)))
            {
                throw new RegressionTestException("Unexpected security changes");
            }
            Log($"OnSecuritiesChanged({Time:o}):: {changes}");
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
        public long DataPoints => 63891;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-45.405%"},
            {"Drawdown", "2.300%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "97705.34"},
            {"Net Profit", "-2.295%"},
            {"Sharpe Ratio", "-3.77"},
            {"Sortino Ratio", "-4.881"},
            {"Probabilistic Sharpe Ratio", "10.598%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.233"},
            {"Beta", "0.705"},
            {"Annual Standard Deviation", "0.097"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "-2.374"},
            {"Tracking Error", "0.075"},
            {"Treynor Ratio", "-0.52"},
            {"Total Fees", "$19.98"},
            {"Estimated Strategy Capacity", "$100000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "7.33%"},
            {"OrderListHash", "62cae7349a5294699d7d71ac4ec42b09"}
        };
    }
}
