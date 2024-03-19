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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm illustrating how to request history data for continuous contracts with different depth offsets.
    /// </summary>
    public class HistoryWithDifferentContinuousContractDepthOffsetsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _continuousContractSymbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2014, 1, 1);
            _continuousContractSymbol = AddFuture(Futures.Indices.SP500EMini, Resolution.Daily).Symbol;
        }

        public override void OnEndOfAlgorithm()
        {
            var contractDepthOffsets = Enumerable.Range(0, 3).ToList();
            var historyResults = contractDepthOffsets.Select(contractDepthOffset =>
            {
                return History(new [] { _continuousContractSymbol }, StartDate, EndDate, Resolution.Daily, contractDepthOffset: contractDepthOffset).ToList();
            }).ToList();

            if (historyResults.Any(x => x.Count == 0 || x.Count != historyResults[0].Count))
            {
                throw new Exception("History results are empty or bar counts did not match");
            }

            // Check that all history results at least one mapping and that different contracts are used for each offset (which can be checked by
            // comparing the underlying symbols)
            List<HashSet<Symbol>> underlyingsPerHistory = new();
            for (int i = 0; i < historyResults.Count; i++)
            {
                HashSet<Symbol> underlyings = new();

                foreach (var slice in historyResults[i])
                {
                    var underlying = slice.Keys.Single().Underlying;

                    if (underlyings.Add(underlying) && underlyings.Count > 1)
                    {
                        var currentExpiration = underlying.ID.Date;
                        var frontMonthExpiration = FuturesExpiryFunctions.FuturesExpiryFunction(_continuousContractSymbol)(slice.Time.AddMonths(1));

                        if (contractDepthOffsets[i] == 0)   // Front month
                        {
                            if (currentExpiration != frontMonthExpiration.Date)
                            {
                                throw new Exception($"Unexpected current mapped contract expiration {currentExpiration}" +
                                    $" @ {Time} it should be AT front month expiration {frontMonthExpiration}");
                            }
                        }
                        else    // Back month
                        {
                            if (currentExpiration <= frontMonthExpiration.Date)
                            {
                                throw new Exception($"Unexpected current mapped contract expiration {currentExpiration}" +
                                    $" @ {Time} it should be AFTER front month expiration {frontMonthExpiration}");
                            }
                        }
                    }
                }

                if (underlyings.Count == 0)
                {
                    throw new Exception($"History results for contractDepthOffset={contractDepthOffsets[i]} did not contain any mappings");
                }

                underlyingsPerHistory.Add(underlyings);
            }

            // Check that underlyings are different for each history result (because we're using different contract depth offsets)
            for (int i = 0; i < underlyingsPerHistory.Count; i++)
            {
                for (int j = i + 1; j < underlyingsPerHistory.Count; j++)
                {
                    if (underlyingsPerHistory[i].SetEquals(underlyingsPerHistory[j]))
                    {
                        throw new Exception($"History results for contractDepthOffset={contractDepthOffsets[i]} and {contractDepthOffsets[j]} contain the same underlying");
                    }
                }
            }

            // Check that prices at each time are different for different contract depth offsets
            for (int j = 0; j < historyResults[0].Count; j++)
            {
                var closePrices = historyResults.Select(hr => hr[j].Bars.Values.SingleOrDefault(new TradeBar()).Close).ToHashSet();
                if (closePrices.Count != contractDepthOffsets.Count)
                {
                    throw new Exception($"History results close prices should have been different for each contract depth offset at each time");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1578;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 366;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
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
            {"Information Ratio", "-3.681"},
            {"Tracking Error", "0.086"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
