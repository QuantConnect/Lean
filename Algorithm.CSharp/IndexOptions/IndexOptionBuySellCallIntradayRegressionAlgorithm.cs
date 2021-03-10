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
            var spxOptions = OptionChainProvider.GetOptionContractList(spx, Time)
                .Where(x => (x.ID.StrikePrice == 3700m || x.ID.StrikePrice == 3800m) && x.ID.OptionRight == OptionRight.Call && x.ID.Date.Year == 2021 && x.ID.Date.Month == 1)
                .Select(x => AddIndexOptionContract(x, Resolution.Minute).Symbol)
                .OrderBy(x => x.ID.StrikePrice)
                .ToList();

            var expectedContract3700 = QuantConnect.Symbol.CreateOption(
                spx,
                Market.CME,
                OptionStyle.European,
                OptionRight.Call,
                3700m,
                new DateTime(2021, 1, 15));

            var expectedContract3800 = QuantConnect.Symbol.CreateOption(
                spx,
                Market.CME,
                OptionStyle.European,
                OptionRight.Call,
                3800m,
                new DateTime(2021, 1, 15));

            if (spxOptions.Count != 2)
            {
                throw new Exception($"Expected 2 index options symbols from chain provider, found {spxOptions.Count}");
            }

            if (spxOptions[0] != expectedContract3700)
            {
                Log($"{spxOptions[0].ID.Market}\n{spxOptions[0].ID.OptionStyle}\n{spxOptions[0].ID.OptionRight}\n{spxOptions[0].ID.StrikePrice}\n{spxOptions[0].ID.Date}");
                throw new Exception($"Contract {expectedContract3700} was not found in the chain, found instead: {spxOptions[0]}");
            }
            if (spxOptions[1] != expectedContract3800)
            {
                Log($"{spxOptions[1].ID.Market}\n{spxOptions[1].ID.OptionStyle}\n{spxOptions[1].ID.OptionRight}\n{spxOptions[1].ID.StrikePrice}\n{spxOptions[1].ID.Date}");
                throw new Exception($"Contract {expectedContract3800} was not found in the chain, found instead: {spxOptions[1]}");
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
        /// <exception cref="Exception">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "2.93%"},
            {"Average Loss", "-4.15%"},
            {"Compounding Annual Return", "-6.023%"},
            {"Drawdown", "5.700%"},
            {"Expectancy", "-0.148"},
            {"Net Profit", "-2.802%"},
            {"Sharpe Ratio", "-0.501"},
            {"Probabilistic Sharpe Ratio", "10.679%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.70"},
            {"Alpha", "-0.045"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.089"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "0.966"},
            {"Tracking Error", "0.195"},
            {"Treynor Ratio", "55.977"},
            {"Total Fees", "$14.80"},
            {"Estimated Strategy Capacity", "$2100000.00"},
            {"Fitness Score", "0.018"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.103"},
            {"Return Over Maximum Drawdown", "-1.063"},
            {"Portfolio Turnover", "0.045"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "fc9eb9b0a644e4890d5ec3d40367d0e1"}
        };
    }
}

