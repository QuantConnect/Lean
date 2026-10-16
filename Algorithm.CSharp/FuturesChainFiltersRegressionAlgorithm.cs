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
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using the futures chain filters, the same ones the futures universe selection offers,
    /// on <see cref="QCAlgorithm.FuturesChain(Symbol, bool)"/> and on the chains of the <see cref="Slice"/>
    /// </summary>
    public class FuturesChainFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime EndOf2013 = new(2013, 12, 31);

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

            // The contracts expiring within a year
            var es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            es.SetFilter(universe => universe.Expiration(0, 365));
            _es = es.Symbol;

            // The liquid contracts, by open interest
            var gc = AddFuture(Futures.Metals.Gold, Resolution.Minute, Market.COMEX);
            gc.SetFilter(universe => universe.OpenInterest(100000, long.MaxValue));
            _gc = gc.Symbol;

            // The full chain from the universe data: December 2013 and March, June, September and December 2014
            var chain = FuturesChain(_es);
            if (chain.Count != 5)
            {
                throw new RegressionTestException($"Expected 5 ES contracts but got {chain.Count}");
            }
            AssertExpiries(chain.FrontMonth(), "FrontMonth()", (2013, 12));
            AssertExpiries(chain.BackMonth(), "BackMonth()", (2014, 3));
            AssertExpiries(chain.BackMonths(), "BackMonths()", (2014, 3), (2014, 6), (2014, 9), (2014, 12));
            AssertExpiries(chain.FarthestExpiration(), "FarthestExpiration()", (2014, 12));
            AssertExpiries(chain.ExpirationCycle([3, 9]), "ExpirationCycle([3, 9])", (2014, 3), (2014, 9));
            // ES contracts are named after their expiration month, so the contract month filters agree with the expiration ones
            AssertExpiries(chain.ContractMonths([3, 9]), "ContractMonths([3, 9])", (2014, 3), (2014, 9));
            AssertExpiries(chain.ExpiringBefore(EndOf2013), "ExpiringBefore(2013-12-31)", (2013, 12));
            AssertExpiries(chain.ExpiringAfter(EndOf2013).ExpiringBefore(new DateTime(2014, 7, 1)), "ExpiringAfter(2013-12-31).ExpiringBefore(2014-07-01)", (2014, 3), (2014, 6));
            AssertExpiries(chain.Expiration([chain.FrontMonth().First().Expiry]), "Expiration([front month expiry])", (2013, 12));
            if (chain.ZeroDte().Count != 0 || chain.StandardsOnly().Count != chain.Count || chain.WeeklysOnly().Count != 0)
            {
                throw new RegressionTestException("Expected no contract expiring today and only standard contracts");
            }

            // The liquidity filters read the universe data: only the front month has more than a million contracts open
            AssertExpiries(chain.OpenInterest(1000000, long.MaxValue), "OpenInterest(1000000, max)", (2013, 12));
            if (chain.OI(0, 1000000).Count != chain.Count - 1 || chain.Volume(1, long.MaxValue).Count != chain.Count(x => x.Volume >= 1))
            {
                throw new RegressionTestException("Open interest or volume filter mismatch");
            }
        }

        public override void OnData(Slice slice)
        {
            if (slice.FuturesChains.TryGetValue(_es, out var esChain))
            {
                _esChainSeen = true;
                // The universe selected the contracts expiring within a year, so the chain filters agree with it
                if (esChain.Count == 0 || esChain.Count > 4 || esChain.Expiration(0, 365).Count != esChain.Count || esChain.ExpiringAfter(Time).Count != esChain.Count
                    || esChain.ZeroDte().Count != 0 || esChain.ExpirationCycle([3, 6, 9, 12]).Count != esChain.Count || esChain.ExpirationCycle([1, 2]).Count != 0
                    || esChain.StandardsOnly().Count != esChain.Count || esChain.WeeklysOnly().Count != 0)
                {
                    throw new RegressionTestException("The ES slice chain disagrees with the universe filter");
                }
                var frontMonth = esChain.FrontMonth();
                var farthest = esChain.FarthestExpiration();
                if (frontMonth.Count == 0 || frontMonth.Any(x => x.Expiry != esChain.Min(c => c.Expiry)) || farthest.Any(x => x.Expiry != esChain.Max(c => c.Expiry))
                    || esChain.BackMonths().Count != esChain.Count - frontMonth.Count)
                {
                    throw new RegressionTestException("Front month, back months or farthest expiration mismatch on the ES slice chain");
                }
                if (esChain.OpenInterest(1, long.MaxValue).Count != esChain.Count(x => x.OpenInterest >= 1) || esChain.Volume(1, long.MaxValue).Count != esChain.Count(x => x.Volume >= 1))
                {
                    throw new RegressionTestException("Open interest or volume filter mismatch on the ES slice chain");
                }

                // Buy the front contract expiring at least 90 days out
                if (!_traded)
                {
                    var contract = esChain.ExpiringAfter(Time.Date.AddDays(90)).FrontMonth().FirstOrDefault();
                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                        _traded = true;
                    }
                }
            }

            if (slice.FuturesChains.TryGetValue(_gc, out var gcChain))
            {
                _gcChainSeen = true;
                // Only the December 2013 contract had more than a hundred thousand contracts open
                if (gcChain.Count == 0 || gcChain.Any(x => x.Expiry.Year != 2013 || x.Expiry.Month != 12) || gcChain.FrontMonth().Count != gcChain.Count)
                {
                    throw new RegressionTestException($"The GC slice chain disagrees with the universe filter: {string.Join(", ", gcChain.Select(x => x.Expiry))}");
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

        private static void AssertExpiries(FuturesChain chain, string filter, params (int year, int month)[] expected)
        {
            var actual = chain.Select(x => (x.Expiry.Year, x.Expiry.Month)).OrderBy(x => x).ToList();
            if (!actual.SequenceEqual(expected.OrderBy(x => x)))
            {
                throw new RegressionTestException($"{filter}: expected {string.Join(", ", expected)} but got {string.Join(", ", actual)}");
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
        public long DataPoints => 34838;

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
            {"Adjusted Sharpe Ratio", "0"},
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
