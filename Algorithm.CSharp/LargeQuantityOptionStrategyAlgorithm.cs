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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that orders for option strategies can be placed with large quantities as long as there is margin available.
    /// This asserts the expected behavior in GH issue #5693
    /// </summary>
    public class LargeQuantityOptionStrategyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            SetSecurityInitializer(x => x.SetMarketPrice(GetLastKnownPrice(x)));

            var equity = AddEquity("GOOG");
            var option = AddOption("GOOG");
            _optionSymbol = option.Symbol;

            option.SetFilter(-2, +2, 0, 180);
        }

        public override void OnData(Slice slice)
        {
            if (Portfolio.Invested || !slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                return;
            }

            var putContractsWithLatestExpiry = chain
                    // puts only
                    .Where(x => x.Right == OptionRight.Put)
                    // contracts with latest expiry
                    .GroupBy(x => x.Expiry)
                    .OrderBy(x => x.Key)
                    .Last()
                    // ordered by strike
                    .OrderBy(x => x.Strike)
                    .ToList();

            if (putContractsWithLatestExpiry.Count < 2)
            {
                return;
            }

            var longContract = putContractsWithLatestExpiry[0];
            var shortContract = putContractsWithLatestExpiry[1];

            var strategy = OptionStrategies.BearPutSpread(_optionSymbol, shortContract.Strike, longContract.Strike, shortContract.Expiry);

            // Before option strategies orders were place as combo orders, only a quantity up to 18 could be used in this case,
            // even though the remaining margin was enough to support a larger quantity. See GH issue #5693.
            // We want to assert that with combo orders, large quantities can be used on option strategies
            Order(strategy, 19);

            Quit($"Margin used: {Portfolio.TotalMarginUsed}; Remaining: {Portfolio.MarginRemaining}");
        }

        public override void OnEndOfAlgorithm()
        {
            var filledOrders = Transactions.GetOrders(x => x.Status == OrderStatus.Filled).ToList();

            if (filledOrders.Count != 2)
            {
                throw new Exception($"Expected 2 filled orders but found {filledOrders.Count}");
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
        public long DataPoints => 2262;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 25;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "95130.3"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$24.70"},
            {"Estimated Strategy Capacity", "$6000.00"},
            {"Lowest Capacity Asset", "GOOCV 30AKMELSHQVZA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "208.51%"},
            {"OrderListHash", "80f3cfbffc903339387a788a4d35dad1"}
        };
    }
}
