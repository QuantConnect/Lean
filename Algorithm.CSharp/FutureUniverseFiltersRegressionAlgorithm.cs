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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using the expiration set and bound filters in the futures universe selection,
    /// the same ones the option universes and chains offer, and checking the selected chains in the <see cref="Slice"/>
    /// </summary>
    public class FutureUniverseFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime EndOf2013 = new(2013, 12, 31);
        private static readonly DateTime EndOfNovember2014 = new(2014, 11, 30);

        private Symbol _es;
        private Symbol _gc;
        private bool _esChainSeen;
        private bool _gcChainSeen;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 9);
            SetCash(1000000);

            // The 2014 contracts up to September
            var es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            es.SetFilter(universe => universe.ExpiringAfter(EndOf2013).ExpiringBefore(EndOfNovember2014));
            _es = es.Symbol;

            // The contracts expiring this year
            var gc = AddFuture(Futures.Metals.Gold, Resolution.Minute, Market.COMEX);
            gc.SetFilter(universe => universe.ExpiringBefore(new DateTime(2014, 1, 1)));
            _gc = gc.Symbol;

            // The full chain from the universe data lists the December 2013 contract and the March to December 2014 ones
            var chain = FuturesChain(_es);
            var expiries = chain.Select(x => x.Expiry).OrderBy(x => x).ToList();
            if (expiries.Count != 5 || expiries[0] > EndOf2013 || expiries.Skip(1).Any(x => x.Year != 2014))
            {
                throw new RegressionTestException($"Unexpected ES chain expiries: {string.Join(", ", expiries)}");
            }
        }

        public override void OnData(Slice slice)
        {
            if (slice.FuturesChains.TryGetValue(_es, out var esChain))
            {
                _esChainSeen = true;
                // March, June and September 2014
                if (esChain.Count == 0 || esChain.Count > 3 || esChain.Any(x => x.Expiry <= EndOf2013 || x.Expiry >= EndOfNovember2014))
                {
                    throw new RegressionTestException($"The ES chain disagrees with the universe filter: {string.Join(", ", esChain.Select(x => x.Expiry))}");
                }
                if (!_traded)
                {
                    MarketOrder(esChain.OrderBy(x => x.Expiry).First().Symbol, 1);
                    _traded = true;
                }
            }

            if (slice.FuturesChains.TryGetValue(_gc, out var gcChain))
            {
                _gcChainSeen = true;
                // October, November and December 2013
                if (gcChain.Count == 0 || gcChain.Count > 3 || gcChain.Any(x => x.Expiry.Year != 2013))
                {
                    throw new RegressionTestException($"The GC chain disagrees with the universe filter: {string.Join(", ", gcChain.Select(x => x.Expiry))}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_esChainSeen || !_gcChainSeen || !_traded)
            {
                throw new RegressionTestException($"Expected the ES chain ({_esChainSeen}), the GC chain ({_gcChainSeen}) and a trade ({_traded})");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 38894;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-11.911%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "998958.2"},
            {"Net Profit", "-0.104%"},
            {"Sharpe Ratio", "-9.32"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.048"},
            {"Beta", "0.095"},
            {"Annual Standard Deviation", "0.013"},
            {"Annual Variance", "0"},
            {"Information Ratio", "5.187"},
            {"Tracking Error", "0.123"},
            {"Treynor Ratio", "-1.269"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$940000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "2.77%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "3b6b723d50c0d435d763aa456af197a6"}
        };
    }
}
