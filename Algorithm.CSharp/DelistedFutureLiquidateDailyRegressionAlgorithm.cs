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
    /// Regression algorithm which reproduces GH issue 4446, in the case of daily resolution.
    /// </summary>
    public class DelistedFutureLiquidateDailyRegressionAlgorithm : DelistedFutureLiquidateRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        protected override void PlaceOrder(Symbol symbol)
        {
            // We place a limit order because on daily resolution, data may come at a time when market is closed, so market orders are not allowed.
            // Also, we use a very high limit price to ensure the order is filled right away with the next bar.
            LimitOrder(symbol, 1, Securities[symbol].Price * 2m);
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 1450;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "7.61%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "37.073%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "7.604%"},
            {"Sharpe Ratio", "3.103"},
            {"Probabilistic Sharpe Ratio", "99.007%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.141"},
            {"Beta", "0.272"},
            {"Annual Standard Deviation", "0.081"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-1.511"},
            {"Tracking Error", "0.098"},
            {"Treynor Ratio", "0.918"},
            {"Total Fees", "$1.85"},
            {"Estimated Strategy Capacity", "$62000000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.019"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "112.785"},
            {"Portfolio Turnover", "0.019"},
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
            {"OrderListHash", "ec45ccbcd8123256973822923b4919ac"}
        };
    }
}
