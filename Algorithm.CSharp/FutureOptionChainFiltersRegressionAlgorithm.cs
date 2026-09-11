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
    /// Regression algorithm using the strike, expiration and moneyness filters on future options: in the universe selection
    /// of the future and of its options, on the chains of the <see cref="Slice"/> and on <see cref="QCAlgorithm.OptionChain(Symbol, bool)"/>
    /// </summary>
    public class FutureOptionChainFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime MarchExpiry = new(2020, 3, 20);
        private static readonly decimal[] SelectedStrikes = [3200m, 3210m, 3220m, 3230m, 3240m, 3250m];

        private Symbol _es;
        private bool _chainSeen;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 1, 6);
            SetCash(1000000);

            // The March 2020 future, by its expiration date
            var es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            es.SetFilter(universe => universe.Expiration([MarchExpiry]));
            _es = es.Symbol;

            // Its options: the out of the money contracts within three strikes of the future price
            AddFutureOption(_es, universe => universe.Strikes(-3, 3).OutOfTheMoney());

            // The option chain of the March future from the universe data: one expiration, the future at 3223.75
            var chain = OptionChain(QuantConnect.Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, MarchExpiry));
            if (chain.Count == 0 || chain.Underlying.Price != 3223.75m || chain.Symbol.SecurityType != SecurityType.FutureOption)
            {
                throw new RegressionTestException($"Expected the March ES option chain at 3223.75 but got {chain.Count} contracts at {chain.Underlying.Price}");
            }
            // Strikes are 10 points apart around the money: three each side of 3223.75 are 3200 to 3250
            AssertStrikes(chain.Strikes(-3, 3).OutOfTheMoney().CallsOnly(), "Strikes(-3, 3).OutOfTheMoney().CallsOnly()", 3230m, 3240m, 3250m);
            AssertStrikes(chain.Strikes(-3, 3).OutOfTheMoney().PutsOnly(), "Strikes(-3, 3).OutOfTheMoney().PutsOnly()", 3200m, 3210m, 3220m);
            // Only the put is listed at 3310
            AssertStrikes(chain.StrikesAbove(3300m).StrikesBelow(3320m), "StrikesAbove(3300).StrikesBelow(3320)", 3310m);
            AssertStrikes(chain.StrikesAbove(3300m).StrikesBelow(3320m).CallsOnly(), "StrikesAbove(3300).StrikesBelow(3320).CallsOnly()");
            AssertStrikes(chain.AtTheMoney(5m), "AtTheMoney(5)", 3220m, 3220m);
            if (chain.AtTheMoney().Count != 0 || chain.Expiration([MarchExpiry]).Count != chain.Count || chain.FarthestExpiration().Count != chain.Count
                || chain.ExpiringAfter(MarchExpiry).Count != 0 || chain.ZeroDte().Count != 0
                || chain.StandardsOnly().Count != chain.Count || chain.WeeklysOnly().Count != 0)
            {
                throw new RegressionTestException("Expiration or contract type filters mismatch on the March ES option chain");
            }
        }

        public override void OnData(Slice slice)
        {
            // One chain per future contract, keyed by its canonical option symbol
            foreach (var chain in slice.OptionChains.Values)
            {
                if (chain.Symbol.Underlying.ID.Date != MarchExpiry)
                {
                    throw new RegressionTestException($"Unexpected option chain for {chain.Symbol.Underlying}");
                }
                _chainSeen = true;

                // The universe selected the out of the money contracts within three strikes of the previous close: 3200 to 3250
                if (chain.Count == 0 || chain.Strikes(SelectedStrikes).Count != chain.Count || chain.Expiration([MarchExpiry]).Count != chain.Count)
                {
                    throw new RegressionTestException($"The option chain disagrees with the universe filter: {string.Join(", ", chain.Select(x => x.Symbol.Value))}");
                }

                // The moneyness filters partition the chain around the current future price, and match the strike bounds for a single right
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

                // Buy the out of the money call closest to the future price
                if (!_traded)
                {
                    var contract = otm.CallsOnly().OrderBy(x => x.Strike).FirstOrDefault();
                    if (contract != null)
                    {
                        MarketOrder(contract.Symbol, 1);
                        _traded = true;
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_chainSeen || !_traded)
            {
                throw new RegressionTestException($"Expected the March ES option chain ({_chainSeen}) and a trade ({_traded})");
            }
        }

        private static void AssertStrikes(OptionChain chain, string filter, params decimal[] expected)
        {
            var actual = chain.Select(x => x.Strike).OrderBy(x => x).ToList();
            if (!actual.SequenceEqual(expected.OrderBy(x => x)))
            {
                throw new RegressionTestException($"{filter}: expected strikes {string.Join(", ", expected)} but got {string.Join(", ", actual)}");
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
        public long DataPoints => 7888;

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
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1000586.08"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$6900000.00"},
            {"Lowest Capacity Asset", "ES XCZJLDR35F50|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "0.18%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "8786bed30a9a11b79580196098932f23"}
        };
    }
}
