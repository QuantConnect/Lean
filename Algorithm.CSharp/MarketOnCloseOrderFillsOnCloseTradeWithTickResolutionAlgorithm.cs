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

using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that MarketOnClose orders are filled with official close price.
    /// </summary>
    public class MarketOnCloseOrderFillsOnCloseTradeWithTickResolutionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 8);
            SetCash(1000000);

            _symbol = AddEquity("SPY", Resolution.Tick, extendedMarketHours: true, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;

            Schedule.On(DateRules.EveryDay(_symbol),
                TimeRules.BeforeMarketClose(_symbol, 20),
                () => MarketOnCloseOrder(_symbol, 1));
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Debug(orderEvent.ToString());

                if (orderEvent.Message != "")
                {
                    throw new Exception($"OrderEvent.Message should be empty, but is '{orderEvent.Message}'");
                }

                var order = Transactions.GetOrderById(orderEvent.OrderId);
                if (order.Tag != "")
                {
                    throw new Exception($"Order.Tag should be empty, but is '{order.Tag}'");
                }

                var expectedFillPrice = orderEvent.UtcTime.Date == StartDate.Date ? 167.42m : 165.48m;
                if (orderEvent.FillPrice != expectedFillPrice)
                {
                    throw new Exception(
                        $"Expected {orderEvent.UtcTime.Date} order fill price to be {expectedFillPrice} but was {orderEvent.FillPrice}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var orders = Transactions.GetOrders().ToList();

            // We expect 2 orders, one for each day
            var expectedOrdersCount = 2;
            if (orders.Count != expectedOrdersCount)
            {
                throw new Exception($"Expected {expectedOrdersCount} orders, but found {orders.Count}");
            }

            if (orders.Any(x => x.Status != OrderStatus.Filled))
            {
                throw new Exception(
                    $"Expected all orders to be filled, but found {orders.Count(x => x.Status != OrderStatus.Filled)} unfilled orders");
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
        public long DataPoints => 7077871;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "999997"},
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
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.02%"},
            {"OrderListHash", "dafe02af29d6a320da2e5dad28411559"}
        };
    }
}
