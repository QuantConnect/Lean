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
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity Long Call Backspread option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityCallBackspreadRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
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
                    callContracts = callContracts.Where(x => x.Expiry == expiry)
                        .OrderBy(x => x.Strike)
                        .ToList();

                    var strike = callContracts.Select(x => x.Strike).Distinct();
                    if (strike.Count() < 2) return;

                    var lowStrikeCall = callContracts.First();
                    var highStrikeCall = callContracts.First(contract => contract.Strike > lowStrikeCall.Strike && contract.Expiry == lowStrikeCall.Expiry);

                    var initialMargin = Portfolio.MarginRemaining;

                    MarketOrder(lowStrikeCall.Symbol, -5);
                    MarketOrder(highStrikeCall.Symbol, 10);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    // It is a combination of bear call spread and long call
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BearCallSpread.Name, 5);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedCall.Name, 5);

                    // Should only involve the bear call spread part
                    var expectedMarginUsage = (highStrikeCall.Strike - lowStrikeCall.Strike) * Securities[highStrikeCall.Symbol].SymbolProperties.ContractMultiplier * 5;

                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new Exception($"Unexpect margin used!:{Portfolio.TotalMarginUsed}");
                    }

                    // we payed the ask and value using the assets price
                    var priceLadderDifference = GetPriceSpreadDifference(lowStrikeCall.Symbol, highStrikeCall.Symbol);
                    if (initialMargin != (freeMarginPostTrade + expectedMarginUsage + _paidFees - priceLadderDifference))
                    {
                        throw new Exception("Unexpect margin remaining!");
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
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "198565.25"},
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
            {"Total Fees", "$9.75"},
            {"Estimated Strategy Capacity", "$47000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "11.81%"},
            {"OrderListHash", "6dbe3ae68d040c89feb283adeea5ee4c"}
        };
    }
}
