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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to assert that the option strategy matcher works as expected
    /// </summary>
    public class OptionEquityStrategyMatcherRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            AddEquity("SPY", Resolution.Hour);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                OptionChain chain;
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain) && Securities["SPY"].HasData)
                {
                    var contracts = chain
                        .Where(contract => contract.Right == OptionRight.Call)
                        .GroupBy(x => x.Expiry)
                        .First()
                        .OrderBy(x => x.Strike)
                        .ToList();

                    // let's setup and trade a butterfly call
                    var distanceBetweenStrikes = 2.5m;
                    var lowerCall = contracts.First();
                    var middleCall = contracts.First(contract => contract.Expiry == lowerCall.Expiry && contract.Strike == lowerCall.Strike + distanceBetweenStrikes);
                    var highestCall = contracts.First(contract => contract.Expiry == lowerCall.Expiry && contract.Strike == middleCall.Strike + distanceBetweenStrikes);

                    var initialMargin = Portfolio.MarginRemaining;
                    MarketOrder(lowerCall.Symbol, 10);
                    MarketOrder(middleCall.Symbol, -20);
                    MarketOrder(highestCall.Symbol, 10);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);

                    // let's make some trades to add some noise
                    MarketOrder(_optionSymbol.Underlying, 490);
                    freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);
                    AssertDefaultGroup(_optionSymbol.Underlying, 490);

                    LimitOrder(_optionSymbol.Underlying, 100, Securities[_optionSymbol.Underlying].AskPrice);

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);
                    AssertDefaultGroup(_optionSymbol.Underlying, 490);

                    MarketOrder(lowerCall.Symbol, 5);
                    freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);
                    AssertDefaultGroup(_optionSymbol.Underlying, 490);
                    // naked call for the lowerCall contract
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedCall.Name, 5);

                    MarketOrder(middleCall.Symbol, -5);
                    freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.CoveredCall.Name, 4);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BullCallSpread.Name, 1);
                    AssertDefaultGroup(_optionSymbol.Underlying, 90);
                    // naked call for the lowerCall contract
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedCall.Name, 4);

                    // trade some other asset
                    MarketOrder("SPY", 200);
                    freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.ButterflyCall.Name, 10);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.CoveredCall.Name, 4);
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BullCallSpread.Name, 1);
                    AssertDefaultGroup(_optionSymbol.Underlying, 90);
                    // naked call for the lowerCall contract
                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedCall.Name, 4);
                }
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 15204;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "8"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "200000"},
            {"End Equity", "199576.82"},
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
            {"Total Fees", "$36.95"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GOOCV W78ZFMEBBB2E|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "274.86%"},
            {"OrderListHash", "003871a1f5e8ed7352d41c4b66fe8944"}
        };
    }
}
