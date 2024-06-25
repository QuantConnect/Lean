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
using QuantConnect.Brokerages.CrossZero;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class OrderCrossingBrokerageTests
    {
        /// <summary>
        /// Provides a collection of test case data for order scenarios.
        /// </summary>
        /// <remarks>
        /// This property generates test case data for various order statuses, specifically 
        /// for a Stop Market Order on the AAPL symbol.
        /// </remarks>
        /// <returns>
        /// An <see cref="IEnumerable{TestCaseData}"/> containing test cases with a stop market order and an array of order statuses.
        /// </returns>
        private static IEnumerable<TestCaseData> OrderParameters
        {
            get
            {
                var expectedOrderStatusChangedOrdering = new[] { OrderStatus.Submitted, OrderStatus.PartiallyFilled, OrderStatus.Filled };
                yield return new TestCaseData(new MarketOrder(Symbols.AAPL, -15, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new LimitOrder(Symbols.AAPL, -15, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopMarketOrder(Symbols.AAPL, -20, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopLimitOrder(Symbols.AAPL, -15, 180m, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
            }
        }

        /// <summary>
        /// Tests placing an order and updating it, verifying the sequence of order status changes.
        /// </summary>
        /// <param name="leanOrder">The order to be placed and updated.</param>
        /// <param name="expectedOrderStatusChangedOrdering">The expected sequence of order status changes.</param>
        [Test, TestCaseSource(nameof(OrderParameters))]
        public void PlaceCrossOrder(Order leanOrder, OrderStatus[] expectedOrderStatusChangedOrdering)
        {
            var actualCrossZeroOrderStatusOrdering = new Queue<OrderStatus>();
            using var autoResetEventPartialFilledStatus = new AutoResetEvent(false);
            using var autoResetEventFilledStatus = new AutoResetEvent(false);

            using var brokerage = InitializeBrokerage((leanOrder?.Symbol.Value, 180m, 10));

            var skipFirstFilledEvent = default(bool);
            brokerage.OrdersStatusChanged += (_, orderEvents) =>
            {
                var orderEventStatus = orderEvents[0].Status;

                // Skip processing the first occurrence of the Filled event, The First Part of CrossZeroOrder was filled.
                if (!skipFirstFilledEvent && orderEventStatus == OrderStatus.Filled)
                {
                    skipFirstFilledEvent = true;
                    return;
                }

                actualCrossZeroOrderStatusOrdering.Enqueue(orderEventStatus);

                Log.Trace($"{nameof(PlaceCrossOrder)}.OrdersStatusChangedEvent.Status: {orderEventStatus}");

                if (orderEventStatus == OrderStatus.PartiallyFilled)
                {
                    autoResetEventPartialFilledStatus.Set();
                }

                if (orderEventStatus == OrderStatus.Filled)
                {
                    autoResetEventFilledStatus.Set();
                }
            };

            var response = brokerage.PlaceOrder(leanOrder);

            Assert.IsTrue(response);

            AssertComingOrderStatusByEvent(autoResetEventPartialFilledStatus, brokerage, OrderStatus.PartiallyFilled);

            AssertComingOrderStatusByEvent(autoResetEventFilledStatus, brokerage, OrderStatus.Filled);

            CollectionAssert.AreEquivalent(expectedOrderStatusChangedOrdering, actualCrossZeroOrderStatusOrdering);
            Assert.AreEqual(0, brokerage.GetLeanOrderByZeroCrossBrokerageOrderIdCount());
        }

        /// <summary>
        /// Provides a collection of test case data for order update scenarios.
        /// </summary>
        /// <remarks>
        /// This property generates test case data for various order statuses, specifically 
        /// for a Stop Market Order on the AAPL symbol.
        /// </remarks>
        /// <returns>
        /// An <see cref="IEnumerable{TestCaseData}"/> containing test cases with a stop market order and an array of order statuses.
        /// </returns>
        private static IEnumerable<TestCaseData> OrderUpdateParameters
        {
            get
            {
                var expectedOrderStatusChangedOrdering = new[] { OrderStatus.Submitted, OrderStatus.PartiallyFilled, OrderStatus.UpdateSubmitted, OrderStatus.Filled };
                yield return new TestCaseData(new MarketOrder(Symbols.AAPL, -15, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new LimitOrder(Symbols.AAPL, -15, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopMarketOrder(Symbols.AAPL, -20, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopLimitOrder(Symbols.AAPL, -15, 180m, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
            }
        }

        /// <summary>
        /// Tests placing an order and updating it, verifying the sequence of order status changes.
        /// </summary>
        /// <param name="leanOrder">The order to be placed and updated.</param>
        /// <param name="expectedOrderStatusChangedOrdering">The expected sequence of order status changes.</param>
        [Test, TestCaseSource(nameof(OrderUpdateParameters))]
        public void PlaceCrossOrderAndUpdate(Order leanOrder, OrderStatus[] expectedOrderStatusChangedOrdering)
        {
            var actualCrossZeroOrderStatusOrdering = new Queue<OrderStatus>();
            using var autoResetEventPartialFilledStatus = new AutoResetEvent(false);
            using var autoResetEventUpdateSubmittedStatus = new AutoResetEvent(false);
            using var autoResetEventFilledStatus = new AutoResetEvent(false);

            using var brokerage = InitializeBrokerage((leanOrder?.Symbol.Value, 180m, 10));

            var skipFirstFilledEvent = default(bool);
            brokerage.OrdersStatusChanged += (_, orderEvents) =>
            {
                var orderEventStatus = orderEvents[0].Status;

                // Skip processing the first occurrence of the Filled event, The First Part of CrossZeroOrder was filled.
                if (!skipFirstFilledEvent && orderEventStatus == OrderStatus.Filled)
                {
                    skipFirstFilledEvent = true;
                    return;
                }

                actualCrossZeroOrderStatusOrdering.Enqueue(orderEventStatus);

                Log.Trace($"{nameof(PlaceCrossOrder)}.OrdersStatusChangedEvent.Status: {orderEventStatus}");

                if (orderEventStatus == OrderStatus.PartiallyFilled)
                {
                    autoResetEventPartialFilledStatus.Set();
                }

                if (orderEventStatus == OrderStatus.UpdateSubmitted)
                {
                    autoResetEventUpdateSubmittedStatus.Set();
                }

                if (orderEventStatus == OrderStatus.Filled)
                {
                    autoResetEventFilledStatus.Set();
                }
            };

            var response = brokerage.PlaceOrder(leanOrder);
            Assert.IsTrue(response);

            AssertComingOrderStatusByEvent(autoResetEventPartialFilledStatus, brokerage, OrderStatus.PartiallyFilled);

            var updateResponse = brokerage.UpdateOrder(leanOrder);
            Assert.IsTrue(updateResponse);

            AssertComingOrderStatusByEvent(autoResetEventUpdateSubmittedStatus, brokerage, OrderStatus.UpdateSubmitted);

            AssertComingOrderStatusByEvent(autoResetEventFilledStatus, brokerage, OrderStatus.Filled);

            CollectionAssert.AreEquivalent(expectedOrderStatusChangedOrdering, actualCrossZeroOrderStatusOrdering);
            Assert.AreEqual(0, brokerage.GetLeanOrderByZeroCrossBrokerageOrderIdCount());
        }

        private static IEnumerable<TestCaseData> CrossZeroInvalidFirstPartParameters
        {
            get
            {
                var expectedOrderStatusChangedOrdering = new[] { OrderStatus.Submitted, OrderStatus.Invalid };
                yield return new TestCaseData(new MarketOrder(Symbols.AAPL, -15, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new LimitOrder(Symbols.AAPL, -15, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopMarketOrder(Symbols.AAPL, -20, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopLimitOrder(Symbols.AAPL, -15, 180m, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
            }
        }

        [Test, TestCaseSource(nameof(CrossZeroInvalidFirstPartParameters))]
        public void PlaceCrossOrderInvalid(Order leanOrder, OrderStatus[] expectedOrderStatusChangedOrdering)
        {
            var actualCrossZeroOrderStatusOrdering = new Queue<OrderStatus>();
            using var autoResetEventInvalidStatus = new AutoResetEvent(false);

            using var brokerage = InitializeBrokerage((leanOrder?.Symbol.Value, 180m, 10));

            brokerage.OrdersStatusChanged += (_, orderEvents) =>
            {
                var orderEventStatus = orderEvents[0].Status;

                actualCrossZeroOrderStatusOrdering.Enqueue(orderEventStatus);

                Log.Trace($"{nameof(PlaceCrossOrder)}.OrdersStatusChangedEvent.Status: {orderEventStatus}");

                if (orderEventStatus == OrderStatus.Invalid)
                {
                    autoResetEventInvalidStatus.Set();
                }
            };

            brokerage.IsPlaceOrderPhonyBrokerageFirstPartSuccessfully = false;
            brokerage.IsPlaceOrderPhonyBrokerageSecondPartSuccessfully = false;

            var response = brokerage.PlaceOrder(leanOrder);
            Assert.IsFalse(response);

            AssertComingOrderStatusByEvent(autoResetEventInvalidStatus, brokerage, OrderStatus.Invalid);

            CollectionAssert.AreEquivalent(expectedOrderStatusChangedOrdering, actualCrossZeroOrderStatusOrdering);
            Assert.AreEqual(0, brokerage.GetLeanOrderByZeroCrossBrokerageOrderIdCount());
        }

        private static IEnumerable<TestCaseData> OrderCrossZeroSecondPartParameters
        {
            get
            {
                var expectedOrderStatusChangedOrdering = new[] { OrderStatus.Submitted, OrderStatus.PartiallyFilled, OrderStatus.Canceled };
                yield return new TestCaseData(new MarketOrder(Symbols.AAPL, -15, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new LimitOrder(Symbols.AAPL, -15, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopMarketOrder(Symbols.AAPL, -20, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
                yield return new TestCaseData(new StopLimitOrder(Symbols.AAPL, -15, 180m, 180m, new DateTime(2024, 6, 10)), expectedOrderStatusChangedOrdering);
            }
        }

        [Test, TestCaseSource(nameof(OrderCrossZeroSecondPartParameters))]
        public void PlaceCrossZeroSecondPartInvalid(Order leanOrder, OrderStatus[] expectedOrderStatusChangedOrdering)
        {
            var actualCrossZeroOrderStatusOrdering = new Queue<OrderStatus>();
            using var autoResetEventInvalidStatus = new AutoResetEvent(false);

            using var brokerage = InitializeBrokerage((leanOrder?.Symbol.Value, 180m, 10));

            var skipFirstFilledEvent = default(bool);
            brokerage.OrdersStatusChanged += (_, orderEvents) =>
            {
                var orderEventStatus = orderEvents[0].Status;

                // Skip processing the first occurrence of the Filled event, The First Part of CrossZeroOrder was filled.
                if (!skipFirstFilledEvent && orderEventStatus == OrderStatus.Filled)
                {
                    skipFirstFilledEvent = true;
                    return;
                }

                actualCrossZeroOrderStatusOrdering.Enqueue(orderEventStatus);

                Log.Trace($"{nameof(PlaceCrossOrder)}.OrdersStatusChangedEvent.Status: {orderEventStatus}");

                if (orderEventStatus == OrderStatus.Canceled)
                {
                    autoResetEventInvalidStatus.Set();
                }
            };

            brokerage.IsPlaceOrderPhonyBrokerageFirstPartSuccessfully = true;
            brokerage.IsPlaceOrderPhonyBrokerageSecondPartSuccessfully = false;

            var response = brokerage.PlaceOrder(leanOrder);
            Assert.IsTrue(response);

            AssertComingOrderStatusByEvent(autoResetEventInvalidStatus, brokerage, OrderStatus.Canceled);

            CollectionAssert.AreEquivalent(expectedOrderStatusChangedOrdering, actualCrossZeroOrderStatusOrdering);
            Assert.AreEqual(0, brokerage.GetLeanOrderByZeroCrossBrokerageOrderIdCount());
        }

        /// <summary>
        /// Create instance of Phony brokerage.
        /// </summary>
        /// <param name="equityQuantity">("AAPL", 190m, 10)</param>
        /// <returns>The instance of Phony Brokerage</returns>
        private static PhonyBrokerage InitializeBrokerage(params (string ticker, decimal averagePrice, decimal quantity)[] equityQuantity)
        {
            var algorithm = new AlgorithmStub();
            foreach (var (symbol, averagePrice, quantity) in equityQuantity)
            {
                algorithm.AddEquity(symbol).Holdings.SetHoldings(averagePrice, quantity);
            }

            var brokerage = new PhonyBrokerage("Phony", algorithm);

            AssertThatHoldingIsNotEmpty(brokerage);

            return brokerage;
        }

        /// <summary>
        /// Asserts that the brokerage account holdings are not empty and that the first holding has a positive quantity.
        /// </summary>
        /// <param name="brokerage">The brokerage instance to check the account holdings.</param>
        /// <exception cref="AssertionException">
        /// Thrown if the account holdings are empty or the first holding has a non-positive quantity.
        /// </exception>
        private static void AssertThatHoldingIsNotEmpty(IBrokerage brokerage)
        {
            var holdings = brokerage.GetAccountHoldings();
            Assert.Greater(holdings.Count, 0);
            Assert.Greater(holdings[0].Quantity, 0);
        }

        /// <summary>
        /// Asserts that an order with the specified status has arrived by waiting for an event signal.
        /// </summary>
        /// <param name="resetEvent">The event to wait on, which signals that an order status update has occurred.</param>
        /// <param name="brokerage">The phony brokerage instance used to check the order status.</param>
        /// <param name="comingOrderStatus">The expected status of the coming order to assert.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there is not exactly one order with the specified status.
        /// </exception>
        private static void AssertComingOrderStatusByEvent(AutoResetEvent resetEvent, PhonyBrokerage brokerage, OrderStatus comingOrderStatus)
        {
            Assert.True(resetEvent.WaitOne(TimeSpan.FromSeconds(5)));
            var partialFilledOrder = brokerage.GetAllOrders(o => o.Status == comingOrderStatus).Single();
            Assert.IsNotNull(partialFilledOrder);
        }

        private class PhonyBrokerage : Brokerage
        {
            /// <inheritdoc cref="IAlgorithm"/>
            private readonly IAlgorithm _algorithm;

            /// <inheritdoc cref="ISecurityProvider"/>
            private readonly ISecurityProvider _securityProvider;

            /// <inheritdoc cref="CustomOrderProvider"/>
            private readonly CustomOrderProvider _orderProvider;

            /// <inheritdoc cref="CancellationTokenSource"/>
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// Indicates whether the first occurrence of the Filled event has been skipped.
            /// Used to ensure the first Filled event is processed appropriately for setting the
            /// order state to PartiallyFilled.
            /// </summary>
            private bool _isSkipFirstFilled;

            /// <summary>
            /// Temporarily stores the IDs of brokerage orders for testing purposes.
            /// </summary>
            private List<string> _tempBrokerageOrderIds = new();

            /// <summary>
            /// This field indicates whether the order has been placed successfully with a phony brokerage during testing.
            /// </summary>
            /// <remarks>
            /// The default value is <c>true</c>. This is used specifically for testing purposes to simulate the successful placement of an order.
            /// </remarks>
            public bool IsPlaceOrderPhonyBrokerageFirstPartSuccessfully { get; set; } = true;

            /// <summary>
            /// This field indicates whether the second part of the order has been placed successfully with a phony brokerage during testing.
            /// </summary>
            /// <remarks>
            /// The default value is <c>true</c>. This is used specifically for testing purposes to simulate the successful placement of the second part of an order.
            /// </remarks>
            public bool IsPlaceOrderPhonyBrokerageSecondPartSuccessfully { get; set; } = true;

            public override bool IsConnected => true;

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

                if (!TryGetOrRemoveCrossZeroOrder(brokerageOrderId, orderEvent.Status, out var leanOrder))
                {
                    leanOrder = _orderProvider.GetOrderById(orderEvent.OrderId);
                }

                // Process the first occurrence of the Filled event to simulate the leanOrder as PartiallyFilled.
                if (!_isSkipFirstFilled && orderEvent.Status == OrderStatus.Filled)
                {
                    _isSkipFirstFilled = true;
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
                OnOrderEvent(new OrderEvent(order, new DateTime(2024, 6, 10), OrderFee.Zero, "CancelOrder") { Status = OrderStatus.Canceled });
                return true;
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

            /// <summary>
            /// Gets the count of Lean orders indexed by ZeroCross brokerage order ID.
            /// </summary>
            /// <returns>
            /// The number of Lean orders that are indexed by ZeroCross brokerage order ID.
            /// </returns>
            public int GetLeanOrderByZeroCrossBrokerageOrderIdCount()
            {
                return LeanOrderByZeroCrossBrokerageOrderId.Count;
            }

            public override bool PlaceOrder(Order order)
            {
                // For testing purposes only: Adds the specified order to the order provider.
                _orderProvider.Add(order);

                var holdingQuantity = _securityProvider.GetHoldingsQuantity(order.Symbol);

                var isPlaceCrossOrder = TryCrossZeroPositionOrder(order, holdingQuantity);

                // Alert: This test covers only CrossZeroOrdering scenarios.
                // If isPlaceCrossOrder is null, it indicates failure to place a cross order.
                // Please ensure your account has sufficient securities and try again.
                if (isPlaceCrossOrder == null)
                {
                    Assert.Fail("Unable to place a cross order. Please ensure your account holds the necessary securities and try again.");
                }

                return isPlaceCrossOrder.Value;
            }

            /// <summary>
            /// Places a cross-zero order with the PhonyBrokerage.
            /// </summary>
            /// <param name="crossZeroOrderRequest">The cross-zero order request.</param>
            /// <param name="isPlaceOrderWithoutLeanEvent">Flag indicating whether to place the order without Lean event.</param>
            /// <returns>
            /// A <see cref="CrossZeroOrderResponse"/> containing the result of placing the order.
            /// </returns>
            protected override CrossZeroOrderResponse PlaceCrossZeroOrder(CrossZeroFirstOrderRequest crossZeroOrderRequest, bool isPlaceOrderWithoutLeanEvent)
            {
                Log.Trace($"{nameof(PhonyBrokerage)}.{nameof(PlaceCrossZeroOrder)}");

                // Step 1: Create order request under the hood of any brokerage
                var brokeragePhonyParameterRequest = new PhonyPlaceOrderRequest(crossZeroOrderRequest.LeanOrder.Symbol.Value, crossZeroOrderRequest.OrderQuantity,
                    crossZeroOrderRequest.LeanOrder.Direction, 0m, crossZeroOrderRequest.OrderType);

                // Step 2: Place the order request, paying attention to the flag 'isPlaceOrderWithoutLeanEvent'
                var response = PlaceOrderPhonyBrokerage(crossZeroOrderRequest.LeanOrder, isPlaceOrderWithoutLeanEvent, brokeragePhonyParameterRequest);

                // Step 3: Return the result of placing the order
                return new CrossZeroOrderResponse(response.OrderId, response.IsOrderPlacedSuccessfully, response.Message);
            }

            /// <summary>
            /// Places an order with the PhonyBrokerage.
            /// </summary>
            /// <param name="originalLeanOrder">The original Lean order.</param>
            /// <param name="isSubmittedEvent">Flag indicating whether to trigger the order submitted event.</param>
            /// <param name="orderRequest">The order request parameters.</param>
            /// <returns>
            /// A <see cref="PhonyPlaceOrderResponse"/> containing the result of placing the order.
            /// </returns>
            private PhonyPlaceOrderResponse PlaceOrderPhonyBrokerage(Order originalLeanOrder, bool isSubmittedEvent = true, PhonyPlaceOrderRequest orderRequest = default)
            {
                var newOrderId = Guid.NewGuid().ToString();
                _tempBrokerageOrderIds.Add(newOrderId);

                if (isSubmittedEvent)
                {
                    OnOrderEvent(new OrderEvent(originalLeanOrder, new DateTime(2024, 6, 10), OrderFee.Zero) { Status = OrderStatus.Submitted });
                }

                if (IsPlaceOrderPhonyBrokerageFirstPartSuccessfully)
                {
                    IsPlaceOrderPhonyBrokerageFirstPartSuccessfully = false;

                    return new PhonyPlaceOrderResponse(newOrderId, true);
                }

                if (IsPlaceOrderPhonyBrokerageSecondPartSuccessfully)
                {
                    return new PhonyPlaceOrderResponse(newOrderId, true);
                }

                return new PhonyPlaceOrderResponse(newOrderId, false, "Something was wrong");
            }

            public override void Dispose()
            {
                _cancellationTokenSource.Dispose();
                base.Dispose();
            }

            public override bool UpdateOrder(Order order)
            {
                OnOrderEvent(new OrderEvent(order, new DateTime(2024, 6, 10), OrderFee.Zero, $"{nameof(PhonyBrokerage)} Order Event")
                {
                    Status = OrderStatus.UpdateSubmitted
                });
                return true;
            }

            /// <summary>
            /// Simulates a brokerage sending order update events at regular intervals.
            /// </summary>
            /// <remarks>
            /// This method starts a new long-running task that periodically checks for open orders
            /// and updates their status. Specifically, it transitions orders with statuses 
            /// <see cref="OrderStatus.Submitted"/> or <see cref="OrderStatus.PartiallyFilled"/> 
            /// to <see cref="OrderStatus.Filled"/> after a fixed delay.
            /// </remarks>
            /// <example>
            /// <code>
            /// // Example usage
            /// var brokerage = new Brokerage();
            /// brokerage.ImitationBrokerageOrderUpdates();
            /// </code>
            /// </example>
            /// <exception cref="OperationCanceledException">
            /// Thrown if the operation is canceled via the cancellation token.
            /// </exception>
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
                            if (order.Status == OrderStatus.Submitted || order.Status == OrderStatus.PartiallyFilled || order.Status == OrderStatus.UpdateSubmitted)
                            {
                                OnOrderEvent(new OrderEvent(order, new DateTime(2024, 6, 10), OrderFee.Zero) { Status = OrderStatus.Filled });
                            }
                        }
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            /// <summary>
            /// Represents a request to place an order in the Phony Brokerage system.
            /// </summary>
            private protected readonly struct PhonyPlaceOrderRequest
            {
                /// <summary>
                /// Gets the symbol for the order.
                /// </summary>
                public string Symbol { get; }

                /// <summary>
                /// Gets the quantity for the order.
                /// </summary>
                public decimal Quantity { get; }

                /// <summary>
                /// Gets the direction of the order.
                /// </summary>
                public OrderPosition Direction { get; }

                /// <summary>
                /// Gets the custom brokerage order type.
                /// </summary>
                public OrderType CustomBrokerageOrderType { get; }

                /// <summary>
                /// Initializes a new instance of the <see cref="PhonyPlaceOrderRequest"/> struct.
                /// </summary>
                /// <param name="symbol">The symbol for the order.</param>
                /// <param name="quantity">The quantity for the order.</param>
                /// <param name="orderDirection">The direction of the order.</param>
                /// <param name="holdingQuantity">The quantity currently held.</param>
                /// <param name="leanOrderType">The type of the order.</param>
                public PhonyPlaceOrderRequest(string symbol, decimal quantity, OrderDirection orderDirection, decimal holdingQuantity, OrderType leanOrderType)
                {
                    Symbol = symbol;
                    Quantity = quantity;
                    Direction = GetOrderPosition(orderDirection, holdingQuantity);
                    CustomBrokerageOrderType = leanOrderType;
                }
            }

            /// <summary>
            /// Represents a response from placing an order in the Phony Brokerage system.
            /// </summary>
            private protected readonly struct PhonyPlaceOrderResponse
            {
                /// <summary>
                /// Gets the unique identifier for the placed order.
                /// </summary>
                public string OrderId { get; }

                /// <summary>
                /// Gets a value indicating whether the order was placed successfully.
                /// </summary>
                public bool IsOrderPlacedSuccessfully { get; }

                /// <summary>
                /// Gets the message associated with the order response.
                /// </summary>
                public string Message { get; }

                /// <summary>
                /// Initializes a new instance of the <see cref="PhonyPlaceOrderResponse"/> struct.
                /// </summary>
                /// <param name="orderId">The unique identifier for the placed order.</param>
                /// <param name="isOrderPlacedSuccessfully">A value indicating whether the order was placed successfully.</param>
                /// <param name="message">The message associated with the order response. This parameter is optional and defaults to <c>null</c>.</param>
                public PhonyPlaceOrderResponse(string orderId, bool isOrderPlacedSuccessfully, string message = null)
                {
                    OrderId = orderId;
                    IsOrderPlacedSuccessfully = isOrderPlacedSuccessfully;
                    Message = message;
                }
            }
        }

        /// <inheritdoc cref="OrderProvider"/>
        private protected class CustomOrderProvider : OrderProvider
        {
            public void UpdateOrderStatusById(int orderId, OrderStatus newOrderStatus)
            {
                var order = _orders.First(x => x.Id == orderId);
                order.Status = newOrderStatus;
            }
        }
    }
}
