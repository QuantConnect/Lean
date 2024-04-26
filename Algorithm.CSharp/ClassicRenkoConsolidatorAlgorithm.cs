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
    public class ClassicRenkoConsolidatorAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
            var renkoClose = new ClassicRenkoConsolidator(2.5m);
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
            var renko7bar = new ClassicRenkoConsolidator<TradeBar>(2.5m, x => (2 * x.Open + x.High + x.Low + 3 * x.Close) / 7m, x => x.Volume);
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2003;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "29"},
            {"Average Win", "1.85%"},
            {"Average Loss", "-1.49%"},
            {"Compounding Annual Return", "7.817%"},
            {"Drawdown", "6.800%"},
            {"Expectancy", "0.281"},
            {"Start Equity", "100000"},
            {"End Equity", "107838.74"},
            {"Net Profit", "7.839%"},
            {"Sharpe Ratio", "0.692"},
            {"Sortino Ratio", "0.636"},
            {"Probabilistic Sharpe Ratio", "39.336%"},
            {"Loss Rate", "43%"},
            {"Win Rate", "57%"},
            {"Profit-Loss Ratio", "1.24"},
            {"Alpha", "0.004"},
            {"Beta", "0.411"},
            {"Annual Standard Deviation", "0.07"},
            {"Annual Variance", "0.005"},
            {"Information Ratio", "-0.704"},
            {"Tracking Error", "0.083"},
            {"Treynor Ratio", "0.118"},
            {"Total Fees", "$129.34"},
            {"Estimated Strategy Capacity", "$1000000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "7.91%"},
            {"OrderListHash", "cb118f22e33089e9ab4af8514e4f2b5f"}
        };
    }
}
