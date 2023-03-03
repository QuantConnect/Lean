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
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Shows how setting to use the SecurityMarginModel.Null (or BuyingPowerModel.Null)
    /// to disable the sufficient margin call verification.
    /// See also: <see cref="OptionEquityBullCallSpreadRegressionAlgorithm"/>
    /// </summary>
    /// <meta name="tag" content="reality model" />
    public class NullBuyingPowerOptionBullCallSpreadAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(200000);

            SetSecurityInitializer(security => security.MarginModel = SecurityMarginModel.Null);

            var equity = AddEquity("GOOG");
            var option = AddOption(equity.Symbol);
            _optionSymbol = option.Symbol;

            option.SetFilter(-2, +2, 0, 180);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested && IsMarketOpen(_optionSymbol) &&
                slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                var callContracts = chain
                    .Where(contract => contract.Right == OptionRight.Call).ToList();

                var expiry = callContracts.Min(x => x.Expiry);

                callContracts = callContracts
                    .Where(x => x.Expiry == expiry)
                    .OrderBy(x => x.Strike)
                    .ToList();

                var longCall = callContracts.First();
                var shortCall = callContracts.First(contract => contract.Strike > longCall.Strike);

                const int quantity = 1000;

                var tickets = new[]
                {
                    MarketOrder(shortCall.Symbol, -quantity),
                    MarketOrder(longCall.Symbol, quantity)
                };

                foreach (var ticket in tickets)
                {
                    if (ticket.Status != OrderStatus.Filled)
                    {
                        throw new Exception($"There should be no restriction on buying {ticket.Quantity} of {ticket.Symbol} with BuyingPowerModel.Null");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.TotalMarginUsed != 0)
            {
                throw new Exception("The TotalMarginUsed should be zero to avoid margin calls.");
            }
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
        public long DataPoints => 475788;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"Total Fees", "$500.00"},
            {"Estimated Strategy Capacity", "$36000.00"},
            {"Lowest Capacity Asset", "GOOCV W78ZERHAOVVQ|GOOCV VP83T1ZUHROL"},
            {"Return Over Maximum Drawdown", "0"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "5c32a1e90845e321ae1cc8b893353acd"}
        };
    }
}
