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
    /// This regression algorithm tests In The Money (ITM) future option calls across different strike prices.
    /// We expect 6 orders from the algorithm, which are:
    ///
    ///   * (1) Initial entry, buy ES Call Option (ES19H21 expiring ITM)
    ///   * (2) Initial entry, sell ES Call Option at different strike (ES18Z20 expiring ITM)
    ///   * [2] Option assignment, opens a position in the underlying (ES18Z20, Qty: -1)
    ///   * [2] Future contract liquidation, due to impending expiry
    ///   * [1] Option exercise, receive 1 ES19H21 future contract
    ///   * [1] Liquidate ES19H21 contract, due to expiry
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionBuySellCallIntradayRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2020, 3, 1);
            typeof(QCAlgorithm)
                .GetField("_endDate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, new DateTime(2021, 3, 30));

            var start = new DateTime(2020, 9, 22);

            // We add AAPL as a temporary workaround for https://github.com/QuantConnect/Lean/issues/4872
            // which causes delisting events to never be processed, thus leading to options that might never
            // be exercised until the next data point arrives.
            AddEquity("AAPL", Resolution.Daily);

            var es18z20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 12, 18)),
                Resolution.Minute).Symbol;

            var es19h21 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2021, 3, 19)),
                Resolution.Minute).Symbol;

            // Select a future option expiring ITM, and adds it to the algorithm.
            var esOptions = OptionChainProvider.GetOptionContractList(es19h21, start)
                .Concat(OptionChainProvider.GetOptionContractList(es18z20, start))
                .Where(x => x.ID.StrikePrice == 3250m && x.ID.OptionRight == OptionRight.Call)
                .Select(x => AddFutureOptionContract(x, Resolution.Minute).Symbol)
                .ToList();

            var expectedContracts = new[]
            {
                QuantConnect.Symbol.CreateOption(es19h21, Market.CME, OptionStyle.American, OptionRight.Call, 3250m,
                    new DateTime(2021, 3, 19)),
                QuantConnect.Symbol.CreateOption(es18z20, Market.CME, OptionStyle.American, OptionRight.Call, 3250m,
                    new DateTime(2020, 12, 18))
            };

            foreach (var esOption in esOptions)
            {
                if (!expectedContracts.Contains(esOption))
                {
                    throw new Exception($"Contract {esOption} was not found in the chain");
                }
            }

            Schedule.On(DateRules.On(start), TimeRules.AfterMarketOpen(es19h21, 1), () =>
            {
                MarketOrder(esOptions[0], 1);
                MarketOrder(esOptions[1], -1);
            });
            Schedule.On(DateRules.On(start), TimeRules.Noon, () =>
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
            {"Total Trades", "6"},
            {"Average Win", "6.28%"},
            {"Average Loss", "-7.43%"},
            {"Compounding Annual Return", "-3.371%"},
            {"Drawdown", "10.300%"},
            {"Expectancy", "-0.078"},
            {"Net Profit", "-3.634%"},
            {"Sharpe Ratio", "-0.191"},
            {"Probabilistic Sharpe Ratio", "7.956%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.84"},
            {"Alpha", "-0.022"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.112"},
            {"Annual Variance", "0.013"},
            {"Information Ratio", "0.208"},
            {"Tracking Error", "0.147"},
            {"Treynor Ratio", "41.964"},
            {"Total Fees", "$14.80"},
            {"Fitness Score", "0.009"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.029"},
            {"Return Over Maximum Drawdown", "-0.326"},
            {"Portfolio Turnover", "0.02"},
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
            {"OrderListHash", "555229764"}
        };
    }
}

