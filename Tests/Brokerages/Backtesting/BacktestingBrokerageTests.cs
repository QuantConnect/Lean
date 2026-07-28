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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.Backtesting
{
    /// <summary>
    /// Covers the one-cancels-the-other (OCO) group processing added to <see cref="BacktestingBrokerage.Scan"/>:
    /// <see cref="BacktestingBrokerage"/> exposes no seam to inject a controllable fill outcome, so every test
    /// here drives real fills through a small test <see cref="FillModel"/> that fills/holds a leg based on the
    /// security's current price, and reaches into the private pending-order dictionary via reflection to check
    /// the group's pending-set lifecycle, since that state is not otherwise observable from the public API.
    /// </summary>
    [TestFixture]
    public class BacktestingBrokerageTests
    {
        private static readonly DateTime ReferenceTime = new DateTime(2024, 1, 25, 15, 0, 0, DateTimeKind.Utc);
        private static readonly FieldInfo PendingOrdersField =
            typeof(BacktestingBrokerage).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Instance);

        private QCAlgorithm _algorithm;
        private Security _security;
        private BacktestingBrokerage _brokerage;
        private ControlledFillModel _fillModel;
        private List<List<OrderEvent>> _eventBatches;
        private DateTime _orderTime;

        [SetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetBrokerageModel(BrokerageName.Default);
            _algorithm.SetCash(100000);
            _security = _algorithm.AddEquity("SPY");
            _algorithm.SetDateTime(ReferenceTime);
            _algorithm.SetFinishedWarmingUp();

            _fillModel = new ControlledFillModel();
            _security.SetFillModel(_fillModel);
            SetPrice(100m);

            _brokerage = new BacktestingBrokerage(_algorithm);
            _eventBatches = new List<List<OrderEvent>>();
            _brokerage.OrdersStatusChanged += (_, orderEvents) => _eventBatches.Add(orderEvents);

            // legs must not be submitted on the same bar as "now", or Scan() defers them to the next pass
            _orderTime = ReferenceTime.AddMinutes(-1);
        }

        [TearDown]
        public void TearDown()
        {
            _brokerage?.Dispose();
        }

        [Test]
        public void LegFillCancelsSiblingInSameEventBatch()
        {
            // the limit leg touches at 100, the stop leg (120) does not
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 100m, stopPrice: 120m);

            _brokerage.Scan();

            Assert.AreEqual(1, _eventBatches.Count);
            var batch = _eventBatches.Single();
            Assert.AreEqual(2, batch.Count);

            var filledEvent = batch.Single(e => e.OrderId == limitOrder.Id);
            var canceledEvent = batch.Single(e => e.OrderId == stopOrder.Id);
            Assert.AreEqual(OrderStatus.Filled, filledEvent.Status);
            Assert.AreEqual(OrderStatus.Canceled, canceledEvent.Status);
            Assert.AreEqual("OCO", canceledEvent.Message);

            Assert.AreEqual(OrderStatus.Filled, limitOrder.Status);
            Assert.AreEqual(OrderStatus.Canceled, stopOrder.Status);

            // the group leaves the pending set only once every leg is closed
            var pending = GetPendingOrders();
            Assert.IsFalse(pending.ContainsKey(limitOrder.Id));
            Assert.IsFalse(pending.ContainsKey(stopOrder.Id));
        }

        [Test]
        public void StopLegWinsTieOverLimitLeg()
        {
            // both legs touch on the same bar (100 == 100): the fixed evaluation order (stop-type legs
            // first, then limit legs) must make the stop leg the deterministic winner
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 100m, stopPrice: 100m);

            _brokerage.Scan();

            Assert.AreEqual(1, _eventBatches.Count);
            Assert.AreEqual(2, _eventBatches.Single().Count);

            Assert.AreEqual(OrderStatus.Filled, stopOrder.Status);
            Assert.AreEqual(OrderStatus.Canceled, limitOrder.Status);

            var canceledEvent = _eventBatches.Single().Single(e => e.OrderId == limitOrder.Id);
            Assert.AreEqual("OCO", canceledEvent.Message);

            // the limit leg's fill model must never even be asked to fill: the stop leg won first
            Assert.AreEqual(0, _fillModel.LimitFillInvocations);
        }

        [Test]
        public void CancelingOneLegCancelsWholeGroup()
        {
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 50m, stopPrice: 150m);

            var result = _brokerage.CancelOrder(limitOrder);
            ApplyEventsToOrders(limitOrder, stopOrder);

            Assert.IsTrue(result);
            Assert.AreEqual(OrderStatus.Canceled, limitOrder.Status);
            Assert.AreEqual(OrderStatus.Canceled, stopOrder.Status);

            // CancelOrder fires one event per leg (not a single combined batch like Scan() does)
            Assert.AreEqual(2, _eventBatches.Count);
            Assert.IsTrue(_eventBatches.All(batch => batch.Count == 1 && batch[0].Status == OrderStatus.Canceled));

            var pending = GetPendingOrders();
            Assert.IsFalse(pending.ContainsKey(limitOrder.Id));
            Assert.IsFalse(pending.ContainsKey(stopOrder.Id));
        }

        [Test]
        public void CancelOrderLeavesAlreadyClosedLegUntouched()
        {
            // Under the shipped design a group is always either fully pending or fully removed
            // (ProcessOneCancelsTheOtherGroup resolves fill+cancel-siblings+remove-if-closed atomically), so a
            // pending set with one closed leg next to an open sibling cannot arise from Scan()/CancelOrder alone.
            // We set that precondition directly here to exercise CancelOrder's defensive "already closed" guard.
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 50m, stopPrice: 150m);
            stopOrder.Status = OrderStatus.Filled;

            var result = _brokerage.CancelOrder(limitOrder);
            ApplyEventsToOrders(limitOrder, stopOrder);

            Assert.IsTrue(result);
            Assert.AreEqual(OrderStatus.Canceled, limitOrder.Status);
            // the already-closed leg must not be overwritten back to Canceled
            Assert.AreEqual(OrderStatus.Filled, stopOrder.Status);

            Assert.AreEqual(1, _eventBatches.Count);
            var batch = _eventBatches.Single();
            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual(limitOrder.Id, batch[0].OrderId);

            var pending = GetPendingOrders();
            Assert.IsFalse(pending.ContainsKey(limitOrder.Id));
            // left untouched: the guard skips it before it is ever removed
            Assert.IsTrue(pending.ContainsKey(stopOrder.Id));
        }

        [Test]
        public void TimeInForceExpiryOnAnyLegCancelsWholeGroup()
        {
            var properties = new OrderProperties { TimeInForce = TimeInForce.GoodTilDate(ReferenceTime.AddDays(-10)) };
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 50m, stopPrice: 150m, properties: properties);

            _brokerage.Scan();
            ApplyEventsToOrders(limitOrder, stopOrder);

            Assert.AreEqual(1, _eventBatches.Count);
            var batch = _eventBatches.Single();
            Assert.AreEqual(2, batch.Count);
            Assert.IsTrue(batch.All(e => e.Status == OrderStatus.Canceled));
            Assert.IsTrue(batch.All(e => e.Message.Contains("expired")));

            Assert.AreEqual(OrderStatus.Canceled, limitOrder.Status);
            Assert.AreEqual(OrderStatus.Canceled, stopOrder.Status);

            var pending = GetPendingOrders();
            Assert.IsFalse(pending.ContainsKey(limitOrder.Id));
            Assert.IsFalse(pending.ContainsKey(stopOrder.Id));
        }

        [Test]
        public void PartialFillLeavesSiblingsOpenAndGroupStaysPending()
        {
            _fillModel.LimitPartialFillQuantity = 5m;
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 100m, stopPrice: 150m);

            _brokerage.Scan();

            Assert.AreEqual(1, _eventBatches.Count);
            var batch = _eventBatches.Single();
            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual(limitOrder.Id, batch[0].OrderId);
            Assert.AreEqual(OrderStatus.PartiallyFilled, batch[0].Status);

            Assert.AreEqual(OrderStatus.PartiallyFilled, limitOrder.Status);
            // the sibling is untouched: still open, no cancel event fired for it
            Assert.AreEqual(OrderStatus.Submitted, stopOrder.Status);

            // the group stays in the pending set - Scan() must keep finding both legs next time around
            var pending = GetPendingOrders();
            Assert.IsTrue(pending.ContainsKey(limitOrder.Id));
            Assert.IsTrue(pending.ContainsKey(stopOrder.Id));
        }

        [Test]
        public void GroupIsProcessedOnlyOnceExactlyPerScanDespiteTwoPendingEntries()
        {
            // _pending has one dictionary entry per leg (2 entries for this single group); without the
            // processedGroupIds guard in Scan(), the group would be evaluated twice in the same pass
            var (limitOrder, stopOrder) = PlaceOcoGroup(limitPrice: 50m, stopPrice: 150m);

            _brokerage.Scan();

            Assert.AreEqual(1, _fillModel.LimitFillInvocations);
            Assert.AreEqual(1, _fillModel.StopFillInvocations);

            // neither leg actually touched, so nothing should have fired
            Assert.AreEqual(0, _eventBatches.Count);
            Assert.AreEqual(OrderStatus.Submitted, limitOrder.Status);
            Assert.AreEqual(OrderStatus.Submitted, stopOrder.Status);
        }

        private void SetPrice(decimal price)
        {
            _security.SetMarketPrice(new Tick(ReferenceTime, _security.Symbol, price, price));
        }

        private ConcurrentDictionary<int, Order> GetPendingOrders()
        {
            return (ConcurrentDictionary<int, Order>)PendingOrdersField.GetValue(_brokerage);
        }

        /// <summary>
        /// Applies every fired order event in <see cref="_eventBatches"/> back onto the matching Order instance.
        /// In production this is the transaction handler's job; there is none in this test, so tests that call
        /// <see cref="BacktestingBrokerage.CancelOrder"/> directly (which only fires events, it never mutates the
        /// Order objects itself) need this to see the resulting status on the Order instances they hold
        /// </summary>
        private void ApplyEventsToOrders(params Order[] orders)
        {
            var ordersById = orders.ToDictionary(o => o.Id);
            foreach (var orderEvent in _eventBatches.SelectMany(batch => batch))
            {
                if (ordersById.TryGetValue(orderEvent.OrderId, out var order))
                {
                    order.Status = orderEvent.Status;
                }
            }
        }

        /// <summary>
        /// Builds and places a 2-leg one-cancels-the-other group (one Limit leg, one StopMarket leg, both buy
        /// orders on the same security) directly against the brokerage, mirroring the SubmitOrderRequest/
        /// GroupOrderManager wiring QCAlgorithm.OneCancelsTheOtherOrder produces
        /// </summary>
        private (Order Limit, Order Stop) PlaceOcoGroup(decimal limitPrice, decimal stopPrice, decimal quantity = 10m,
            IOrderProperties properties = null)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, quantity) { ComboType = ComboType.OneCancelsTheOther };

            var limitRequest = new SubmitOrderRequest(OrderType.Limit, _security.Type, _security.Symbol, quantity, 0, limitPrice,
                _orderTime, "", properties, groupOrderManager);
            limitRequest.SetOrderId(1);

            var stopRequest = new SubmitOrderRequest(OrderType.StopMarket, _security.Type, _security.Symbol, quantity, stopPrice, 0,
                _orderTime, "", properties, groupOrderManager);
            stopRequest.SetOrderId(2);

            var limitOrder = Order.CreateOrder(limitRequest);
            var stopOrder = Order.CreateOrder(stopRequest);

            // BuyingPowerModel.HasSufficientBuyingPowerForOrder looks up an order's ticket via the order
            // processor, so one must be wired up even though nothing else in this bare-bones setup needs it
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOrderTicket(1)).Returns(new OrderTicket(_algorithm.Transactions, limitRequest));
            orderProcessorMock.Setup(m => m.GetOrderTicket(2)).Returns(new OrderTicket(_algorithm.Transactions, stopRequest));
            _algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            _brokerage.PlaceOrder(limitOrder);
            _brokerage.PlaceOrder(stopOrder);

            // PlaceOrder only fires the Submitted OrderEvent; in production the transaction handler is the one
            // that applies it back onto the Order it is holding. There is no transaction handler in this test,
            // so we apply it directly to keep the two Order instances consistent with what Scan() will see.
            limitOrder.Status = OrderStatus.Submitted;
            stopOrder.Status = OrderStatus.Submitted;

            // drop the two Submitted events fired by PlaceOrder so each test starts from a clean slate
            _eventBatches.Clear();

            return (limitOrder, stopOrder);
        }

        /// <summary>
        /// A fill model whose Limit/StopMarket fills are driven only by the security's current price, so tests
        /// can force a fill (or a partial fill, or no fill) deterministically without needing real bar/tick
        /// market-hours mechanics. Also counts invocations so a test can prove a leg's fill model was (or was
        /// not) asked to fill on a given Scan() pass.
        /// </summary>
        private class ControlledFillModel : FillModel
        {
            public decimal? LimitPartialFillQuantity { get; set; }

            public int LimitFillInvocations { get; private set; }

            public int StopFillInvocations { get; private set; }

            public override OrderEvent LimitFill(Security asset, LimitOrder order)
            {
                LimitFillInvocations++;

                var fill = new OrderEvent(order, asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone), OrderFee.Zero);
                var touched = order.Direction == OrderDirection.Buy
                    ? asset.Price <= order.LimitPrice
                    : asset.Price >= order.LimitPrice;

                if (!touched)
                {
                    return fill;
                }

                if (LimitPartialFillQuantity.HasValue)
                {
                    fill.Status = OrderStatus.PartiallyFilled;
                    fill.FillQuantity = Math.Sign(order.Quantity) * LimitPartialFillQuantity.Value;
                }
                else
                {
                    fill.Status = OrderStatus.Filled;
                    fill.FillQuantity = order.Quantity;
                }
                fill.FillPrice = asset.Price;

                return fill;
            }

            public override OrderEvent StopMarketFill(Security asset, StopMarketOrder order)
            {
                StopFillInvocations++;

                var fill = new OrderEvent(order, asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone), OrderFee.Zero);
                var touched = order.Direction == OrderDirection.Buy
                    ? asset.Price >= order.StopPrice
                    : asset.Price <= order.StopPrice;

                if (touched)
                {
                    fill.Status = OrderStatus.Filled;
                    fill.FillPrice = asset.Price;
                    fill.FillQuantity = order.Quantity;
                }

                return fill;
            }
        }
    }
}
