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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    public class BuyBTCWithLessThanOneDollarAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 1);
            SetEndDate(2013, 10, 1);
            SetCash(1);
            SetBrokerageModel(Brokerages.BrokerageName.Bitfinex, AccountType.Cash);
            AddCrypto("BTCUSD", Resolution.Minute, Market.Bitfinex);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // Place an order that would fail because of the size
                var invalidOrder = MarketOnOpenOrder("BTCUSD", 0.00002);
                if (invalidOrder.Status != OrderStatus.Invalid)
                {
                    throw new Exception("Invalid order expected, order size is less than allowed");
                }

                // Update an order that fails because of the size
                var validOrder = MarketOnOpenOrder("BTCUSD", 0.0002);
                var invalidUpdate = validOrder.Update(new UpdateOrderFields()
                {
                    Quantity = 0.00002m
                });

                if (invalidUpdate.IsSuccess)
                {
                    throw new Exception("Update was expected to fail");
                }

                // Place and update an order that will succeed
                validOrder = MarketOnOpenOrder("BTCUSD", 0.0002);
                var validUpdate = validOrder.Update(new UpdateOrderFields()
                {
                    Quantity = 0.002m
                });

                if (!validUpdate.IsSuccess)
                {
                    throw new Exception("Update was expected to succeed");
                }

                // Order to fill the portfolio
                var closeOrder = MarketOrder("BTCUSD", 0.0002);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "BTCUSD E3"},
            {"Fitness Score", "0.012"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-433.212"},
            {"Portfolio Turnover", "0.025"},
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
            {"OrderListHash", "c880ad4820a90e5e48c22f6fe5a5c7f1"}
        };
    }
}
