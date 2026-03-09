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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a future contract selected by both the continuous future and
    /// the future chain universes gets liquidated on delisting and that the algorithm receives the correct
    /// security addition/removal notifications.
    ///
    /// This algorithm uses Gold futures with midnight expiry time to reproduce an edge case where
    /// the delisting data instance and the universe deselection happen in the same loop but without a particular order.
    ///
    /// This partly reproduces GH issue https://github.com/QuantConnect/Lean/issues/9092
    /// </summary>
    public class DelistedFutureLiquidateFromChainAndContinuousMidnightExpiryRegressionAlgorithm : DelistedFutureLiquidateFromChainAndContinuousRegressionAlgorithm
    {
        protected override string FutureTicker => Futures.Metals.Gold;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 317492;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-5.18%"},
            {"Compounding Annual Return", "-20.700%"},
            {"Drawdown", "6.400%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "94817.53"},
            {"Net Profit", "-5.182%"},
            {"Sharpe Ratio", "-2.965"},
            {"Sortino Ratio", "-3.407"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.064"},
            {"Beta", "-0.2"},
            {"Annual Standard Deviation", "0.048"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-4.899"},
            {"Tracking Error", "0.11"},
            {"Treynor Ratio", "0.716"},
            {"Total Fees", "$2.47"},
            {"Estimated Strategy Capacity", "$1400000.00"},
            {"Lowest Capacity Asset", "GC VL5E74HP3EE5"},
            {"Portfolio Turnover", "3.18%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "c197fe000cd6d7f2fd84860f7086d730"}
        };
    }
}
