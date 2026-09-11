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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using the strike, expiration and moneyness filters on index options: in the universe selection
    /// of standard and weekly contracts, on the chains of the <see cref="Slice"/> and on <see cref="QCAlgorithm.OptionChain(Symbol, bool)"/>
    /// </summary>
    public class IndexOptionChainFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime FirstDay = new(2021, 1, 4);
        private static readonly DateTime StandardExpiry = new(2021, 1, 15);

        private Symbol _spx;
        private Symbol _spxw;
        private bool _spxChainSeen;
        private bool _zeroDteSeen;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 4);
            SetEndDate(2021, 1, 8);
            SetCash(1000000);

            // Standard SPX contracts: the out of the money ones with strikes below 4000
            var spx = AddIndexOption("SPX");
            spx.SetFilter(universe => universe.OutOfTheMoney().StrikesBelow(4000m));
            _spx = spx.Symbol;

            // Weekly SPXW contracts: the 3700 strike of the expirations after the first day
            var spxw = AddIndexOption("SPX", "SPXW");
            spxw.SetFilter(universe => universe.Strikes([3700m]).ExpiringAfter(FirstDay));
            _spxw = spxw.Symbol;

            // The latest universe data, from 2020-12-31, lists the 3200, 3700, 3800 and 4250 calls and the 3200 and 4200 puts
            // expiring on 2021-01-15, with the index at 3766.63: the same filters narrow the chain down
            var chain = OptionChain(_spx);
            if (chain.Count != 6 || chain.Underlying.Price != 3766.63m)
            {
                throw new RegressionTestException($"Expected the 6 SPX contracts at 3766.63 but got {chain.Count} at {chain.Underlying.Price}");
            }
            AssertContracts(chain.OutOfTheMoney(), "OutOfTheMoney()", (3800m, OptionRight.Call), (4250m, OptionRight.Call), (3200m, OptionRight.Put));
            AssertContracts(chain.InTheMoney(), "InTheMoney()", (3200m, OptionRight.Call), (3700m, OptionRight.Call), (4200m, OptionRight.Put));
            // The strikes on either side of 3766.63 are 3700 and 3800, both listed as calls only; 50 points reach 3800, 25 none
            AssertContracts(chain.AtTheMoney(), "AtTheMoney()", (3700m, OptionRight.Call), (3800m, OptionRight.Call));
            AssertContracts(chain.AtTheMoney(50m), "AtTheMoney(50)", (3800m, OptionRight.Call));
            AssertContracts(chain.AtTheMoney(25m), "AtTheMoney(25)");
            AssertContracts(chain.AtTheMoney(0), "AtTheMoney(0)");
            AssertContracts(chain.StrikesAbove(3700m).StrikesBelow(4250m), "StrikesAbove(3700).StrikesBelow(4250)", (3800m, OptionRight.Call), (4200m, OptionRight.Put));
            AssertContracts(chain.Strikes([3200m, 4250m]), "Strikes([3200, 4250])", (3200m, OptionRight.Call), (4250m, OptionRight.Call), (3200m, OptionRight.Put));
            AssertContracts(chain.OutOfTheMoney().StrikesBelow(4000m), "the SPX universe filter", (3800m, OptionRight.Call), (3200m, OptionRight.Put));
            if (chain.Expiration([StandardExpiry]).Count != chain.Count || chain.FarthestExpiration().Count != chain.Count
                || chain.ExpiringAfter(StandardExpiry).Count != 0 || chain.ExpiringBefore(StandardExpiry).Count != 0 || chain.ZeroDte().Count != 0)
            {
                throw new RegressionTestException("Expected every SPX contract to expire on 2021-01-15");
            }
        }

        public override void OnData(Slice slice)
        {
            if (slice.OptionChains.TryGetValue(_spx, out var spxChain))
            {
                _spxChainSeen = true;
                // The universe selected the out of the money contracts below 4000: the 3800 call and the 3200 put.
                // The index stays between those strikes, so the chain filter agrees with the universe filter
                AssertContracts(spxChain, "the SPX slice chain", (3800m, OptionRight.Call), (3200m, OptionRight.Put));
                if (spxChain.OutOfTheMoney().Count != spxChain.Count)
                {
                    throw new RegressionTestException("Expected the SPX slice chain to be out of the money");
                }
                AssertMoneyness(spxChain);
            }

            if (!slice.OptionChains.TryGetValue(_spxw, out var chain))
            {
                return;
            }

            // The universe selected the 3700 strike of the expirations after the first day: 2021-01-06 and 2021-01-08
            if (chain.Count == 0 || chain.Strikes([3700m]).Count != chain.Count || chain.ExpiringAfter(FirstDay).Count != chain.Count
                || chain.ExpiringBefore(new DateTime(2021, 1, 9)).Count != chain.Count)
            {
                throw new RegressionTestException("The SPXW slice chain disagrees with the universe filter");
            }
            AssertMoneyness(chain);

            var zeroDte = chain.ZeroDte();
            if (zeroDte.Any(x => x.Expiry.Date != Time.Date))
            {
                throw new RegressionTestException("ZeroDte() selected contracts not expiring today");
            }
            _zeroDteSeen |= zeroDte.Count > 0;

            var farthest = chain.FarthestExpiration();
            var maxExpiry = chain.Max(x => x.Expiry);
            if (farthest.Count == 0 || farthest.Any(x => x.Expiry != maxExpiry))
            {
                throw new RegressionTestException("FarthestExpiration() mismatch");
            }

            // Buy the 3700 call of the nearest expiration after today
            if (!_traded)
            {
                var contract = chain.CallsOnly().ExpiringAfter(Time).FrontMonth().FirstOrDefault();
                if (contract != null)
                {
                    MarketOrder(contract.Symbol, 1);
                    _traded = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_spxChainSeen || !_zeroDteSeen || !_traded)
            {
                throw new RegressionTestException($"Expected the SPX chain ({_spxChainSeen}), a 0DTE SPXW contract ({_zeroDteSeen}) and a trade ({_traded})");
            }
        }

        /// <summary>
        /// The moneyness filters partition the chain around the current index price, and match the strike bounds for a single right
        /// </summary>
        private static void AssertMoneyness(OptionChain chain)
        {
            var price = chain.Underlying.Price;
            var otm = chain.OutOfTheMoney();
            var itm = chain.InTheMoney();
            if (otm.Count + itm.Count + chain.Strikes([price]).Count != chain.Count
                || otm.Any(x => x.Right == OptionRight.Call ? x.Strike <= price : x.Strike >= price)
                || itm.Any(x => x.Right == OptionRight.Call ? x.Strike >= price : x.Strike <= price)
                || chain.CallsOnly().OutOfTheMoney().Count != chain.CallsOnly().StrikesAbove(price).Count
                || chain.PutsOnly().OutOfTheMoney().Count != chain.PutsOnly().StrikesBelow(price).Count)
            {
                throw new RegressionTestException($"Moneyness filters mismatch at {price}");
            }
        }

        private static void AssertContracts(OptionChain chain, string filter, params (decimal strike, OptionRight right)[] expected)
        {
            var actual = chain.Select(x => (x.Strike, x.Right)).OrderBy(x => x.Strike).ThenBy(x => x.Right).ToList();
            var expectedContracts = expected.OrderBy(x => x.strike).ThenBy(x => x.right).ToList();
            if (!actual.SequenceEqual(expectedContracts))
            {
                throw new RegressionTestException($"{filter}: expected {Format(expectedContracts)} but got {Format(actual)}");
            }
        }

        private static string Format(IEnumerable<(decimal strike, OptionRight right)> contracts)
        {
            return string.Join(", ", contracts.Select(x => $"{x.strike} {x.right}"));
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
        public long DataPoints => 25607;

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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.75%"},
            {"Compounding Annual Return", "-42.123%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "992475"},
            {"Net Profit", "-0.752%"},
            {"Sharpe Ratio", "-3.457"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "22.012%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.468"},
            {"Beta", "-0.369"},
            {"Annual Standard Deviation", "0.04"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-38.008"},
            {"Tracking Error", "0.118"},
            {"Treynor Ratio", "0.377"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$940000.00"},
            {"Lowest Capacity Asset", "SPXW XKZ5O96SL626|SPX 31"},
            {"Portfolio Turnover", "0.13%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "8e3ebdde25785c0e5d3527d7260d2fdc"}
        };
    }
}
