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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating that option chains can be filtered with the same filters used for
    /// option universe selection, both on chains from <see cref="QCAlgorithm.OptionChain(Symbol, bool)"/>
    /// and on the chains delivered in the slice
    /// </summary>
    public class OptionChainFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _option;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var option = AddOption("GOOG");
            _option = option.Symbol;
            // The same words select the universe and, below, narrow down the chains
            option.SetFilter(universe => universe.CallsOnly().Expiration(1, 10).Strikes(-2, 2));

            var chain = OptionChain(_option);
            if (chain.Count == 0)
            {
                throw new RegressionTestException("Expected a non empty option chain");
            }
            // The relative strikes filter needs the underlying price, chains built from universe data must carry it
            if (chain.Underlying.Price == 0)
            {
                throw new RegressionTestException("Expected the chain to carry the underlying price");
            }

            var totalContracts = chain.Count;
            var filtered = chain.CallsOnly().Expiration(1, 10).Strikes(-2, 2);
            // GOOG closed at 748.54 on 2015-12-23 and the only expiration 1 to 10 days out is 2015-12-31,
            // so the two strikes below the spot and the two at or above it are 745, 747.5, 750 and 752.5
            AssertContracts(filtered, OptionRight.Call, new DateTime(2015, 12, 31), new[] { 745m, 747.5m, 750m, 752.5m });
            if (chain.Count != totalContracts)
            {
                throw new RegressionTestException("Filters must not modify the source chain");
            }

            // Front month is the nearest expiration, 2015-12-24 itself
            AssertContracts(chain.PutsOnly().FrontMonth(), OptionRight.Put, new DateTime(2015, 12, 24));

            // Standard contracts expire on the third Friday, weeklys do not
            var standards = chain.StandardsOnly().FrontMonth();
            if (standards.Count == 0 || standards.Any(x => x.Expiry != new DateTime(2016, 1, 15)))
            {
                throw new RegressionTestException("Expected the standard front month to expire on 2016-01-15");
            }
            var weeklys = chain.WeeklysOnly();
            if (weeklys.Count == 0 || weeklys.Any(x => OptionSymbol.IsStandard(x.Symbol)))
            {
                throw new RegressionTestException("Expected only weekly contracts");
            }

            // Greeks filters use the greeks the chain carries
            var deltas = chain.Delta(0.5m, 0.6m);
            var expectedDeltas = chain.Count(x => x.Greeks.Delta >= 0.5m && x.Greeks.Delta <= 0.6m);
            if (deltas.Count == 0 || deltas.Count != expectedDeltas || deltas.Any(x => x.Greeks.Delta < 0.5m || x.Greeks.Delta > 0.6m))
            {
                throw new RegressionTestException("Delta filter mismatch");
            }
        }

        public override void OnData(Slice slice)
        {
            if (_traded || !slice.OptionChains.TryGetValue(_option, out var chain))
            {
                return;
            }

            // The universe only selected calls expiring 1 to 10 days out, so the chain filters agree with it
            if (chain.CallsOnly().Expiration(1, 10).Count != chain.Count || chain.PutsOnly().Count != 0)
            {
                throw new RegressionTestException("Slice chain filters disagree with the universe filter");
            }

            // Buy the call at the first strike at or above the underlying price
            var contract = chain.Strikes(0, 0).FirstOrDefault();
            if (contract != null)
            {
                MarketOrder(contract.Symbol, 1);
                _traded = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_traded)
            {
                throw new RegressionTestException("Expected to trade a contract selected from the slice option chain");
            }
        }

        private static void AssertContracts(OptionChain chain, OptionRight right, DateTime expiry, decimal[] strikes = null)
        {
            if (chain.Count == 0 || chain.Any(x => x.Right != right || x.Expiry != expiry))
            {
                throw new RegressionTestException($"Expected only {right} contracts expiring on {expiry:yyyy-MM-dd}");
            }
            if (strikes != null && !chain.Select(x => x.Strike).OrderBy(x => x).SequenceEqual(strikes))
            {
                throw new RegressionTestException($"Unexpected strikes: {string.Join(", ", chain.Select(x => x.Strike))}");
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
        public long DataPoints => 7080;

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
            {"Start Equity", "100000"},
            {"End Equity", "99764"},
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
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$36000.00"},
            {"Lowest Capacity Asset", "GOOCV W6U7P9WYPQVA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.73%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d505d8b11141dfd2d54b74ac20e39268"}
        };
    }
}
