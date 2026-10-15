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

using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the proceeds of an equity sold on a cash account on its delisting warning
    /// settle even though the security is delisted before the settlement date, the delisted security is kept until its funds settle
    /// </summary>
    public class DelistedEquitySoldOnWarningCashAccountSettlementRegressionAlgorithm : DelistedEquityCashAccountSettlementRegressionAlgorithm
    {
        /// <summary>
        /// Whether to sell the position on the delisting warning instead of holding it through the delisting
        /// </summary>
        protected override bool LiquidateOnDelistingWarning => true;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 87;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-2.79%"},
            {"Compounding Annual Return", "-62.016%"},
            {"Drawdown", "2.800%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "97210.8"},
            {"Net Profit", "-2.789%"},
            {"Sharpe Ratio", "-7.693"},
            {"Sortino Ratio", "-14.741"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.452"},
            {"Beta", "-0.538"},
            {"Annual Standard Deviation", "0.074"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-5.823"},
            {"Tracking Error", "0.136"},
            {"Treynor Ratio", "1.062"},
            {"Total Fees", "$18.35"},
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "AAA SEVKGI6HF885"},
            {"Portfolio Turnover", "8.99%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "a817fe9c9dfa65c610566663bf88febe"}
        };
    }
}
