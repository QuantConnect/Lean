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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm demonstrating the option chain selection helpers:
    /// <see cref="Data.Market.OptionChain.Select"/>, <see cref="Data.Market.OptionChain.ClosestExpiry"/>,
    /// <see cref="Data.Market.OptionChain.At"/>, <see cref="Data.Market.OptionChain.AtTheMoney"/> and
    /// <see cref="Data.Market.OptionChain.Strikes"/>, which replace the usual hand-rolled
    /// sorted-comprehension contract selection with a single call.
    /// </summary>
    public class OptionChainSelectionHelpersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionContract;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);

            var goog = AddEquity("GOOG").Symbol;
            var chain = OptionChain(goog);

            // One-line selection: the call at the expiry closest to 10 days out with the strike closest
            // to the underlying price (at the money is the default when no moneyness/delta is given)
            var contract = chain.Select(right: OptionRight.Call, targetDte: 10);
            if (contract == null)
            {
                throw new RegressionTestException("Select(right, targetDte) returned no contract");
            }

            // The equivalent hand-rolled ceremony must select the very same contract
            var spot = chain.Underlying.Price;
            var calls = chain.Where(x => x.Right == OptionRight.Call).ToList();
            var ceremonyExpiry = calls.Select(x => x.Expiry).Distinct()
                .OrderBy(expiry => Math.Abs((expiry.Date - Time.Date).Days - 10))
                .First();
            var ceremonyContract = calls.Where(x => x.Expiry == ceremonyExpiry)
                .OrderBy(x => Math.Abs(x.Strike - spot))
                .First();
            if (!contract.Symbol.Equals(ceremonyContract.Symbol))
            {
                throw new RegressionTestException($"Select() mismatch: {contract.Symbol.Value} != ceremony {ceremonyContract.Symbol.Value}");
            }
            // 2015-12-24: GOOG at 748.40, closest expiry to 10 days out is 2015-12-31, ATM strike is 747.50
            if (contract.Expiry != new DateTime(2015, 12, 31) || contract.Strike != 747.5m)
            {
                throw new RegressionTestException($"Unexpected contract selected: {contract.Symbol.Value}");
            }

            // Expiry selection with a DTE window: 2015-12-31 (7 days out) is excluded by minDte,
            // so the closest expiry to 10 days out is 2016-01-08
            var expiry = chain.ClosestExpiry(targetDte: 10, minDte: 8, maxDte: 40);
            if (expiry != new DateTime(2016, 1, 8))
            {
                throw new RegressionTestException($"ClosestExpiry() expected 2016-01-08 but got {expiry}");
            }

            // Single-expiry view: composes with Calls/Puts, Strikes and AtTheMoney
            var atExpiry = chain.At(contract.Expiry);
            if (atExpiry.Count == 0 || atExpiry.Any(x => x.Expiry != contract.Expiry))
            {
                throw new RegressionTestException("At() returned contracts of other expiries");
            }
            if (atExpiry.Calls.Count == 0 || atExpiry.Puts.Count == 0)
            {
                throw new RegressionTestException("At().Calls/.Puts should not be empty");
            }
            var atmPut = atExpiry.AtTheMoney(OptionRight.Put);
            if (atmPut == null || atmPut.Strike != 747.5m || atmPut.Right != OptionRight.Put)
            {
                throw new RegressionTestException($"AtTheMoney(Put) expected the 747.50 put but got {atmPut?.Symbol.Value}");
            }

            // Strikes helpers: strictly above/below and closest to the underlying price
            var strikes = atExpiry.Strikes;
            if (strikes.ClosestTo(spot) != 747.5m || strikes.FirstAbove(spot) != 750m || strikes.FirstBelow(spot) != 747.5m)
            {
                throw new RegressionTestException(
                    $"Strikes helpers mismatch: {strikes.ClosestTo(spot)}/{strikes.FirstAbove(spot)}/{strikes.FirstBelow(spot)}");
            }

            // Delta targeting: the put with |delta| closest to 0.35, using the universe pre-calculated greeks
            var deltaPut = chain.Select(right: OptionRight.Put, targetDte: 7, targetDelta: 0.35m);
            var ceremonyDeltaPut = chain
                .Where(x => x.Right == OptionRight.Put && x.Expiry == contract.Expiry && x.Greeks.Delta != 0)
                .OrderBy(x => Math.Abs(Math.Abs(x.Greeks.Delta) - 0.35m))
                .First();
            if (deltaPut == null || !deltaPut.Symbol.Equals(ceremonyDeltaPut.Symbol))
            {
                throw new RegressionTestException($"Select(targetDelta) mismatch: {deltaPut?.Symbol.Value} != {ceremonyDeltaPut.Symbol.Value}");
            }

            // The helpers are null-safe: no match returns null instead of throwing like min()/First() would
            if (chain.Select(right: OptionRight.Call, minDte: 2000) != null ||
                chain.ClosestExpiry(minDte: 2000) != null ||
                chain.At(new DateTime(2050, 1, 1)).Count != 0)
            {
                throw new RegressionTestException("Helpers should return null/empty when nothing matches");
            }

            _optionContract = AddOptionContract(contract.Symbol).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && slice.OptionChains.TryGetValue(_optionContract.Canonical, out var chain))
            {
                // Same one-liner against the slice option chain
                var contract = chain.Select(right: OptionRight.Call, targetDte: 7);
                if (contract != null)
                {
                    MarketOrder(contract.Symbol, 1);
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!Portfolio.Invested)
            {
                throw new RegressionTestException("Expected to select and buy a contract from the slice option chain");
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
        public long DataPoints => 1051;

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
            {"End Equity", "99769"},
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
            {"Estimated Strategy Capacity", "$47000.00"},
            {"Lowest Capacity Asset", "GOOCV W6U7Q7WSA9ZA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "0.86%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "f57c16766cc7f8eb3d65d6c91457529e"}
        };
    }
}
