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
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm of time in force order settings.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class TimeInForceAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private OrderTicket _gtcOrderTicket1, _gtcOrderTicket2;
        private OrderTicket _dayOrderTicket1, _dayOrderTicket2;
        private OrderTicket _gtdOrderTicket1, _gtdOrderTicket2;
        private readonly Dictionary<int, OrderStatus> _expectedOrderStatuses = new Dictionary<int, OrderStatus>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            // The default time in force setting for all orders is GoodTilCancelled (GTC),
            // uncomment this line to set a different time in force.
            // We currently only support GTC, DAY, GTD.
            // DefaultOrderProperties.TimeInForce = TimeInForce.Day;

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_gtcOrderTicket1 == null)
            {
                // These GTC orders will never expire and will not be canceled automatically.

                DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilCanceled;

                // this order will not be filled before the end of the backtest
                _gtcOrderTicket1 = LimitOrder(_symbol, 10, 100m);
                _expectedOrderStatuses.Add(_gtcOrderTicket1.OrderId, OrderStatus.Submitted);

                // this order will be filled before the end of the backtest
                _gtcOrderTicket2 = LimitOrder(_symbol, 10, 160m);
                _expectedOrderStatuses.Add(_gtcOrderTicket2.OrderId, OrderStatus.Filled);
            }

            if (_dayOrderTicket1 == null)
            {
                // These DAY orders will expire at market close,
                // if not filled by then they will be canceled automatically.

                DefaultOrderProperties.TimeInForce = TimeInForce.Day;

                // this order will not be filled before market close and will be canceled
                _dayOrderTicket1 = LimitOrder(_symbol, 10, 150m);
                _expectedOrderStatuses.Add(_dayOrderTicket1.OrderId, OrderStatus.Canceled);

                // this order will be filled before market close
                _dayOrderTicket2 = LimitOrder(_symbol, 10, 180m);
                _expectedOrderStatuses.Add(_dayOrderTicket2.OrderId, OrderStatus.Filled);
            }

            if (_gtdOrderTicket1 == null)
            {
                // These GTD orders will expire on October 10th at market close,
                // if not filled by then they will be canceled automatically.

                DefaultOrderProperties.TimeInForce = TimeInForce.GoodTilDate(new DateTime(2013, 10, 10));

                // this order will not be filled before expiry and will be canceled
                _gtdOrderTicket1 = LimitOrder(_symbol, 10, 100m);
                _expectedOrderStatuses.Add(_gtdOrderTicket1.OrderId, OrderStatus.Canceled);

                // this order will be filled before expiry
                _gtdOrderTicket2 = LimitOrder(_symbol, 10, 160m);
                _expectedOrderStatuses.Add(_gtdOrderTicket2.OrderId, OrderStatus.Filled);
            }
        }

        /// <summary>
        /// Order event handler. This handler will be called for all order events, including submissions, fills, cancellations.
        /// </summary>
        /// <param name="orderEvent">Order event instance containing details of the event</param>
        /// <remarks>This method can be called asynchronously, ensure you use proper locks on thread-unsafe objects</remarks>
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent}");
        }

        /// <summary>
        /// End of algorithm run event handler. This method is called at the end of a backtest or live trading operation.
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            foreach (var kvp in _expectedOrderStatuses)
            {
                var orderId = kvp.Key;
                var expectedStatus = kvp.Value;

                var order = Transactions.GetOrderById(orderId);

                if (order.Status != expectedStatus)
                {
                    throw new Exception($"Invalid status for order {orderId} - Expected: {expectedStatus}, actual: {order.Status}");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "5.937%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.074%"},
            {"Sharpe Ratio", "5.002"},
            {"Probabilistic Sharpe Ratio", "67.171%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.038"},
            {"Beta", "0.046"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.949"},
            {"Tracking Error", "0.21"},
            {"Treynor Ratio", "1.099"},
            {"Total Fees", "$3.00"},
            {"Fitness Score", "0.011"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "5.672"},
            {"Return Over Maximum Drawdown", "58.812"},
            {"Portfolio Turnover", "0.011"},
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
            {"OrderListHash", "359885308"}
        };
    }
}
