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
using System.Reflection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests In The Money (ITM) index option calls across different strike prices.
    /// We expect 4* orders from the algorithm, which are:
    ///
    ///   * (1) Initial entry, buy SPX Call Option (SPXF21 expiring ITM)
    ///   * (2) Initial entry, sell SPX Call Option at different strike (SPXF21 expiring ITM)
    ///   * [2] Option assignment, settle into cash
    ///   * [1] Option exercise, settle into cash
    ///
    /// Additionally, we test delistings for index options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    ///
    /// * Assignments are counted as orders
    /// </summary>
    public class IndexOptionBuySellCallIntradayRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 31);

            var spx = AddIndex("SPX", Resolution.Minute).Symbol;

            // Select a index option expiring ITM, and adds it to the algorithm.
            var spxOptions = OptionChain(spx)
                .Where(x => (x.ID.StrikePrice == 3700m || x.ID.StrikePrice == 3800m) && x.ID.OptionRight == OptionRight.Call && x.ID.Date.Year == 2021 && x.ID.Date.Month == 1)
                .Select(x => AddIndexOptionContract(x, Resolution.Minute).Symbol)
                .OrderBy(x => x.ID.StrikePrice)
                .ToList();

            var expectedContract3700 = QuantConnect.Symbol.CreateOption(
                spx,
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3700m,
                new DateTime(2021, 1, 15));

            var expectedContract3800 = QuantConnect.Symbol.CreateOption(
                spx,
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3800m,
                new DateTime(2021, 1, 15));

            if (spxOptions.Count != 2)
            {
                throw new RegressionTestException($"Expected 2 index options symbols from chain provider, found {spxOptions.Count}");
            }

            if (spxOptions[0] != expectedContract3700)
            {
                throw new RegressionTestException($"Contract {expectedContract3700} was not found in the chain, found instead: {spxOptions[0]}");
            }
            if (spxOptions[1] != expectedContract3800)
            {
                throw new RegressionTestException($"Contract {expectedContract3800} was not found in the chain, found instead: {spxOptions[1]}");
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(spx, 1), () =>
            {
                MarketOrder(spxOptions[0], 1);
                MarketOrder(spxOptions[1], -1);
            });
            Schedule.On(DateRules.Tomorrow, TimeRules.Noon, () =>
            {
                Liquidate();
            });
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="RegressionTestException">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new RegressionTestException($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
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
        public long DataPoints => 32144;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "2.46%"},
            {"Average Loss", "-2.58%"},
            {"Compounding Annual Return", "-2.473%"},
            {"Drawdown", "0.900%"},
            {"Expectancy", "0.956"},
            {"Start Equity", "100000"},
            {"End Equity", "99824"},
            {"Net Profit", "-0.176%"},
            {"Sharpe Ratio", "-0.672"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "29.734%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0.96"},
            {"Alpha", "-0.02"},
            {"Beta", "0.014"},
            {"Annual Standard Deviation", "0.029"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-0.448"},
            {"Tracking Error", "0.139"},
            {"Treynor Ratio", "-1.397"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P3HB9YI6|SPX 31"},
            {"Portfolio Turnover", "0.56%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "de57068f577da644fba18d85710fcea2"}
        };
    }
}

