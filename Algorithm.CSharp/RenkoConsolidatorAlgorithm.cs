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

using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of how to initialize and use the RenkoConsolidator
    /// </summary>
    /// <meta name="tag" content="renko" />
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="consolidating data" />
    public class RenkoConsolidatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initializes the algorithm state.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2012, 01, 01);
            SetEndDate(2013, 01, 01);

            AddEquity("SPY", Resolution.Daily);

            // this is the simple constructor that will perform the renko logic to the Value
            // property of the data it receives.

            // break SPY into $2.5 renko bricks and send that data to our 'OnRenkoBar' method
            var renkoClose = new RenkoConsolidator(2.5m);
            renkoClose.DataConsolidated += (sender, consolidated) =>
            {
                // call our event handler for renko data
                HandleRenkoClose(consolidated);
            };

            // register the consolidator for updates
            SubscriptionManager.AddConsolidator("SPY", renkoClose);


            // this is the full constructor that can accept a value selector and a volume selector
            // this allows us to perform the renko logic on values other than Close, even computed values!

            // break SPY into (2*o + h + l + 3*c)/7
            var renko7bar = new RenkoConsolidator<TradeBar>(2.5m, x => (2 * x.Open + x.High + x.Low + 3 * x.Close) / 7m, x => x.Volume);
            renko7bar.DataConsolidated += (sender, consolidated) =>
            {
                HandleRenko7Bar(consolidated);
            };

            // register the consolidator for updates
            SubscriptionManager.AddConsolidator("SPY", renko7bar);
        }

        /// <summary>
        /// We're doing our analysis in the OnRenkoBar method, but the framework verifies that this method exists, so we define it.
        /// </summary>
        public void OnData(TradeBars data)
        {
        }

        /// <summary>
        /// This function is called by our renkoClose consolidator defined in Initialize()
        /// </summary>
        /// <param name="data">The new renko bar produced by the consolidator</param>
        public void HandleRenkoClose(RenkoBar data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(data.Symbol, 1.0);
            }
            Log($"CLOSE - {data.Time.ToIso8601Invariant()} - {data.Open} {data.Close}");
        }

        /// <summary>
        /// This function is called by our renko7bar onsolidator defined in Initialize()
        /// </summary>
        /// <param name="data">The new renko bar produced by the consolidator</param>
        public void HandleRenko7Bar(RenkoBar data)
        {
            if (Portfolio.Invested)
            {
                Liquidate(data.Symbol);
            }
            Log($"7BAR - {data.Time.ToIso8601Invariant()} - {data.Open} {data.Close}");
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "29"},
            {"Average Win", "1.14%"},
            {"Average Loss", "-1.76%"},
            {"Compounding Annual Return", "-2.045%"},
            {"Drawdown", "11.000%"},
            {"Expectancy", "-0.059"},
            {"Net Profit", "-2.050%"},
            {"Sharpe Ratio", "-0.148"},
            {"Probabilistic Sharpe Ratio", "10.284%"},
            {"Loss Rate", "43%"},
            {"Win Rate", "57%"},
            {"Profit-Loss Ratio", "0.65"},
            {"Alpha", "-0.013"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0.089"},
            {"Annual Variance", "0.008"},
            {"Information Ratio", "-1.032"},
            {"Tracking Error", "0.145"},
            {"Treynor Ratio", "-25.917"},
            {"Total Fees", "$117.46"},
            {"Fitness Score", "0.044"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.219"},
            {"Return Over Maximum Drawdown", "-0.185"},
            {"Portfolio Turnover", "0.094"},
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
            {"OrderListHash", "f6815165e259f48000413986baa32b75"}
        };
    }
}
