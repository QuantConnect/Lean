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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic algorithm demonstrating how to place Stop Limit orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />`
    /// <meta name="tag" content="stop limit order"/>
    public class StopLimitRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _request;
        private int _negative;

        // We assert the following occur in FIFO order in OnOrderEvent
        private readonly Queue<string> _expectedEvents = new (new[]
        {
            "Time: 10/08/2013 19:37:00 OrderID: 69 EventID: 16 Symbol: SPY Status: Filled Quantity: 3 FillQuantity: 3 FillPrice: 143.5491 USD LimitPrice: 143.5491 StopPrice: 143.9 OrderFee: 1 USD",
            "Time: 10/09/2013 14:33:00 OrderID: 73 EventID: 63 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.3427 USD LimitPrice: 143.3427 StopPrice: 142.99 OrderFee: 1 USD",
            "Time: 10/09/2013 17:27:00 OrderID: 74 EventID: 184 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.4464 USD LimitPrice: 143.4464 StopPrice: 143.1 OrderFee: 1 USD",
            "Time: 10/10/2013 13:31:00 OrderID: 75 EventID: 164 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 143.5243 USD LimitPrice: 143.5243 StopPrice: 143.17 OrderFee: 1 USD"
        });

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07); //Set Start Date
            SetEndDate(2013, 10, 11); //Set End Date
            SetCash(100000); //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddEquity("SPY");
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!data.ContainsKey("SPY"))
            {
                return;
            }

            // After an order is placed, it will decrease in quantity by one for each minute, being cancelled altogether
            // if not filled within 10 minutes.
            if (Transactions.GetOpenOrders().Count == 0)
            {
                var goLong = Time.Day < 9;
                _negative = goLong ? 1 : -1;
                var orderRequest = new SubmitOrderRequest(OrderType.StopLimit, SecurityType.Equity, "SPY",
                    _negative * 10, data["SPY"].Price + 0.25m * _negative,
                    data["SPY"].Price - 0.1m * _negative, 0, UtcTime,
                    $"StopLimit - Quantity: {_negative * 10}");
                _request = Transactions.AddOrder(orderRequest);
                return;
            }

            // Order updating if request exists 
            if (_request != null)
            {
                if (_request.Quantity == 1)
                {
                    Transactions.CancelOpenOrders();
                    _request = null;
                    return;
                }

                var newQuantity = _request.Quantity - _negative;
                _request.UpdateQuantity(newQuantity, $"StopLimit - Quantity: {newQuantity}");
                _request.UpdateStopPrice(_request.Get(OrderField.StopPrice).RoundToSignificantDigits(5));
            }
        }

        /// <summary>
        /// Order fill event handler. On an order fill update the resulting information is passed to this method.
        /// </summary>
        /// <param name="orderEvent">Order event details containing details of the events</param>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                var expected = _expectedEvents.Dequeue();

                if (orderEvent.ToString() != expected)
                {
                    throw new Exception($"orderEvent {orderEvent.Id} differed from {expected}");
                }
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages => new[] { Language.CSharp, Language.Python };

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
            {"Total Trades", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.338%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.004%"},
            {"Sharpe Ratio", "-16.669"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.004"},
            {"Beta", "0.001"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.927"},
            {"Tracking Error", "0.222"},
            {"Treynor Ratio", "-5.012"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$45000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-35.136"},
            {"Return Over Maximum Drawdown", "-82.872"},
            {"Portfolio Turnover", "0.002"},
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
            {"OrderListHash", "615d6253f06f84144200b51448c19b72"}
        };
    }
}
