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
    /// Regression algorithm exercising an equity Bear Call Spread option strategy using SetHoldings and asserting it's being detected by Lean and works as expected
    /// </summary>
    /// <remarks>SetHoldings percentage calculation is using the default position group so it fails to determine the correct value,
    /// either way Lean has to detect the option strategy been executed and margin used has to get reduced</remarks>
    public class OptionEquityBearCallSpreadSetHoldingsRegressionAlgorithm : OptionEquityBaseStrategyRegressionAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            SetCash(1000000);
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
                if (IsMarketOpen(_optionSymbol) && slice.OptionChains.TryGetValue(_optionSymbol, out chain))
                {
                    var callContracts = chain
                        .Where(contract => contract.Right == OptionRight.Call)
                        .OrderByDescending(x => x.Expiry)
                        .ThenBy(x => x.Strike)
                        .ToList();

                    var shortCall = callContracts.First();
                    var longCall = callContracts.First(contract => contract.Strike > shortCall.Strike && contract.Expiry == shortCall.Expiry);

                    SetHoldings(shortCall.Symbol, -0.05m);
                    var freeMargin = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.NakedCall.Name,
                        (int)Math.Abs(Securities[shortCall.Symbol].Holdings.Quantity));

                    SetHoldings(longCall.Symbol, +0.05m);
                    var freeMarginPostTrade = Portfolio.MarginRemaining;

                    AssertOptionStrategyIsPresent(OptionStrategyDefinitions.BearCallSpread.Name);

                    if (freeMargin >= freeMarginPostTrade)
                    {
                        throw new Exception("We expect the margin used to actually be lower once we perform the second trade");
                    }
                }
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 471135;

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
            {"Start Equity", "1000000"},
            {"End Equity", "998807.85"},
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
            {"Total Fees", "$7.15"},
            {"Estimated Strategy Capacity", "$33000000.00"},
            {"Lowest Capacity Asset", "GOOCV WBGM95TAH2LI|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "6.17%"},
            {"OrderListHash", "8f1288896dafb2856b6045f8930e86a6"}
        };
    }
}
