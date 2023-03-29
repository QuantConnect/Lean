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
    /// Regression algorithm to assert the behavior of <see cref="MacdAlphaModel"/>.
    /// </summary>
    public class MacdAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new MacdAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
            const int expected = 4;
            if (Insights.TotalCount != expected)
            {
                throw new Exception($"The total number of insights should be {expected}. Actual: {Insights.TotalCount}");
            }
        }

        public override int AlgorithmHistoryDataPoints => 136;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "38"},
            {"Average Win", "0.38%"},
            {"Average Loss", "-0.09%"},
            {"Compounding Annual Return", "66.547%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "2.700"},
            {"Net Profit", "4.282%"},
            {"Sharpe Ratio", "6.951"},
            {"Probabilistic Sharpe Ratio", "94.794%"},
            {"Loss Rate", "29%"},
            {"Win Rate", "71%"},
            {"Profit-Loss Ratio", "4.24"},
            {"Alpha", "0.5"},
            {"Beta", "-0.29"},
            {"Annual Standard Deviation", "0.064"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "2.776"},
            {"Tracking Error", "0.088"},
            {"Treynor Ratio", "-1.527"},
            {"Total Fees", "$75.37"},
            {"Estimated Strategy Capacity", "$4900000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.18%"},
            {"OrderListHash", "796c98641f705dc9eafc0772aa83a0ea" }
        };
    }
}
