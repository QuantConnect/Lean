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
    /// Basic algorithm demonstrating how to place LimitIfTouched orders.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />`
    /// <meta name="tag" content="limit if touched order"/>
    public class LimitIfTouchedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private OrderTicket _request;
        private int _negative;

        // We assert the following occur in FIFO order in OnOrderEvent
        private readonly Queue<string> _expectedEvents = new Queue<string>(new[]
        {
            "Time: 10/10/2013 13:31:00 OrderID: 72 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 152.8807 USD LimitPrice: 152.519 TriggerPrice: 151.769 OrderFee: 1 USD",
            "Time: 10/10/2013 15:55:00 OrderID: 73 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 153.9225 USD LimitPrice: 153.8898 TriggerPrice: 153.1398 OrderFee: 1 USD",
            "Time: 10/11/2013 14:02:00 OrderID: 74 EventID: 11 Symbol: SPY Status: Filled Quantity: -1 FillQuantity: -1 FillPrice: 154.9643 USD LimitPrice: 154.9317 TriggerPrice: 154.1817 OrderFee: 1 USD",
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
                var orderRequest = new SubmitOrderRequest(OrderType.LimitIfTouched, SecurityType.Equity, "SPY",
                    _negative * 10, 0,
                    data["SPY"].Price - (decimal) _negative, data["SPY"].Price - (decimal) 0.25 * _negative, UtcTime,
                    $"LIT - Quantity: {_negative * 10}");
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
                _request.UpdateQuantity(newQuantity, $"LIT - Quantity: {newQuantity}");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.625%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.008%"},
            {"Sharpe Ratio", "-13.588"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.002"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.779"},
            {"Tracking Error", "0.22"},
            {"Treynor Ratio", "3.431"},
            {"Total Fees", "$3.00"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-15.79"},
            {"Return Over Maximum Drawdown", "-82.891"},
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
            {"OrderListHash", "05ae058d8e98b92dcb6fa0612f9a598e"}
        };
    }
}
