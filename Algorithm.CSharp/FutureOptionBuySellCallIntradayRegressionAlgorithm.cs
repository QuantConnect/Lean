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
    ///   * (1) Initial entry, buy ES Call Option (ES19M20 expiring ITM)
    ///   * (2) Initial entry, sell ES Call Option at different strike (ES20H20 expiring ITM)
    ///   * [2] Option assignment, opens a position in the underlying (ES20H20, Qty: -1)
    ///   * [2] Future contract liquidation, due to impending expiry
    ///   * [1] Option exercise, receive 1 ES19M20 future contract
    ///   * [1] Liquidate ES19M20 contract, due to expiry
    ///
    /// Additionally, we test delistings for future options and assert that our
    /// portfolio holdings reflect the orders the algorithm has submitted.
    /// </summary>
    public class FutureOptionBuySellCallIntradayRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 6, 30);

            var es20h20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 3, 20)),
                Resolution.Minute).Symbol;

            var es20m20 = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Indices.SP500EMini,
                    Market.CME,
                    new DateTime(2020, 6, 19)),
                Resolution.Minute).Symbol;

            // Select a future option expiring ITM, and adds it to the algorithm.
            var esOptions = OptionChainProvider.GetOptionContractList(es20m20, Time)
                .Concat(OptionChainProvider.GetOptionContractList(es20h20, Time))
                .Where(x => x.ID.StrikePrice == 3200m && x.ID.OptionRight == OptionRight.Call)
                .Select(x => AddFutureOptionContract(x, Resolution.Minute).Symbol)
                .ToList();

            var expectedContracts = new[]
            {
                QuantConnect.Symbol.CreateOption(es20h20, Market.CME, OptionStyle.American, OptionRight.Call, 3200m,
                    new DateTime(2020, 3, 20)),
                QuantConnect.Symbol.CreateOption(es20m20, Market.CME, OptionStyle.American, OptionRight.Call, 3200m,
                    new DateTime(2020, 6, 19))
            };

            foreach (var esOption in esOptions)
            {
                if (!expectedContracts.Contains(esOption))
                {
                    throw new Exception($"Contract {esOption} was not found in the chain");
                }
            }

            Schedule.On(DateRules.Tomorrow, TimeRules.AfterMarketOpen(es20m20, 1), () =>
            {
                MarketOrder(esOptions[0], 1);
                MarketOrder(esOptions[1], -1);
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
            {"Total Trades", "6"},
            {"Average Win", "2.94%"},
            {"Average Loss", "-4.15%"},
            {"Compounding Annual Return", "-5.589%"},
            {"Drawdown", "5.600%"},
            {"Expectancy", "-0.145"},
            {"Net Profit", "-2.760%"},
            {"Sharpe Ratio", "-0.45"},
            {"Probabilistic Sharpe Ratio", "9.306%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.71"},
            {"Alpha", "-0.036"},
            {"Beta", "-0.012"},
            {"Annual Standard Deviation", "0.08"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-0.149"},
            {"Tracking Error", "0.387"},
            {"Treynor Ratio", "2.943"},
            {"Total Fees", "$3.70"},
            {"Estimated Strategy Capacity", "$280000000.00"},
            {"Lowest Capacity Asset", "ES XFH59UPBIJ7O|ES XFH59UK0MYO1"},
            {"Fitness Score", "0.017"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.096"},
            {"Return Over Maximum Drawdown", "-0.993"},
            {"Portfolio Turnover", "0.043"},
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
            {"OrderListHash", "18f8a17034aa12be40581baecca96788"}
        };
    }
}

