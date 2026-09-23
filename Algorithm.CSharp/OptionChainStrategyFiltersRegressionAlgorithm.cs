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
    /// Regression algorithm demonstrating that the option strategy filters of the universe selection, like
    /// <see cref="Securities.BaseOptionFilterUniverse{TUniverse, TData}.Straddle"/> or
    /// <see cref="Securities.BaseOptionFilterUniverse{TUniverse, TData}.IronCondor"/>, select the strategy legs
    /// straight from an option chain too
    /// </summary>
    public class OptionChainStrategyFiltersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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
            // The universe selects the straddle legs, the same filter picks them again from the slice chain below
            option.SetFilter(universe => universe.Straddle(7));

            var chain = OptionChain(_option);
            var expiry = new DateTime(2015, 12, 31);

            // GOOG closed at 748.54 on 2015-12-23, the first expiry at least 7 days out is 2015-12-31 and the ATM strike is 747.50
            var straddle = chain.Straddle(7);
            AssertLegs(straddle, expiry, (OptionRight.Call, 747.5m), (OptionRight.Put, 747.5m));

            // Iron condor: near legs 5 away from the spot, far legs 10 away
            var ironCondor = chain.IronCondor(7, 5, 10);
            AssertLegs(ironCondor, expiry, (OptionRight.Put, 737.5m), (OptionRight.Put, 742.5m), (OptionRight.Call, 752.5m), (OptionRight.Call, 757.5m));

            // Single contract and vertical spread pickers
            AssertLegs(chain.NakedPut(7, -5), expiry, (OptionRight.Put, 742.5m));
            AssertLegs(chain.CallSpread(7, 5), expiry, (OptionRight.Call, 742.5m), (OptionRight.Call, 752.5m));

            // Calendar spread: same strike, expiries at least 7 and 14 days out
            var calendar = chain.CallCalendarSpread(0, 7, 14);
            if (calendar.Count != 2 || calendar.Any(x => x.Right != OptionRight.Call || x.Strike != 747.5m)
                || !calendar.Select(x => x.Expiry).OrderBy(x => x).SequenceEqual(new[] { expiry, new DateTime(2016, 1, 8) }))
            {
                throw new RegressionTestException($"Unexpected calendar spread legs: {string.Join(", ", calendar.Select(x => x.Symbol.Value))}");
            }

            // No match selects nothing instead of throwing
            if (chain.Straddle(1000).Count != 0)
            {
                throw new RegressionTestException("Expected no legs for an expiry out of the chain");
            }

            // Invalid arguments are rejected like the universe filters do
            try
            {
                chain.Strangle(7, -5, 5);
                throw new RegressionTestException("Expected Strangle() to reject a negative call strike distance");
            }
            catch (ArgumentException)
            {
            }
        }

        public override void OnData(Slice slice)
        {
            if (_traded || !slice.OptionChains.TryGetValue(_option, out var chain))
            {
                return;
            }

            // The same filter that selected the universe picks the legs from the slice chain
            var legs = chain.Straddle(7);
            if (legs.Count == 2)
            {
                var leg = legs.First();
                Buy(OptionStrategies.Straddle(_option, leg.Strike, leg.Expiry), 1);
                _traded = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_traded)
            {
                throw new RegressionTestException("Expected to trade the straddle selected from the slice option chain");
            }
        }

        private static void AssertLegs(OptionChain legs, DateTime expiry, params (OptionRight right, decimal strike)[] expected)
        {
            var actual = legs.Select(x => (x.Right, x.Strike)).OrderBy(x => x.Right).ThenBy(x => x.Strike).ToList();
            if (legs.Any(x => x.Expiry != expiry) || !actual.SequenceEqual(expected.OrderBy(x => x.right).ThenBy(x => x.strike)))
            {
                throw new RegressionTestException($"Unexpected legs: {string.Join(", ", legs.Select(x => x.Symbol.Value))}");
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
        public long DataPoints => 5886;

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
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99638"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$23000.00"},
            {"Lowest Capacity Asset", "GOOCV 305Y7VNVZK3D2|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "1.55%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "0918e55ec2074aaafad98475aa2fcc43"}
        };
    }
}
