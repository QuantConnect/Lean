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
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity Short Put Backspread option strategy and asserting it's being detected by Lean and works as expected
    /// </summary>
    public class OptionEquityShortPutBackspreadRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
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
                    var putContracts = chain
                        .Where(contract => contract.Right == OptionRight.Put);
                    var expiry = putContracts.Min(x => x.Expiry);
                    putContracts = putContracts.Where(x => x.Expiry == expiry)
                        .OrderBy(x => x.Strike)
                        .ToList();

                    var strike = putContracts.Select(x => x.Strike).Distinct();
                    if (strike.Count() < 2) return;

                    var lowStrikePut = putContracts.First();
                    var highStrikePut = putContracts.First(contract => contract.Strike > lowStrikePut.Strike && contract.Expiry == lowStrikePut.Expiry);

                    var initialMargin = Portfolio.MarginRemaining;

                    var optionStrategy = OptionStrategies.ShortPutBackspread(_optionSymbol, highStrikePut.Strike, lowStrikePut.Strike, expiry);
                    Buy(optionStrategy, 5);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    // It is a combination of bear put spread and naked put
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BearPutSpread.Name, 5);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedPut.Name, 5);

                    // Should only involve the naked put part
                    var security = Securities[lowStrikePut.Symbol];
                    var expectedMarginUsage = security.BuyingPowerModel.GetMaintenanceMargin(MaintenanceMarginParameters.ForQuantityAtCurrentPrice(security, -5)).Value;

                    if (expectedMarginUsage != Portfolio.TotalMarginUsed)
                    {
                        throw new Exception("Unexpect margin used!");
                    }

                    // we payed the ask and value using the assets price
                    var priceLadderDifference = GetPriceSpreadDifference(lowStrikePut.Symbol, highStrikePut.Symbol);
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
            {"End Equity", "199165.25"},
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
            {"Estimated Strategy Capacity", "$1200000.00"},
            {"Lowest Capacity Asset", "GOOCV 306CZL2DIL4G6|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "8.84%"},
            {"OrderListHash", "7294da06231632975e97c57721d26442"}
        };
    }
}
