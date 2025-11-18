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
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity Short Jelly Roll option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityShortJellyRollRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
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
                    var contracts = chain.GroupBy(x => x.Strike)
                        .First()
                        .OrderBy(x => x.Expiry)
                        .ToList();

                    var nearPut = contracts.First(contract => contract.Right == OptionRight.Put);

                    var farPut = contracts.First(contract => contract.Right == OptionRight.Put
                        && contract.Expiry > nearPut.Expiry
                        && contract.Strike == nearPut.Strike);

                    var nearCall = contracts.Single(contract => contract.Right == OptionRight.Call
                        && contract.Expiry == nearPut.Expiry
                        && contract.Strike == nearPut.Strike);

                    var farCall = contracts.Single(contract => contract.Right == OptionRight.Call
                        && contract.Expiry == farPut.Expiry
                        && contract.Strike == nearPut.Strike);

                    var initialMargin = Portfolio.MarginRemaining;
                    MarketOrder(nearPut.Symbol, -1);
                    MarketOrder(nearCall.Symbol, +1);

                    MarketOrder(farPut.Symbol, +1);
                    MarketOrder(farCall.Symbol, -1);

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ShortJellyRoll.Name, 1);

                    var freeMarginPostTrade = Portfolio.MarginRemaining;
                    var undPrice = farPut.UnderlyingLastPrice;
                    var expectedMarginUsage = 18530.8m;
                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new Exception($"Unexpect margin used!:{Portfolio.TotalMarginUsed}");
                    }

                    // we payed the ask and value using the assets price
                    var priceSpreadDifference = GetPriceSpreadDifference(nearPut.Symbol, nearCall.Symbol, farPut.Symbol, farCall.Symbol);
                    if (initialMargin != (freeMarginPostTrade + expectedMarginUsage + _paidFees - priceSpreadDifference))
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
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "199741"},
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
            {"Estimated Strategy Capacity", "$110000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAT67A|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "4.70%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "858390f60e75ee2a501d9570ad37a925"}
        };
    }
}
