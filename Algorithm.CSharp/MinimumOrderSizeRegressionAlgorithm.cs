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
    /// <summary>
    /// This algorithm asserts that the minimum order size is respected at the moment of
    /// place an order or update an order
    /// </summary>
    public class MinimumOrderSizeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 1);
            SetEndDate(2013, 10, 1);
            SetBrokerageModel(Brokerages.BrokerageName.Bitfinex, AccountType.Cash);
            AddCrypto("BTCUSD", Resolution.Hour);
        }

        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                // Place an order that will fail because of the size
                var invalidOrder = MarketOnOpenOrder("BTCUSD", 0.00002);
                if (invalidOrder.Status != OrderStatus.Invalid)
                {
                    throw new Exception("Invalid order expected, order size is less than allowed");
                }

                // Update an order that fails because of the size
                var validOrderOne = MarketOnOpenOrder("BTCUSD", 0.0002, "NotUpdated");
                validOrderOne.Update(new UpdateOrderFields()
                {
                    Quantity = 0.00002m,
                    Tag = "Updated"
                });

                // Place and update an order that will succeed
                var validOrderTwo = MarketOnOpenOrder("BTCUSD", 0.0002, "NotUpdated");
                validOrderTwo.Update(new UpdateOrderFields()
                {
                    Quantity = 0.002m,
                    Tag = "Updated"
                });
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);

            // Update of validOrderOne is expected to fail
            if( (order.Id == 2) && (order.LastUpdateTime != null) && (order.Tag == "Updated"))
            {
                throw new Exception("Order update expected to fail");
            }

            // Update of validOrdertwo is expected to succeed
            if ((order.Id == 3) && (order.LastUpdateTime != null) && (order.Tag == "NotUpdated"))
            {
                throw new Exception("Order update expected to succeed");
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
            {"Total Trades", "40"},
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
            {"Total Fees", "$0.01"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "BTCUSD E3"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
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
            {"OrderListHash", "dda3d03e8154ae0aad7ee17bdfd306cb"}
        };
    }
}
