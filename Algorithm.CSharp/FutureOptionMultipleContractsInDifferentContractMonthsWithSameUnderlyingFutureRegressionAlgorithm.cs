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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression test tests for the loading of futures options contracts with a contract month of 2020-03 can live
    /// and be loaded from the same ZIP file that the 2020-04 contract month Future Option contract lives in.
    /// </summary>
    public class FutureOptionMultipleContractsInDifferentContractMonthsWithSameUnderlyingFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Dictionary<Symbol, bool> _expectedSymbols = new Dictionary<Symbol, bool>
        {
            { CreateOption(new DateTime(2020, 3, 26), OptionRight.Call, 1650), false },
            { CreateOption(new DateTime(2020, 3, 26), OptionRight.Put, 1540), false },
            { CreateOption(new DateTime(2020, 2, 25), OptionRight.Call, 1600), false },
            { CreateOption(new DateTime(2020, 2, 25), OptionRight.Put, 1545), false }
        };

        public override void Initialize()
        {
            // Required for FOPs to use extended hours, until GH #6491 is addressed
            UniverseSettings.ExtendedMarketHours = true;

            SetStartDate(2020, 1, 4);
            SetEndDate(2020, 1, 6);

            var goldFutures = AddFuture("GC", Resolution.Minute, Market.COMEX, extendedMarketHours: true);
            goldFutures.SetFilter(0, 365);

            AddFutureOption(goldFutures.Symbol);
        }

        public override void OnData(Slice data)
        {
            foreach (var symbol in data.QuoteBars.Keys)
            {
                // Check that we are in regular hours, we can place a market order (on extended hours, limit orders should be used)
                if (_expectedSymbols.ContainsKey(symbol) && IsInRegularHours(symbol))
                {
                    var invested = _expectedSymbols[symbol];
                    if (!invested)
                    {
                        MarketOrder(symbol, 1);
                    }

                    _expectedSymbols[symbol] = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var notEncountered = _expectedSymbols.Where(kvp => !kvp.Value).ToList();
            if (notEncountered.Any())
            {
                throw new Exception($"Expected all Symbols encountered and invested in, but the following were not found: {string.Join(", ", notEncountered.Select(kvp => kvp.Value.ToStringInvariant()))}");
            }
            if (!Portfolio.Invested)
            {
                throw new Exception("Expected holdings at the end of algorithm, but none were found.");
            }
        }

        private bool IsInRegularHours(Symbol symbol)
        {
            return Securities[symbol].Exchange.ExchangeOpen;
        }

        private static Symbol CreateOption(DateTime expiry, OptionRight optionRight, decimal strikePrice)
        {
            return QuantConnect.Symbol.CreateOption(
                QuantConnect.Symbol.CreateFuture("GC", Market.COMEX, new DateTime(2020, 4, 28)),
                Market.COMEX,
                OptionStyle.American,
                optionRight,
                strikePrice,
                expiry);
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
        public long DataPoints => 24379;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-25.338%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99760.12"},
            {"Net Profit", "-0.240%"},
            {"Sharpe Ratio", "-10.528"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.09"},
            {"Beta", "-0.629"},
            {"Annual Standard Deviation", "0.027"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-12.58"},
            {"Tracking Error", "0.07"},
            {"Treynor Ratio", "0.451"},
            {"Total Fees", "$9.88"},
            {"Estimated Strategy Capacity", "$31000000.00"},
            {"Lowest Capacity Asset", "OG 31BFX0QKBVPGG|GC XE1Y0ZJ8NQ8T"},
            {"Portfolio Turnover", "2.65%"},
            {"OrderListHash", "82e3ec4837c53db0254b0e6329d1937b"}
        };
    }
}
