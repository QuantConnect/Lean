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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting we can specify a custom Shortable Provider
    /// </summary>
    public class CustomShortableProviderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Security _spy;
        private OrderTicket _orderId;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 4);
            SetEndDate(2013, 10, 6);
            SetCash(10000000);

            _spy = AddEquity("SPY", Resolution.Daily);
            _spy.SetShortableProvider(new CustomSPYShortableProvider());
        }

        public override void OnData(Slice slice)
        {
            var spyShortableQuantity = _spy.ShortableProvider.ShortableQuantity(_spy.Symbol, Time);
            if (spyShortableQuantity > 1000)
            {
                _orderId = Sell("SPY", (int)spyShortableQuantity);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            var transactions = Transactions.OrdersCount;
            if (transactions != 1)
            {
                throw new Exception($"Algorithm should have just 1 order, but was {transactions}");
            }
            var orderQuantity = Transactions.GetOrderById(_orderId).Quantity;
            if (orderQuantity != -1001)
            {
                throw new Exception($"Quantity of order {_orderId} should be -1001, but was {orderQuantity}");
            }
        }

        private class CustomSPYShortableProvider : IShortableProvider
        {
            public long? ShortableQuantity(Symbol symbol, DateTime localTime)
            {
                if (localTime < new DateTime(2013, 10, 5))
                {
                    return 10;
                }
                else
                {
                    return 1001;
                }
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
        public long DataPoints => 17;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
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
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "f3552fd04615002c13875b1604a29b6e"}
        };
    }
}
