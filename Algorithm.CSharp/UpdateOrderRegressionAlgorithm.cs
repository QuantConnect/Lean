
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
using System.Globalization;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides a regression baseline focused on updating orders
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class UpdateOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int LastMonth = -1;
        private Security Security;
        private int Quantity = 100;
        private const int DeltaQuantity = 10;

        private const decimal StopPercentage = 0.025m;
        private const decimal StopPercentageDelta = 0.005m;
        private const decimal LimitPercentage = 0.025m;
        private const decimal LimitPercentageDelta = 0.005m;

        private const string symbol = "SPY";
        private const SecurityType SecType = SecurityType.Equity;

        private readonly CircularQueue<OrderType> _orderTypesQueue = new CircularQueue<OrderType>(Enum.GetValues(typeof(OrderType))
                                                                        .OfType<OrderType>()
                                                                        .Where (x => x != OrderType.OptionExercise && x != OrderType.LimitIfTouched
                                                                            && x != OrderType.ComboMarket && x != OrderType.ComboLimit && x != OrderType.ComboLegLimit));
        private readonly List<OrderTicket> _tickets = new List<OrderTicket>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);  //Set Start Date
            SetEndDate(2015, 01, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecType, symbol, Resolution.Daily);
            Security = Securities[symbol];

            _orderTypesQueue.CircleCompleted += (sender, args) =>
            {
                // flip our signs when we've gone through all the order types
                Quantity *= -1;
            };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!slice.Bars.ContainsKey(symbol)) return;

            // each month make an action
            if (Time.Month != LastMonth)
            {
                // we'll submit the next type of order from the queue
                var orderType = _orderTypesQueue.Dequeue();
                //Log("");
                Log($"\r\n--------------MONTH: {Time.ToStringInvariant("MMMM")}:: {orderType}\r\n");
                //Log("");
                LastMonth = Time.Month;
                Log("ORDER TYPE:: " + orderType);
                var isLong = Quantity > 0;
                var stopPrice = isLong ? (1 + StopPercentage)*slice.Bars[symbol].High : (1 - StopPercentage)*slice.Bars[symbol].Low;
                var limitPrice = isLong ? (1 - LimitPercentage)*stopPrice : (1 + LimitPercentage)*stopPrice;
                if (orderType == OrderType.Limit)
                {
                    limitPrice = !isLong ? (1 + LimitPercentage) * slice.Bars[symbol].High : (1 - LimitPercentage) * slice.Bars[symbol].Low;
                }
                var request = new SubmitOrderRequest(orderType, SecType, symbol, Quantity, stopPrice, limitPrice, 0, 0.01m, true, UtcTime,
                    ((int)orderType).ToString(CultureInfo.InvariantCulture));
                var ticket = Transactions.AddOrder(request);
                _tickets.Add(ticket);
            }
            else if (_tickets.Count > 0)
            {
                var ticket = _tickets.Last();
                if (Time.Day > 8 && Time.Day < 14)
                {
                    if (ticket.UpdateRequests.Count == 0 && ticket.Status.IsOpen())
                    {
                        Log("TICKET:: " + ticket);
                        ticket.Update(new UpdateOrderFields
                        {
                            Quantity = ticket.Quantity + Math.Sign(Quantity)*DeltaQuantity,
                            Tag = "Change quantity: " + Time.Day
                        });
                        Log("UPDATE1:: " + ticket.UpdateRequests.Last());
                    }
                }
                else if (Time.Day > 13 && Time.Day < 20)
                {
                    if (ticket.UpdateRequests.Count == 1 && ticket.Status.IsOpen())
                    {
                        Log("TICKET:: " + ticket);
                        ticket.Update(new UpdateOrderFields
                        {
                            LimitPrice = ticket.OrderType != OrderType.TrailingStopLimit
                                ? Security.Price*(1 - Math.Sign(ticket.Quantity)*LimitPercentageDelta)
                                : null,
                            StopPrice = (ticket.OrderType != OrderType.TrailingStop && ticket.OrderType != OrderType.TrailingStopLimit)
                                ? Security.Price*(1 + Math.Sign(ticket.Quantity)*StopPercentageDelta)
                                : null,
                            Tag = "Change prices: " + Time.Day
                        });
                        Log("UPDATE2:: " + ticket.UpdateRequests.Last());
                    }
                }
                else
                {
                    if (ticket.UpdateRequests.Count == 2 && ticket.Status.IsOpen())
                    {
                        Log("TICKET:: " + ticket);
                        ticket.Cancel(Time.Day + " and is still open!");
                        Log("CANCELLED:: " + ticket.CancelRequest);
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            // if the order time isn't equal to the algo time, then the modified time on the order should be updated
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            var ticket = Transactions.GetOrderTicket(orderEvent.OrderId);
            if (order.Status == OrderStatus.Canceled && order.CanceledTime != orderEvent.UtcTime)
            {
                throw new RegressionTestException("Expected canceled order CanceledTime to equal canceled order event time.");
            }

            // fills update LastFillTime
            if ((order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled) && order.LastFillTime != orderEvent.UtcTime)
            {
                throw new RegressionTestException("Expected filled order LastFillTime to equal fill order event time.");
            }

            // check the ticket to see if the update was successfully processed
            if (ticket.UpdateRequests.Any(ur => ur.Response?.IsSuccess == true) && order.CreatedTime != UtcTime && order.LastUpdateTime == null)
            {
                throw new RegressionTestException("Expected updated order LastUpdateTime to equal submitted update order event time");
            }

            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log("FILLED:: " + Transactions.GetOrderById(orderEvent.OrderId) + " FILL PRICE:: " + orderEvent.FillPrice.SmartRounding());
            }
            else
            {
                Log(orderEvent.ToString());
                Log("TICKET:: " + ticket);
            }
        }

        private new void Log(string msg)
        {
            if (LiveMode) Debug(msg);
            else base.Log(msg);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 4030;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "24"},
            {"Average Win", "0%"},
            {"Average Loss", "-2.17%"},
            {"Compounding Annual Return", "-14.133%"},
            {"Drawdown", "28.500%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "73741.52"},
            {"Net Profit", "-26.258%"},
            {"Sharpe Ratio", "-1.072"},
            {"Sortino Ratio", "-1.232"},
            {"Probabilistic Sharpe Ratio", "0.027%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.031"},
            {"Beta", "-0.906"},
            {"Annual Standard Deviation", "0.096"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "-1.364"},
            {"Tracking Error", "0.184"},
            {"Treynor Ratio", "0.114"},
            {"Total Fees", "$21.00"},
            {"Estimated Strategy Capacity", "$750000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.52%"},
            {"OrderListHash", "f2371f5962b956c9d102b95263702242"}
        };
    }
}
