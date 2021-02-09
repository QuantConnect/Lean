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
            SetStartDate(2020, 1, 5);
            SetEndDate(2020, 1, 6);

            var goldFutures = AddFuture("GC", Resolution.Minute, Market.COMEX);
            goldFutures.SetFilter(0, 365);

            AddFutureOption(goldFutures.Symbol);
        }

        public override void OnData(Slice data)
        {
            foreach (var symbol in data.QuoteBars.Keys)
            {
                if (_expectedSymbols.ContainsKey(symbol))
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-8.289%"},
            {"Drawdown", "3.500%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.047%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-14.395"},
            {"Tracking Error", "0.043"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$7.40"},
            {"Fitness Score", "0.019"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-194.237"},
            {"Portfolio Turnover", "0.038"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "979e3995c0dbedc46eaf3705e0438bf5"}
        };
    }
}
