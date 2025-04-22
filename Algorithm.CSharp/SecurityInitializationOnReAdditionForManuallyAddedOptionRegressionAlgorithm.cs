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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing the behavior of the algorithm when a security is removed and re-added.
    /// It asserts that the securities are marked as non-tradable when removed and that they are tradable when re-added.
    /// It also asserts that the algorithm receives the correct security changed events for the added and removed securities.
    ///
    /// Additionally, it tests that the security is initialized after every addition, and no more.
    ///
    /// This specific algorithm tests this behavior for manually added option contracts.
    /// </summary>
    public class SecurityInitializationOnReAdditionForManuallyAddedOptionRegressionAlgorithm : SecurityInitializationOnReAdditionForEquityRegressionAlgorithm
    {
        private static readonly Symbol _optionContractSymbol = QuantConnect.Symbol.CreateOption(
            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            Market.USA,
            OptionStyle.American,
            OptionRight.Call,
            342.9m,
            new DateTime(2014, 07, 19));

        private int _securityAdditionsCount;

        protected override DateTime StartTimeToUse => new DateTime(2014, 06, 04);

        protected override DateTime EndTimeToUse => new DateTime(2014, 06, 20);

        protected override Security AddSecurity()
        {
            _securityAdditionsCount++;
            return AddOptionContract(_optionContractSymbol, Resolution.Daily);
        }

        protected override void AssertSecurityInitializationCount(Dictionary<Security, int> securityInializationCounts, Security security)
        {
            // The first time the contract is added, the underlying equity will be added and initialized as well.
            // The following times the contract is added, the underlying equity will not be added again.
            var expectedSecuritiesInitialized = 1;
            if (_securityAdditionsCount == 1)
            {
                expectedSecuritiesInitialized = 2;
            }

            if (securityInializationCounts.Count != expectedSecuritiesInitialized)
            {
                throw new RegressionTestException($"Expected {expectedSecuritiesInitialized} security to be initialized. " +
                    $"Got {securityInializationCounts.Count}");
            }

            if (!securityInializationCounts.TryGetValue(security, out var count) || count != 1)
            {
                throw new RegressionTestException($"Expected the option contract to be initialized once and once only, " +
                    $"but was initialized {count} times");
            }

            if (expectedSecuritiesInitialized == 2 &&
                !securityInializationCounts.TryGetValue(Securities[security.Symbol.Underlying], out count) || count != 1)
            {
                throw new RegressionTestException($"Expected the underlying security to be initialized once and once only, " +
                    $"but was initialized {count} times");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 115;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 5;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-6.27"},
            {"Tracking Error", "0.056"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
