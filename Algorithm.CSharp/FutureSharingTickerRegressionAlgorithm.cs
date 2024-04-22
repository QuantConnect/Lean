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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm showcasing adding two futures with the same ticker for different market, related to PR 4328
    /// </summary>
    public class FutureSharingTickerRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);

            var gold = AddFuture(Futures.Metals.Gold, market: Market.COMEX);
            gold.SetFilter(0, 182);

            // this future does not exist just added as an example
            var gold2 = AddFuture(Futures.Metals.Gold, market: Market.NYMEX);
            gold2.SetFilter(0, 182);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                foreach (var chain in data.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        where futuresContract.Expiry > Time.Date.AddDays(90)
                        select futuresContract
                    ).FirstOrDefault();

                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                    }
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 51429;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-99.356%"},
            {"Drawdown", "4.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "96325.06"},
            {"Net Profit", "-3.675%"},
            {"Sharpe Ratio", "-15.545"},
            {"Sortino Ratio", "-15.545"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "3.263"},
            {"Beta", "-0.263"},
            {"Annual Standard Deviation", "0.064"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "-56.095"},
            {"Tracking Error", "0.306"},
            {"Treynor Ratio", "3.773"},
            {"Total Fees", "$2.47"},
            {"Estimated Strategy Capacity", "$19000000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Portfolio Turnover", "44.37%"},
            {"OrderListHash", "2c82779586fa2691d412e4bd4c4ff2b1"}
        };
    }
}
