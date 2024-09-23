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
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity Short Iron Butterfly option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityShortIronButterflyRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
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
                    var contracts = chain.GroupBy(x => x.Expiry)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    var outOfTheMoneyPut = contracts.Where(contract => contract.Right == OptionRight.Put)
                        .OrderBy(contract => contract.Strike)
                        .Skip(1)
                        .First();

                    var atTheMoneyPut = contracts.OrderBy(contract => Math.Abs(contract.Strike - chain.Underlying.Price))
                        .First(contract => contract.Right == OptionRight.Put
                        && contract.Expiry == outOfTheMoneyPut.Expiry);

                    var atTheMoneyCall = contracts.Single(contract => contract.Right == OptionRight.Call
                        && contract.Expiry == outOfTheMoneyPut.Expiry
                        && contract.Strike == atTheMoneyPut.Strike);

                    var outOfTheMoneyCall = contracts.Single(contract => contract.Right == OptionRight.Call
                        && contract.Expiry == outOfTheMoneyPut.Expiry
                        && contract.Strike == atTheMoneyPut.Strike * 2 - outOfTheMoneyPut.Strike);

                    var initialMargin = Portfolio.MarginRemaining;
                    MarketOrder(outOfTheMoneyPut.Symbol, -10);
                    MarketOrder(atTheMoneyPut.Symbol, +10);

                    MarketOrder(atTheMoneyCall.Symbol, +10);
                    MarketOrder(outOfTheMoneyCall.Symbol, -10);

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ShortIronButterfly.Name, 10);

                    var freeMarginPostTrade = Portfolio.MarginRemaining;
                    var expectedMarginUsage = 0;
                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new Exception("Unexpect margin used!");
                    }

                    // we payed the ask and value using the assets price
                    var priceSpreadDifference = GetPriceSpreadDifference(outOfTheMoneyPut.Symbol, atTheMoneyPut.Symbol,
                        atTheMoneyCall.Symbol, outOfTheMoneyCall.Symbol);
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
            {"End Equity", "197174"},
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
            {"Total Fees", "$26.00"},
            {"Estimated Strategy Capacity", "$150000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMML01JA|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "26.63%"},
            {"OrderListHash", "2e8aabda630eb75675b202456d2b085a"}
        };
    }
}
