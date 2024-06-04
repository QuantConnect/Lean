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
using System.Linq;
using NUnit.Framework;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using QuantConnect.Orders.CrossZero;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class OrderCrossingBrokerageTests
    {
        [TestCase(0, 1, false)]
        [TestCase(-1, -1, false)]
        [TestCase(-1, 1, false)]
        [TestCase(-1, 2, true)]
        [TestCase(1, -2, true)]
        public void ShouldOrderCrossesZero(decimal holdingQuantity, decimal orderQuantity, bool expectedCrossResult)
        {
            using var brokerage = new PhonyBrokerage("phony", new AlgorithmStub());
            var isOrderCrosses = brokerage.GetOrderCrossesZero(holdingQuantity, orderQuantity);
            Assert.That(isOrderCrosses, Is.EqualTo(expectedCrossResult));
        }

        [TestCase(-1, 2, 1, 1, Description = "short to long")]
        [TestCase(1, -2, -1, -1, Description = "long to short")]
        [TestCase(-10, 20, 10, 10, Description = "long to short")]
        [TestCase(10, -20, -10, -10, Description = "long to short")]
        public void GetQuantityOnCrossPosition(decimal holdingQuantity, decimal orderQuantity, decimal expectedFirstOrderQuantity, decimal expectedSecondOrderQuantity)
        {
            using var brokerage = new PhonyBrokerage("phony", new AlgorithmStub());
            if (brokerage.GetOrderCrossesZero(holdingQuantity, orderQuantity))
            {
                var (firstOrderQuantity, secondOrderQuantity) = brokerage.GetQuantityOnCrossPosition(holdingQuantity, orderQuantity);
                Assert.That(expectedFirstOrderQuantity, Is.EqualTo(firstOrderQuantity));
                Assert.That(expectedSecondOrderQuantity, Is.EqualTo(secondOrderQuantity));
            }
            else
            {
                Assert.Fail($"Order does not cross zero.Holding quantity: {holdingQuantity}, Order quantity: {orderQuantity}");
            }
        }

        [TestCase(new[] { OrderStatus.Submitted, OrderStatus.PartiallyFilled, OrderStatus.Filled })]
        public void PlaceCrossOrder(OrderStatus[] expectedOrderStatusChangedOrdering)
        {
            var actualCrossZeroOrderStatusOrdering = new Queue<OrderStatus>();
            using var autoResetEventPartialFilledStatus = new AutoResetEvent(false);

            var stopMarket = new StopMarketOrder(Symbols.AAPL, -20, 180m, DateTime.UtcNow);

            using var brokerage = InitializeBrokerage(("AAPL", 180m, 10));

            brokerage.OrdersStatusChanged += (_, orderEvents) =>
            {
                var orderEvent = orderEvents[0];

                actualCrossZeroOrderStatusOrdering.Enqueue(orderEvent.Status);

                Log.Trace($"{nameof(PlaceCrossOrder)}.OrdersStatusChangedEvent.Status: {orderEvent.Status}");

                if (orderEvent.Status == OrderStatus.PartiallyFilled)
                {
                    autoResetEventPartialFilledStatus.Set();
                }

                if (orderEvent.Status == OrderStatus.Filled)
                {
                    autoResetEventPartialFilledStatus.Set();
                }
            };

            var response = brokerage.PlaceOrder(stopMarket);

            Assert.IsTrue(response);

            autoResetEventPartialFilledStatus.WaitOne(TimeSpan.FromSeconds(5));
            var partialFilledOrder = brokerage.GetAllOrders(o => o.Status == OrderStatus.PartiallyFilled).Single();
            Assert.IsNotNull(partialFilledOrder);

            CollectionAssert.AreEquivalent(expectedOrderStatusChangedOrdering, actualCrossZeroOrderStatusOrdering);
        }

        /// <summary>
        /// Create instance of Phony brokerage.
        /// </summary>
        /// <param name="equityQuantity">("AAPL", 10)</param>
        /// <returns>The instance of Phony Brokerage</returns>
        private static PhonyBrokerage InitializeBrokerage(params (string ticker, decimal averagePrice, decimal quantity)[] equityQuantity)
        {
            var algorithm = new AlgorithmStub();
            foreach (var (symbol, averagePrice, quantity) in equityQuantity)
            {
                algorithm.AddEquity(symbol).Holdings.SetHoldings(180m, quantity);
            }

            var brokerage = new PhonyBrokerage("Phony", algorithm);

            AssertThatHoldingIsNotEmpty(brokerage);

            return brokerage;
        }

        private static void AssertThatHoldingIsNotEmpty(IBrokerage brokerage)
        {
            var holdings = brokerage.GetAccountHoldings();
            Assert.Greater(holdings.Count, 0);
            Assert.Greater(holdings[0].Quantity, 0);
        }

        private class PhonyBrokerage : Brokerage
        {
            private readonly IAlgorithm _algorithm;
            private readonly ISecurityProvider _securityProvider;
            private readonly CustomOrderProvider _orderProvider;
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// Temporarily stores the IDs of brokerage orders for testing purposes.
            /// </summary>
            private List<string> _tempBrokerageOrderIds = new();

            public override bool IsConnected => throw new NotImplementedException();

            public PhonyBrokerage(string name, IAlgorithm algorithm) : base(name)
            {
                _algorithm = algorithm;
                _orderProvider = new CustomOrderProvider();
                _securityProvider = algorithm.Portfolio;

                OrdersStatusChanged += OrdersStatusChangedEventHandler;
                ImitationBrokerageOrderUpdates();
            }

            private void OrdersStatusChangedEventHandler(object _, List<OrderEvent> orderEvents)
            {
                var orderEvent = orderEvents[0];
                
                var brokerageOrderId = _tempBrokerageOrderIds.Last();

                if (!TryGetOrRemoveCrossZeroOrder(brokerageOrderId, orderEvent.Status == OrderStatus.Filled, out var leanOrder))
                {
                    leanOrder = _orderProvider.GetOrderById(orderEvent.OrderId);
                }

                if (orderEvent.Status == OrderStatus.Filled)
                {

                    TryHandleRemainingCrossZeroOrder(leanOrder, orderEvent);
                }
                else
                {
                    _orderProvider.UpdateOrderStatusById(orderEvent.OrderId, orderEvent.Status);
                }
            }

            public IEnumerable<Order> GetAllOrders(Func<Order, bool> filter)
            {
                return _orderProvider.GetOrders(filter);
            }


            public override bool CancelOrder(Order order)
            {
                throw new NotImplementedException();
            }

            public override void Connect()
            {
                throw new NotImplementedException();
            }

            public override void Disconnect()
            {
                throw new NotImplementedException();
            }

            public override List<Holding> GetAccountHoldings()
            {
                return base.GetAccountHoldings(null, _algorithm.Securities.Values);
            }

            public override List<CashAmount> GetCashBalance()
            {
                throw new NotImplementedException();
            }

            public override List<Order> GetOpenOrders()
            {
                throw new NotImplementedException();
            }

            public override bool PlaceOrder(Order order)
            {
                _orderProvider.Add(order);

                var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);

                var isPlaceCrossOrder = TryCrossZeroPositionOrder(order, holdingQuantity);

                // Place simple order 
                if (isPlaceCrossOrder == null)
                {
                    Assert.Fail("Unable to place a cross order. Please ensure your account holds the necessary securities and try again.");
                }

                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.Submitted });

                return isPlaceCrossOrder.Value;
            }

            public int GetLeanOrderByZeroCrossBrokerageOrderIdCount()
            {
                return LeanOrderByZeroCrossBrokerageOrderId.Count;
            }

            public bool GetOrderCrossesZero(decimal holdingQuantity, decimal orderQuantity)
            {
                return OrderCrossesZero(holdingQuantity, orderQuantity);
            }

            public (decimal closePostionQunatity, decimal newPositionQuantity) GetQuantityOnCrossPosition(decimal holdingQuantity, decimal orderQuantity)
            {
                return GetQuantityOnCrossPosition(holdingQuantity, orderQuantity);
            }

            protected override CrossZeroOrderResponse PlaceCrossZeroOrder(CrossZeroOrderRequest crossZeroOrderRequest, bool isPlaceOrderWithoutLeanEvent)
            {
                Log.Trace($"{nameof(PhonyBrokerage)}.{nameof(PlaceCrossZeroOrder)}");
                var response = PlaceOrderPhonyBrokerage(new PhonyPlaceOrderRequest(crossZeroOrderRequest.LeanOrder.Symbol.Value, crossZeroOrderRequest.OrderQuantity,
                    crossZeroOrderRequest.LeanOrder.Direction, 0m, crossZeroOrderRequest.OrderType));
                return new CrossZeroOrderResponse(response.OrderId, response.IsOrderPlacedSuccessfully);
            }

            private PhonyPlaceOrderResponse PlaceOrderPhonyBrokerage(PhonyPlaceOrderRequest order)
            {
                var newOrderId = Guid.NewGuid().ToString();
                _tempBrokerageOrderIds.Add(newOrderId);
                return new PhonyPlaceOrderResponse(newOrderId, true);
            }

            public override void Dispose()
            {
                _cancellationTokenSource.Dispose();
                base.Dispose();
            }

            public override bool UpdateOrder(Order order)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, $"{nameof(PhonyBrokerage)} Order Event")
                {
                    Status = OrderStatus.UpdateSubmitted
                });
                return true;
            }

            /// <summary>
            /// Imitates a brokerage sending order update events.
            /// </summary>
            private void ImitationBrokerageOrderUpdates()
            {
                Task.Factory.StartNew(() =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                        var orders = _orderProvider.GetOpenOrders();
                        foreach (var order in orders)
                        {
                            if (order.Status == OrderStatus.Submitted || order.Status == OrderStatus.PartiallyFilled)
                            {
                                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero) { Status = OrderStatus.Filled });
                            }
                        }
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            private readonly struct PhonyPlaceOrderRequest
            {
                public string Symbol { get; }

                public decimal Quantity { get; }

                public OrderPosition Direction { get; }

                public OrderType CustomBrokerageOrderType { get;  }

                public PhonyPlaceOrderRequest(string symbol, decimal quantity, OrderDirection orderDirection, decimal holdingQuantity, OrderType leanOrderType)
                {
                    Symbol = symbol;
                    Quantity = quantity;
                    Direction = GetOrderPosition(orderDirection, holdingQuantity);
                    CustomBrokerageOrderType = leanOrderType;
                }
            }

            private readonly struct PhonyPlaceOrderResponse
            {
                public string OrderId { get; }

                public bool IsOrderPlacedSuccessfully { get; }

                public PhonyPlaceOrderResponse(string orderId, bool isOrderPlacedSuccessfully)
                {
                    OrderId = orderId;
                    IsOrderPlacedSuccessfully = isOrderPlacedSuccessfully;
                }
            }

            private class CustomOrderProvider : OrderProvider
            {
                public void UpdateOrderStatusById(int orderId, OrderStatus newOrderStatus)
                {
                    var order = _orders.First(x => x.Id == orderId);
                    order.Status = newOrderStatus;
                }
            }
        }

    }
}
