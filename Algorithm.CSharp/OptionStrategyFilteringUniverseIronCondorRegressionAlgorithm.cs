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
    /// Regression algorithm of filtering with Iron Condor option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionStrategyFilteringUniverseIronCondorRegressionAlgorithm
        : OptionStrategyFilteringUniverseBaseAlgorithm
    {
        public override void Initialize()
        {
            FilterFunc = u => u.IncludeWeeklys().IronCondor(28, 5, 10);
            ExpectedCount = 4;

            base.Initialize();
        }

        protected override void TestFiltering(OptionChain chain)
        {
            var count = chain.Count();
            if (count != ExpectedCount)
            {
                throw new RegressionTestException(
                    $"Number of contract returned does not match expectation, {count}, {ExpectedCount}"
                );
            }

            var farCallStrike = 757.50m;
            var nearCallStrike = 752.50m;
            var nearPutStrike = 742.50m;
            var farPutStrike = 737.50m;
            var expiry = new DateTime(2016, 1, 22);

            var farCall = chain.SingleOrDefault(x =>
                x.Right == OptionRight.Call && x.Strike == farCallStrike && x.Expiry == expiry
            );
            var nearCall = chain.SingleOrDefault(x =>
                x.Right == OptionRight.Call && x.Strike == nearCallStrike && x.Expiry == expiry
            );
            var nearPut = chain.SingleOrDefault(x =>
                x.Right == OptionRight.Put && x.Strike == nearPutStrike && x.Expiry == expiry
            );
            var farPut = chain.SingleOrDefault(x =>
                x.Right == OptionRight.Put && x.Strike == farPutStrike && x.Expiry == expiry
            );
            if (farCall == null || nearCall == null || nearPut == null || farPut == null)
            {
                throw new RegressionTestException($"No contract returned match condition");
            }

            var strategy = OptionStrategies.IronCondor(
                OptionSymbol,
                farPutStrike,
                nearPutStrike,
                nearCallStrike,
                farCallStrike,
                expiry
            );
            Buy(strategy, 1);

            /* we can obtain the same result from market orders
            MarketOrder(farCall.Symbol, +1);
            MarketOrder(nearCall.Symbol, -1);
            MarketOrder(nearPut.Symbol, -1);
            MarketOrder(farPut.Symbol, +1);
            */

            AssertOptionStrategyIsPresent(OptionStrategyDefinitions.IronCondor.Name, 1);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 462436;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics =>
            new Dictionary<string, string>
            {
                { "Total Orders", "4" },
                { "Average Win", "0%" },
                { "Average Loss", "0%" },
                { "Compounding Annual Return", "0%" },
                { "Drawdown", "0%" },
                { "Expectancy", "0" },
                { "Start Equity", "200000" },
                { "End Equity", "199661" },
                { "Net Profit", "0%" },
                { "Sharpe Ratio", "0" },
                { "Sortino Ratio", "0" },
                { "Probabilistic Sharpe Ratio", "0%" },
                { "Loss Rate", "0%" },
                { "Win Rate", "0%" },
                { "Profit-Loss Ratio", "0" },
                { "Alpha", "0" },
                { "Beta", "0" },
                { "Annual Standard Deviation", "0" },
                { "Annual Variance", "0" },
                { "Information Ratio", "0" },
                { "Tracking Error", "0" },
                { "Treynor Ratio", "0" },
                { "Total Fees", "$4.00" },
                { "Estimated Strategy Capacity", "$3400000.00" },
                { "Lowest Capacity Asset", "GOOCV 306JVPPH5QXMU|GOOCV VP83T1ZUHROL" },
                { "Portfolio Turnover", "2.76%" },
                { "OrderListHash", "5162055c872b1954d3c51d05391defc3" }
            };
    }
}
