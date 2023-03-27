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
            const int expected = 6;
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
            {"Average Win", "0.35%"},
            {"Average Loss", "-0.09%"},
            {"Compounding Annual Return", "66.292%"},
            {"Drawdown", "1.800%"},
            {"Expectancy", "2.541"},
            {"Net Profit", "4.269%"},
            {"Sharpe Ratio", "6.944"},
            {"Probabilistic Sharpe Ratio", "94.826%"},
            {"Loss Rate", "28%"},
            {"Win Rate", "72%"},
            {"Profit-Loss Ratio", "3.90"},
            {"Alpha", "0.498"},
            {"Beta", "-0.288"},
            {"Annual Standard Deviation", "0.063"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "2.763"},
            {"Tracking Error", "0.088"},
            {"Treynor Ratio", "-1.528"},
            {"Total Fees", "$75.55"},
            {"Estimated Strategy Capacity", "$4900000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.21%"},
            {"OrderListHash", "fa61d8c4f00c03bddfef5a33c8c3aa4c"}
        };
    }
}
