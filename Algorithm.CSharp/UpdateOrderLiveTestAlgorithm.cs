
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
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides a regression baseline focused on updating orders
    /// </summary>
    public class UpdateOrderLiveTestAlgorithm : QCAlgorithm
    {
        private static readonly Random Random = new Random();

        private const decimal ImmediateCancelPercentage = 0.05m;

        private int LastMinute = -1;
        private Security Security;
        private int Quantity = 5;
        private const int DeltaQuantity = 1;

        private const decimal StopPercentage = 0.025m;
        private const decimal StopPercentageDelta = 0.005m;
        private const decimal LimitPercentage = 0.025m;
        private const decimal LimitPercentageDelta = 0.005m;

        private const string Symbol = "SPY";
        private const SecurityType SecType = SecurityType.Equity;

        private readonly CircularQueue<OrderType> _orderTypesQueue = new CircularQueue<OrderType>(new []
        {
            OrderType.MarketOnOpen,
            OrderType.MarketOnClose,
            OrderType.StopLimit,
            OrderType.StopMarket,
            OrderType.Limit,
            OrderType.Market
        });

        private readonly List<OrderTicket> _tickets = new List<OrderTicket>();

        private readonly HashSet<int> _immediateCancellations = new HashSet<int>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 07);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecType, Symbol, Resolution.Second);
            Security = Securities[Symbol];

            _orderTypesQueue.CircleCompleted += (sender, args) =>
            {
                // flip our signs
                Quantity *= -1;
            };
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Security.HasData)
            {
                Log("::::: NO DATA :::::");
                return;
            }

            // each month make an action
            if (Time.Minute != LastMinute && Time.Second == 0)
            {
                Log("");
                Log("--------------Minute: " + Time.Minute);
                Log("");
                LastMinute = Time.Minute;
                // we'll submit the next type of order from the queue
                var orderType = _orderTypesQueue.Dequeue();
                Log("ORDER TYPE:: " + orderType);
                var isLong = Quantity > 0;
                var stopPrice = isLong ? (1 + StopPercentage) * Security.High : (1 - StopPercentage) * Security.Low;
                var limitPrice = isLong ? (1 - LimitPercentage) * stopPrice : (1 + LimitPercentage) * stopPrice;
                if (orderType == OrderType.Limit)
                {
                    limitPrice = !isLong ? (1 + LimitPercentage) * Security.High : (1 - LimitPercentage) * Security.Low;
                }
                var request = new SubmitOrderRequest(orderType, SecType, Symbol, Quantity, stopPrice, limitPrice, Time, orderType.ToString());
                var ticket = Transactions.AddOrder(request);
                _tickets.Add(ticket);
                if ((decimal)Random.NextDouble() < ImmediateCancelPercentage)
                {
                    Log("Immediate cancellation requested!");
                    _immediateCancellations.Add(ticket.OrderId);
                }
            }
            else if (_tickets.Count > 0)
            {
                var ticket = _tickets.Last();
                if (Time.Second > 15 && Time.Second < 30)
                {
                    if (ticket.UpdateRequests.Count == 0 && ticket.Status.IsOpen())
                    {
                        Log(ticket.ToString());
                        ticket.Update(new UpdateOrderFields
                        {
                            Quantity = ticket.Quantity + Math.Sign(Quantity) * DeltaQuantity,
                            Tag = "Change quantity: " + Time
                        });
                        Log("UPDATE1:: " + ticket.UpdateRequests.Last());
                    }
                }
                else if (Time.Second > 29 && Time.Second < 45)
                {
                    if (ticket.UpdateRequests.Count == 1 && ticket.Status.IsOpen())
                    {
                        Log(ticket.ToString());
                        ticket.Update(new UpdateOrderFields
                        {
                            LimitPrice = Security.Price * (1 - Math.Sign(ticket.Quantity) * LimitPercentageDelta),
                            StopPrice = Security.Price * (1 + Math.Sign(ticket.Quantity) * StopPercentageDelta),
                            Tag = "Change prices: " + Time
                        });
                        Log("UPDATE2:: " + ticket.UpdateRequests.Last());
                    }
                }
                else
                {
                    if (ticket.UpdateRequests.Count == 2 && ticket.Status.IsOpen())
                    {
                        Log(ticket.ToString());
                        ticket.Cancel(Time + " and is still open!");
                        Log("CANCELLED:: " + ticket.CancelRequest);
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_immediateCancellations.Contains(orderEvent.OrderId))
            {
                _immediateCancellations.Remove(orderEvent.OrderId);
                Transactions.CancelOrder(orderEvent.OrderId);
            }

            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log("FILLED:: " + Transactions.GetOrderById(orderEvent.OrderId) + " FILL PRICE:: " + orderEvent.FillPrice.SmartRounding());
            }
            else
            {
                Log(orderEvent.ToString());
            }
        }

        private void Log(string msg)
        {
            // redirect live logs to debug window
            if (LiveMode)
            {
                Debug(msg);
            }
            else
            {
                base.Log(msg);
            }
        }

        /// <summary>
        /// A never ending queue that will dequeue and reenqueue the same item
        /// </summary>
        private class CircularQueue<T>
        {
            private readonly T _head;
            private readonly Queue<T> _queue;

            /// <summary>
            /// Fired when we do a full circle
            /// </summary>
            public event EventHandler CircleCompleted;

            public CircularQueue(IEnumerable<T> items)
            {
                _queue = new Queue<T>();

                var first = true;
                foreach (var item in items)
                {
                    if (first)
                    {
                        first = false;
                        _head = item;
                    }
                    _queue.Enqueue(item);
                }
            }

            public T Dequeue()
            {
                var item = _queue.Dequeue();
                if (item.Equals(_head))
                {
                    OnCircleCompleted();
                }
                _queue.Enqueue(item);
                return item;
            }

            protected virtual void OnCircleCompleted()
            {
                var handler = CircleCompleted;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }
    }
}