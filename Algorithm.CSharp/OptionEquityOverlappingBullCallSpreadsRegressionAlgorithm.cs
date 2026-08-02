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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a book of two overlapping bull call debit spreads, with interleaved
    /// strikes and the same expiration, is grouped as two margin-free bull call spreads. The greedy leg-count
    /// descending matching used to carve this book into a bull call ladder plus an unmatched long, charging
    /// naked call margin for the ladder's uncovered short leg on a fully covered, defined-risk book
    /// </summary>
    public class OptionEquityOverlappingBullCallSpreadsRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
    {
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var callContracts = chain
                        .Where(contract => contract.Right == OptionRight.Call);
                    var expiry = callContracts.Min(x => x.Expiry);
                    var contracts = callContracts.Where(x => x.Expiry == expiry)
                        .DistinctBy(x => x.Strike)
                        .OrderBy(x => x.Strike)
                        .ToList();
                    if (contracts.Count < 4) return;

                    var initialMargin = Portfolio.MarginRemaining;

                    // first debit spread: long the lowest strike, short the third lowest
                    MarketOrder(contracts[0].Symbol, 1);
                    MarketOrder(contracts[2].Symbol, -1);

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BullCallSpread.Name, 1);

                    // second debit spread, its strikes interleaved with the first: long the second lowest, short the fourth lowest
                    MarketOrder(contracts[1].Symbol, 1);
                    MarketOrder(contracts[3].Symbol, -1);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    // every short call is covered by a long call at a lower strike: the book must resolve into two
                    // margin-free bull call spreads, not a bull call ladder charging naked call margin plus an orphan long
                    var bullCallSpreadsCount = Portfolio.Positions.Groups.Count(group =>
                        group.BuyingPowerModel is OptionStrategyPositionGroupBuyingPowerModel
                        && group.BuyingPowerModel.ToString() == OptionStrategyDefinitions.BullCallSpread.Name);
                    if (bullCallSpreadsCount != 2)
                    {
                        throw new RegressionTestException($"Expected two Bull Call Spread groups, found {bullCallSpreadsCount}: " +
                            string.Join(", ", Portfolio.Positions.Groups.Select(group => group.BuyingPowerModel.ToString())));
                    }

                    var expectedMarginUsage = 0m;
                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new RegressionTestException($"Unexpected margin used!: {Portfolio.TotalMarginUsed}");
                    }

                    // we paid the ask and value using the assets price
                    var priceSpreadDifference = GetPriceSpreadDifference(contracts[0].Symbol, contracts[1].Symbol,
                        contracts[2].Symbol, contracts[3].Symbol);
                    if (initialMargin != (freeMarginPostTrade + expectedMarginUsage + _paidFees - priceSpreadDifference))
                    {
                        throw new RegressionTestException("Unexpected margin remaining!");
                    }
                }
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 15023;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "199756"},
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
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$65000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAT67A|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "2.85%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "4f2d6ca65efe107133bf6baff5fe5512"}
        };
    }
}
