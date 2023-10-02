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
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert the behavior of <see cref="HistoricalReturnsAlphaModel"/>.
    /// </summary>
    public class HistoricalReturnsAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new HistoricalReturnsAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
            const int expected = 74;
            if (Insights.TotalCount != expected)
            {
                throw new Exception($"The total number of insights should be {expected}. Actual: {Insights.TotalCount}");
            }
        }

        public override long DataPoints => 779;

        public override int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "62"},
            {"Average Win", "0.18%"},
            {"Average Loss", "-0.20%"},
            {"Compounding Annual Return", "33.779%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "0.230"},
            {"Net Profit", "2.421%"},
            {"Sharpe Ratio", "3.796"},
            {"Probabilistic Sharpe Ratio", "80.897%"},
            {"Loss Rate", "35%"},
            {"Win Rate", "65%"},
            {"Profit-Loss Ratio", "0.89"},
            {"Alpha", "0.23"},
            {"Beta", "-0.024"},
            {"Annual Standard Deviation", "0.059"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "0.448"},
            {"Tracking Error", "0.077"},
            {"Treynor Ratio", "-9.367"},
            {"Total Fees", "$255.12"},
            {"Estimated Strategy Capacity", "$11000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.85%"},
            {"OrderListHash", "3d5a177bbc0474147b39ce5bfa69c9fc"}
        };
    }
}
