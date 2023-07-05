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
                throw new Exception($"The number of consolidators should be zero. Actual: {consolidatorCount}");
            }
        }

        public override long DataPoints => 772;

        public override int AlgorithmHistoryDataPoints => 56;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "24"},
            {"Average Win", "0.15%"},
            {"Average Loss", "-0.08%"},
            {"Compounding Annual Return", "5.947%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0.247"},
            {"Net Profit", "0.476%"},
            {"Sharpe Ratio", "0.903"},
            {"Probabilistic Sharpe Ratio", "48.470%"},
            {"Loss Rate", "57%"},
            {"Win Rate", "43%"},
            {"Profit-Loss Ratio", "1.91"},
            {"Alpha", "0.114"},
            {"Beta", "-0.358"},
            {"Annual Standard Deviation", "0.048"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-1.959"},
            {"Tracking Error", "0.079"},
            {"Treynor Ratio", "-0.121"},
            {"Total Fees", "$58.36"},
            {"Estimated Strategy Capacity", "$38000000.00"},
            {"Lowest Capacity Asset", "NB R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.56%"},
            {"OrderListHash", "18560b80df7aea5dda5dbfded0749ce0"}
        };
    }
}
