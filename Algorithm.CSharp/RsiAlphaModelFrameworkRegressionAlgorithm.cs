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
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert the behavior of <see cref="RsiAlphaModel"/>.
    /// </summary>
    public class RsiAlphaModelFrameworkRegressionAlgorithm : BaseFrameworkRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetAlpha(new RsiAlphaModel());
        }

        public override void OnEndOfAlgorithm()
        {
            // We have removed all securities from the universe. The Alpha Model should remove the consolidator
            var consolidatorCount = SubscriptionManager.Subscriptions.Sum(s => s.Consolidators.Count);
            if (consolidatorCount > 0)
            {
                throw new RegressionTestException($"The number of consolidators should be zero. Actual: {consolidatorCount}");
            }
        }

        public override long DataPoints => 772;

        public override int AlgorithmHistoryDataPoints => 56;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "28"},
            {"Average Win", "0.14%"},
            {"Average Loss", "-0.08%"},
            {"Compounding Annual Return", "0.234%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0.186"},
            {"Start Equity", "100000"},
            {"End Equity", "100019.20"},
            {"Net Profit", "0.019%"},
            {"Sharpe Ratio", "-0.1"},
            {"Sortino Ratio", "-0.126"},
            {"Probabilistic Sharpe Ratio", "37.678%"},
            {"Loss Rate", "57%"},
            {"Win Rate", "43%"},
            {"Profit-Loss Ratio", "1.77"},
            {"Alpha", "0.059"},
            {"Beta", "-0.335"},
            {"Annual Standard Deviation", "0.048"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-2.498"},
            {"Tracking Error", "0.078"},
            {"Treynor Ratio", "0.014"},
            {"Total Fees", "$62.12"},
            {"Estimated Strategy Capacity", "$30000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.67%"},
            {"OrderListHash", "ddd8bbb62bc3d6306d7e388d383c872a"}
        };
    }
}
