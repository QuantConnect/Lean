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
using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm of filtering with Put Butterfly option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionStrategyFilteringUniversePutButterflyRegressionAlgorithm : OptionStrategyFilteringUniverseBaseAlgorithm
    {
        public override void Initialize()
        {
            _func = u => u.IncludeWeeklys().PutButterfly(28, 5);
            _expectedCount = 3;

            base.Initialize();
        }

        protected override void TestFiltering(OptionChain chain)
        {
            var count = chain.Count();
            if (count != _expectedCount)
            {
                throw new Exception($"Number of contract returned does not match expectation, {count}, {_expectedCount}");
            }

            var right = OptionRight.Put;
            var otmStrike = 742.50m;
            var atmStrike = 747.50m;
            var itmStrike = 752.50m;
            var expiry = new DateTime(2016, 1, 22);

            var otmContract = chain.SingleOrDefault(x => 
                x.Right == right &&
                x.Strike == otmStrike &&
                x.Expiry == expiry
            );
            var atmContract = chain.SingleOrDefault(x =>
                x.Right == right &&
                x.Strike == atmStrike &&
                x.Expiry == expiry
            );
            var itmContract = chain.SingleOrDefault(x =>
                x.Right == right &&
                x.Strike == itmStrike &&
                x.Expiry == expiry
            );
            if (otmContract == null || atmContract == null || itmContract == null)
            {
                throw new Exception($"No contract returned match condition");
            }

            MarketOrder(otmContract.Symbol, +1);
            MarketOrder(atmContract.Symbol, -2);
            MarketOrder(itmContract.Symbol, +1);

            AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyPut.Name, 1);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 462015;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "199351.7"},
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
            {"Total Fees", "$3.30"},
            {"Estimated Strategy Capacity", "$3400000.00"},
            {"Lowest Capacity Asset", "GOOCV 306JVPQ5YT51I|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "3.43%"},
            {"OrderListHash", "4d232421a55b71dd93e47e25fff3eff7"}
        };
    }
}
