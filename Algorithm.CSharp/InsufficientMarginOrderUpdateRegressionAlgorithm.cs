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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm tests order updates with margin constraints to ensure that orders become invalid when exceeding margin requirements.
    /// </summary>
    public class InsufficientMarginOrderUpdateRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _stopOrderTicket;
        private OrderTicket _limitOrderTicket;
        private OrderTicket _trailingStopOrderTicket;

        public override void Initialize()
        {
            SetStartDate(2018, 4, 3);
            SetEndDate(2018, 4, 4);
            AddForex("EURUSD", Resolution.Minute);
        }

        public override void OnData(Slice data)
        {

            if (!Portfolio.Invested)
            {
                var qty = CalculateOrderQuantity("EURUSD", 50m);

                MarketOrder("EURUSD", qty);

                // Place stop market, limit, and trailing stop orders with half the quantity
                _stopOrderTicket = StopMarketOrder("EURUSD", -qty / 2, Securities["EURUSD"].Price - 0.003m);
                _limitOrderTicket = LimitOrder("EURUSD", -qty / 2, Securities["EURUSD"].Price - 0.003m);
                _trailingStopOrderTicket = TrailingStopOrder("EURUSD", -qty / 2, Securities["EURUSD"].Price - 0.003m, 0.01m, true);

                // Update the stop order 
                var updateStopOrderSettings = new UpdateOrderFields
                {
                    // Attempt to increase the order quantity significantly
                    Quantity = -qty * 100,
                    StopPrice = Securities["EURUSD"].Price - 0.003m
                };
                _stopOrderTicket.Update(updateStopOrderSettings);

                // Update limit order
                var updateLimitOrderSettings = new UpdateOrderFields
                {
                    // Attempt to increase the order quantity significantly
                    Quantity = -qty * 100,
                    LimitPrice = Securities["EURUSD"].Price - 0.003m
                };
                _limitOrderTicket.Update(updateLimitOrderSettings);

                // Update trailing stop order
                var updateTrailingStopOrderSettings = new UpdateOrderFields
                {
                    // Attempt to increase the order quantity significantly
                    Quantity = -qty * 100,
                    StopPrice = Securities["EURUSD"].Price - 0.003m,
                    TrailingAmount = 0.01m,
                };
                _trailingStopOrderTicket.Update(updateTrailingStopOrderSettings);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // Check if the order is invalid
            if (_stopOrderTicket != null && orderEvent.OrderId == _stopOrderTicket.OrderId && _stopOrderTicket.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException($"Order {_stopOrderTicket.OrderId} with symbol {_stopOrderTicket.Symbol} should have been invalid due to insufficient margin after the update, but its current status is {_stopOrderTicket.Status}.");
            }

            // Check if limit order is invalid due to insufficient margin after update
            if (_limitOrderTicket != null && orderEvent.Id == _limitOrderTicket.OrderId && _limitOrderTicket.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException($"Order {_limitOrderTicket.OrderId} with symbol {_limitOrderTicket.Symbol} should have been invalid due to insufficient margin after the update, but its current status is {_limitOrderTicket.Status}.");
            }

            // Check if trailing stop order is invalid due to insufficient margin after update
            if (_trailingStopOrderTicket != null && orderEvent.Id == _trailingStopOrderTicket.OrderId && _trailingStopOrderTicket.Status != OrderStatus.Invalid)
            {
                throw new RegressionTestException($"Order {_trailingStopOrderTicket.OrderId} with symbol {_trailingStopOrderTicket.Symbol} should have been invalid due to insufficient margin after the update, but its current status is {_trailingStopOrderTicket.Status}.");
            }
        }

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2893;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 60;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000.00"},
            {"End Equity", "91982.00"},
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
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$250000.00"},
            {"Lowest Capacity Asset", "EURUSD 8G"},
            {"Portfolio Turnover", "3074.60%"},
            {"OrderListHash", "39c85ecfd3308fd77636d56af3a094e9"}
        };
    }
}
