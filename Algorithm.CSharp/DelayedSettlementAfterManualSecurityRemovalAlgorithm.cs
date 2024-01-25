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

using System.Collections.Generic;
using QuantConnect.Interfaces;
using System.Linq;
using System;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that delayed cash settlement is applied even when the option contract is manually removed
    /// </summary>
    public class DelayedSettlementAfterManualSecurityRemovalAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 31);
            SetCash(100000);

            var equity = AddEquity("GOOG");

            _optionSymbol = OptionChainProvider.GetOptionContractList(equity.Symbol, Time)
                .OrderByDescending(symbol => symbol.ID.Date)
                .First(optionContract => optionContract.ID.OptionRight == OptionRight.Call);
            var option = AddOptionContract(_optionSymbol);

            option.SetSettlementModel(new DelayedSettlementModel(Option.DefaultSettlementDays, Option.DefaultSettlementTime));

            Schedule.On(DateRules.On(StartDate), TimeRules.BeforeMarketClose(_optionSymbol, 30), () =>
            {
                MarketOrder(_optionSymbol, 1);
            });

            Schedule.On(DateRules.On(StartDate), TimeRules.BeforeMarketClose(_optionSymbol, 1), () =>
            {
                RemoveOptionContract(_optionSymbol);
            });

            var expectedSettlementDate = new DateTime(2015, 12, 28);

            Schedule.On(DateRules.On(expectedSettlementDate), TimeRules.AfterMarketOpen(_optionSymbol), () =>
            {
                if (Portfolio.UnsettledCash == 0)
                {
                    throw new Exception($"Expected unsettled cash to be non-zero at {Time}");
                }
            });

            Schedule.On(DateRules.On(expectedSettlementDate), TimeRules.BeforeMarketClose(_optionSymbol), () =>
            {
                if (Portfolio.UnsettledCash != 0)
                {
                    throw new Exception($"Expected unsettled cash to be zero at {Time}");
                }
            });
        }

        public override void OnEndOfAlgorithm()
        {
            if (Transactions.OrdersCount != 2)
            {
                throw new Exception($"Expected 2 orders, found {Transactions.OrdersCount}");
            }

            if (Portfolio.Invested)
            {
                throw new Exception("Expected no holdings at end of algorithm");
            }

            if (Portfolio.UnsettledCash != 0)
            {
                throw new Exception($"Expected no unsettled cash at end of algorithm, found {Portfolio.UnsettledCash}");
            }

            if (Securities.ContainsKey(_optionSymbol))
            {
                throw new Exception($"Expected the option contract {_optionSymbol} to haven been removed from the securities at end of algorithm");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "271.453%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.692%"},
            {"Sharpe Ratio", "8.854"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.609%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-14.565"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.97"},
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$56000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.93%"},
            {"OrderListHash", "0c0f9328786b0c9e8f88d271673d16c3"}
        };
    }
}
