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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Orders;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Services.OrderPolling;
using QuantConnect.Brokerages.Services.OrderPolling.Models;

namespace QuantConnect.Tests.Brokerages.Services.OrderPolling
{
    [TestFixture]
    public class BrokerageOrderPollingServiceTests
    {
        private OrderProvider _orderProvider;
        private AllOrdersPollingService _service;
        private List<OrderEvent> _orderEvents;

        [SetUp]
        public void SetUp()
        {
            _orderProvider = new OrderProvider();
            _orderEvents = new List<OrderEvent>();
            // the read is unused: these tests drive the diff directly through ProcessOrderState
            _service = new AllOrdersPollingService(() => Array.Empty<BrokerOrderState>(), messageHandler: null, _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(50), watchTimeout: TimeSpan.FromMilliseconds(120));
            _service.OrderEvents += (_, orderEvents) => _orderEvents.AddRange(orderEvents);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        private Order AddOrder(decimal quantity, string brokerageId, OrderStatus status = OrderStatus.New)
        {
            var order = new MarketOrder(Symbols.AAPL, quantity, new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc));
            order.Status = status;
            order.BrokerId.Add(brokerageId);
            _orderProvider.Add(order);
            return order;
        }

        private static BrokerOrderState State(string brokerageId, OrderStatus status, decimal? filled = null, decimal? price = null, string message = null)
        {
            return new BrokerOrderState(brokerageId, status, new DateTime(2026, 8, 12, 14, 30, 0, DateTimeKind.Utc),
                filledQuantity: filled, fillPrice: price, message: message);
        }

        [Test]
        public void SubmitIsEmittedOnce()
        {
            AddOrder(100m, "42");

            _service.ProcessOrderState(State("42", OrderStatus.Submitted));

            Assert.AreEqual(1, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.Submitted, _orderEvents[0].Status);

            // the same state again reports nothing new
            _service.ProcessOrderState(State("42", OrderStatus.Submitted));
            Assert.AreEqual(1, _orderEvents.Count);
        }

        [Test]
        public void FirstStateAlreadyFilledEmitsSubmitBeforeFill()
        {
            AddOrder(100m, "42");

            // a market order can already be filled the first time a poll sees it
            _service.ProcessOrderState(State("42", OrderStatus.Filled, filled: 100m, price: 310m));

            Assert.AreEqual(2, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.Submitted, _orderEvents[0].Status);
            Assert.AreEqual(OrderStatus.Filled, _orderEvents[1].Status);
            Assert.AreEqual(100m, _orderEvents[1].FillQuantity);
            Assert.AreEqual(310m, _orderEvents[1].FillPrice);
        }

        [Test]
        public void ReplacementWatchReportsUpdateSubmittedOnce()
        {
            // a replace moved the order onto a new brokerage id, and the plugin marked the new id
            var order = AddOrder(100m, "42", OrderStatus.Submitted);
            order.BrokerId.Clear();
            order.BrokerId.Add("43");
            _service.WatchReplacement("43", "42");

            _service.ProcessOrderState(State("43", OrderStatus.Submitted));

            var updateSubmit = _orderEvents.Single();
            Assert.AreEqual(OrderStatus.UpdateSubmitted, updateSubmit.Status);
            Assert.AreEqual("Update submitted by polling", updateSubmit.Message);

            // the same state again reports nothing new
            _service.ProcessOrderState(State("43", OrderStatus.Submitted));
            Assert.AreEqual(1, _orderEvents.Count);
        }

        [Test]
        public void ReplacementWatchDropsThePreviousIdAndCountsFillsFromZero()
        {
            // the old id already reported a fill, then the replace re-keys the order. The replacement
            // counts its executions from zero, so its first fill must not be shrunk by the old total.
            var order = AddOrder(200m, "42", OrderStatus.Submitted);
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            Assert.AreEqual(100m, _orderEvents.Single().FillQuantity);

            order.BrokerId.Clear();
            order.BrokerId.Add("43");
            _service.WatchReplacement("43", "42");
            _orderEvents.Clear();

            // the first state of the new id can already carry a fill: the update submit still goes first
            _service.ProcessOrderState(State("43", OrderStatus.PartiallyFilled, filled: 40m, price: 311m));

            Assert.AreEqual(2, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.UpdateSubmitted, _orderEvents[0].Status);
            Assert.AreEqual(OrderStatus.PartiallyFilled, _orderEvents[1].Status);
            Assert.AreEqual(40m, _orderEvents[1].FillQuantity);

            // the old order's own end no longer resolves to the Lean order, so it reports nothing
            _service.ProcessOrderState(State("42", OrderStatus.Canceled));
            Assert.AreEqual(2, _orderEvents.Count);
        }

        [Test]
        public void LiveOrderWithoutReplacementMarkStaysQuiet()
        {
            // an order the stream already confirmed, or one adopted at startup: it is past New in Lean
            // and its id carries no replacement mark, so a sweep seeing it for the first time stays quiet
            AddOrder(100m, "42", OrderStatus.Submitted);

            _service.ProcessOrderState(State("42", OrderStatus.Submitted));

            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void CumulativeFillsNeverRepeat()
        {
            // the ADR's worked example: long 1000, two 100-share fills at the same price
            var order = AddOrder(1000m, "42", OrderStatus.Submitted);
            order.Status = OrderStatus.Submitted;

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 200m, price: 310m));
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 200m, price: 310m));

            var fills = _orderEvents.Where(orderEvent => orderEvent.FillQuantity != 0).ToList();
            Assert.AreEqual(2, fills.Count);
            Assert.IsTrue(fills.All(orderEvent => orderEvent.FillQuantity == 100m && orderEvent.FillPrice == 310m));
            Assert.IsTrue(fills.All(orderEvent => orderEvent.Status == OrderStatus.PartiallyFilled));
        }

        [Test]
        public void ShrinkingFilledQuantityEmitsNothing()
        {
            // a broker glitch: the cumulative total drops and comes back. Nothing below or at what was
            // already reported may produce an event.
            AddOrder(1000m, "42", OrderStatus.Submitted);

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 50m, price: 310m));
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            var fills = _orderEvents.Where(orderEvent => orderEvent.FillQuantity != 0).ToList();
            Assert.AreEqual(1, fills.Count);
            Assert.AreEqual(100m, fills[0].FillQuantity);
        }

        [Test]
        public void StreamWriteBelowTheReportedTotalNeverShrinksIt()
        {
            AddOrder(1000m, "42", OrderStatus.Submitted);

            // the poll reported 100, then the stream writes an older state with 50
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            _service.UpdateOrderState("42", State("42", OrderStatus.PartiallyFilled, filled: 50m, price: 310m));

            // the next sweep at 100 repeats nothing: the reported total never moved backwards
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            var fills = _orderEvents.Where(orderEvent => orderEvent.FillQuantity != 0).ToList();
            Assert.AreEqual(1, fills.Count);
        }

        [Test]
        public void FilledQuantityWithoutPriceEmitsNoFill()
        {
            // a read that carries the quantity but no price cannot produce a fill event - the service
            // never invents a number. The part stays unreported, so it goes out once the price arrives.
            AddOrder(100m, "42", OrderStatus.Submitted);

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 40m));
            Assert.IsEmpty(_orderEvents);

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 40m, price: 310m));
            var fill = _orderEvents.Single();
            Assert.AreEqual(40m, fill.FillQuantity);
            Assert.AreEqual(310m, fill.FillPrice);
        }

        [Test]
        public void FilledStatusWithoutNumbersEmitsSubmitOnly()
        {
            // the thinnest read: only the id and a status. The service emits what those two can prove
            // and never closes an order without its fill numbers.
            AddOrder(100m, "42");

            _service.ProcessOrderState(State("42", OrderStatus.Filled));

            Assert.AreEqual(1, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.Submitted, _orderEvents[0].Status);
        }

        [Test]
        public void SellOrderFillsAreSignedByDirection()
        {
            AddOrder(-100m, "42", OrderStatus.Submitted);

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 40m, price: 310m));

            var fill = _orderEvents.Single(orderEvent => orderEvent.FillQuantity != 0);
            Assert.AreEqual(-40m, fill.FillQuantity);
        }

        [Test]
        public void SharedIdComboSplitsFillsByGroupQuantity()
        {
            // one brokerage id, two Lean leg orders: 5 strangles, put leg ratio 1 (quantity 5),
            // call leg ratio 2 (quantity 10)
            var groupOrderManager = new GroupOrderManager(1, legCount: 2, quantity: 5m);
            var time = new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc);
            var putLeg = new ComboMarketOrder(Symbols.SPY_P_192_Feb19_2016, 5m, time, groupOrderManager);
            var callLeg = new ComboMarketOrder(Symbols.SPY_C_192_Feb19_2016, 10m, time, groupOrderManager);
            foreach (var leg in new[] { putLeg, callLeg })
            {
                leg.Status = OrderStatus.Submitted;
                leg.BrokerId.Add("900");
                _orderProvider.Add(leg);
            }

            // the broker reports 2 of 5 strangles filled, one number for the whole combo
            _service.ProcessOrderState(State("900", OrderStatus.PartiallyFilled, filled: 2m, price: 3.5m));

            Assert.AreEqual(2, _orderEvents.Count);
            Assert.AreEqual(2m, _orderEvents.Single(orderEvent => orderEvent.Symbol == putLeg.Symbol).FillQuantity);
            Assert.AreEqual(4m, _orderEvents.Single(orderEvent => orderEvent.Symbol == callLeg.Symbol).FillQuantity);
            Assert.IsTrue(_orderEvents.All(orderEvent => orderEvent.Status == OrderStatus.PartiallyFilled));

            // the rest fills: 3 more strangles complete the order
            _orderEvents.Clear();
            _service.ProcessOrderState(State("900", OrderStatus.Filled, filled: 5m, price: 3.6m));

            Assert.AreEqual(2, _orderEvents.Count);
            Assert.AreEqual(3m, _orderEvents.Single(orderEvent => orderEvent.Symbol == putLeg.Symbol).FillQuantity);
            Assert.AreEqual(6m, _orderEvents.Single(orderEvent => orderEvent.Symbol == callLeg.Symbol).FillQuantity);
            Assert.IsTrue(_orderEvents.All(orderEvent => orderEvent.Status == OrderStatus.Filled));
        }

        [Test]
        public void ComboWithOneLegClosedInLeanSplitsOnlyToTheOpenLeg()
        {
            var groupOrderManager = new GroupOrderManager(1, legCount: 2, quantity: 5m);
            var time = new DateTime(2026, 8, 12, 14, 0, 0, DateTimeKind.Utc);
            var putLeg = new ComboMarketOrder(Symbols.SPY_P_192_Feb19_2016, 5m, time, groupOrderManager);
            var callLeg = new ComboMarketOrder(Symbols.SPY_C_192_Feb19_2016, 10m, time, groupOrderManager);
            foreach (var leg in new[] { putLeg, callLeg })
            {
                leg.BrokerId.Add("900");
                _orderProvider.Add(leg);
            }
            // Lean already applied the put leg's fill events, the call leg is still working
            putLeg.Status = OrderStatus.Filled;
            callLeg.Status = OrderStatus.Submitted;

            _service.ProcessOrderState(State("900", OrderStatus.PartiallyFilled, filled: 3m, price: 3.5m));

            var fill = _orderEvents.Single();
            Assert.AreEqual(callLeg.Symbol, fill.Symbol);
            Assert.AreEqual(6m, fill.FillQuantity);

            // the same state again reports nothing - the entry survived, the totals moved
            _orderEvents.Clear();
            _service.ProcessOrderState(State("900", OrderStatus.PartiallyFilled, filled: 3m, price: 3.5m));
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void MultipleOrdersOnOneIdWithoutAGroupQuantityEmitNoFillAndDoNotThrow()
        {
            // two Lean orders behind one brokerage id but no group manager: the service cannot split the
            // fill, so it skips it instead of guessing or dividing by zero
            AddOrder(100m, "900", OrderStatus.Submitted);
            AddOrder(200m, "900", OrderStatus.Submitted);

            Assert.DoesNotThrow(() => _service.ProcessOrderState(State("900", OrderStatus.PartiallyFilled, filled: 10m, price: 5m)));
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void TerminalSeedIsNotRepeated()
        {
            AddOrder(100m, "42", OrderStatus.Submitted);

            // another path already reported the cancel; the watch moves the state, e.g. across a replace
            _service.Watch("42", State("42", OrderStatus.Canceled, message: "canceled by the broker"));

            _service.ProcessOrderState(State("42", OrderStatus.Canceled, message: "canceled by the broker"));
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void TerminalIsEmittedOnceWithMessageAndAfterFills()
        {
            AddOrder(100m, "42", OrderStatus.Submitted);

            // one state carries a last fill and the cancel: the fill must go out first
            _service.ProcessOrderState(State("42", OrderStatus.Canceled, filled: 30m, price: 310m, message: "canceled by the broker"));

            Assert.AreEqual(2, _orderEvents.Count);
            Assert.AreEqual(30m, _orderEvents[0].FillQuantity);
            Assert.AreEqual(OrderStatus.Canceled, _orderEvents[1].Status);
            Assert.AreEqual("canceled by the broker", _orderEvents[1].Message);

            // the same state on the next sweep reports nothing: the fill is at the reported total and
            // the end already went out
            _service.ProcessOrderState(State("42", OrderStatus.Canceled, filled: 30m, price: 310m, message: "canceled by the broker"));
            Assert.AreEqual(2, _orderEvents.Count);
        }

        [Test]
        public void StateIsDroppedOnlyOnceLeanClosedTheOrder()
        {
            var order = AddOrder(100m, "42", OrderStatus.Submitted);

            _service.ProcessOrderState(State("42", OrderStatus.Canceled, filled: 30m, price: 310m));
            Assert.IsTrue(_service.TryGetLastOrderState("42", out _));

            // Lean applied the cancel: the next compare sees the order closed and drops the state
            order.Status = OrderStatus.Canceled;
            _service.ProcessOrderState(State("42", OrderStatus.Canceled, filled: 30m, price: 310m));
            Assert.IsFalse(_service.TryGetLastOrderState("42", out _));
        }

        [Test]
        public void UnknownBrokerageIdWritesNothing()
        {
            _service.ProcessOrderState(State("77", OrderStatus.Filled, filled: 100m, price: 310m));

            Assert.IsEmpty(_orderEvents);
            Assert.IsFalse(_service.TryGetLastOrderState("77", out _));
        }

        [Test]
        public void RejectDoesNotEmitSubmit()
        {
            AddOrder(100m, "42");

            _service.ProcessOrderState(State("42", OrderStatus.Invalid, message: "rejected"));

            Assert.AreEqual(1, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.Invalid, _orderEvents[0].Status);
            Assert.AreEqual("rejected", _orderEvents[0].Message);
        }

        [Test]
        public void SeededWatchDoesNotRepeatWhatWasAlreadyReported()
        {
            AddOrder(200m, "42", OrderStatus.PartiallyFilled);

            // another path already reported the submit and 100 shares
            _service.Watch("42", State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            Assert.IsEmpty(_orderEvents);

            // only the part above the seed is new
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 150m, price: 311m));
            var fill = _orderEvents.Single();
            Assert.AreEqual(50m, fill.FillQuantity);
            Assert.AreEqual(311m, fill.FillPrice);
        }

        [Test]
        public void StreamWriteThroughUpdateOrderStateIsNotRepeatedByThePoll()
        {
            AddOrder(200m, "42", OrderStatus.PartiallyFilled);

            // the stream reported a fill and wrote it into the shared registry
            _service.UpdateOrderState("42", State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            // the next sweep sees the same broker numbers and repeats nothing
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            Assert.IsEmpty(_orderEvents);

            Assert.IsTrue(_service.TryGetLastOrderState("42", out var lastSeen));
            Assert.AreEqual(100m, lastSeen.FilledQuantity);
        }

        [Test]
        public void StreamReportedTerminalIsNotRepeatedByThePoll()
        {
            AddOrder(100m, "42", OrderStatus.Submitted);

            // the stream reported the cancel and wrote it into the shared registry, but Lean has not
            // applied it yet when the next sweep lands
            _service.UpdateOrderState("42", State("42", OrderStatus.Canceled, message: "canceled by the broker"));

            _service.ProcessOrderState(State("42", OrderStatus.Canceled, message: "canceled by the broker"));
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void WatchNeverOverwritesExistingState()
        {
            AddOrder(200m, "42", OrderStatus.PartiallyFilled);
            _service.UpdateOrderState("42", State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            // a later plain watch keeps the recorded state
            _service.Watch("42");

            Assert.IsTrue(_service.TryGetLastOrderState("42", out var lastSeen));
            Assert.AreEqual(100m, lastSeen.FilledQuantity);

            // and a later seeded watch ignores its seed too: the recorded state wins, so the next poll
            // cannot re-report fills the stream already delivered
            _service.Watch("42", State("42", OrderStatus.Submitted, filled: 0m));

            Assert.IsTrue(_service.TryGetLastOrderState("42", out lastSeen));
            Assert.AreEqual(100m, lastSeen.FilledQuantity);
            _service.ProcessOrderState(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void WatchTimeoutFiresOnceAndUnwatchesTheId()
        {
            var order = AddOrder(100m, "77");
            using var neverNotified = new ManualResetEventSlim(false);
            var raised = new List<BrokerageOrderNeverNotifiedEventArgs>();
            using var service = new PerOrderIdPollingService(
                _ => null,   // the broker never knows the id
                messageHandler: null,
                _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(25),
                watchTimeout: TimeSpan.FromMilliseconds(75));
            service.BrokerageOrderNeverNotified += (_, eventArgs) =>
            {
                lock (raised)
                {
                    raised.Add(eventArgs);
                }
                neverNotified.Set();
            };

            service.Watch("77");
            service.Start();

            Assert.IsTrue(neverNotified.Wait(TimeSpan.FromSeconds(5)), "the watch timeout never fired");

            // let a few more sweeps run: the id was unwatched with the event, so it fires exactly once
            Thread.Sleep(200);
            service.Stop();

            lock (raised)
            {
                Assert.AreEqual(1, raised.Count);
                Assert.AreEqual("77", raised[0].BrokerageOrderId);
                Assert.AreSame(order, raised[0].Order);
                Assert.GreaterOrEqual(raised[0].WatchDuration, TimeSpan.FromMilliseconds(75));
            }
            Assert.IsFalse(service.TryGetLastOrderState("77", out _));
        }

        [Test]
        public void NeverNotifiedArgsPrintTheOrderAndTheWatchDuration()
        {
            var order = AddOrder(100m, "42");

            // the message the default warning embeds: the Lean order's own ToString plus the watch duration
            var withOrder = new BrokerageOrderNeverNotifiedEventArgs("42", order, TimeSpan.FromSeconds(60));
            Assert.AreEqual($"{order}, watched for 60 seconds", withOrder.ToString());

            // a placement whose id was never assigned resolves to no Lean order: the id carries the identity
            var withoutOrder = new BrokerageOrderNeverNotifiedEventArgs("42", order: null, TimeSpan.FromSeconds(60));
            Assert.AreEqual("brokerage order id '42', watched for 60 seconds", withoutOrder.ToString());
        }

        [Test]
        public void AcknowledgedWatchNeverTimesOut()
        {
            var fired = 0;
            using var service = new PerOrderIdPollingService(
                _ => null,
                messageHandler: null,
                _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(25),
                watchTimeout: TimeSpan.FromMilliseconds(75));
            service.BrokerageOrderNeverNotified += (_, _) => Interlocked.Increment(ref fired);

            // the stream acknowledged the order right after it was watched
            service.Watch("77");
            service.UpdateOrderState("77", State("77", OrderStatus.Submitted));
            service.Start();

            Thread.Sleep(300);
            service.Stop();

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void RepeatedReadFailuresRaiseOneWarningPerOutage()
        {
            var fail = true;
            var warnings = new List<BrokerageMessageEvent>();
            using var warned = new AutoResetEvent(false);
            using var service = new AllOrdersPollingService(
                () => fail ? throw new Exception("read failed") : Array.Empty<BrokerOrderState>(),
                messageHandler: null,
                _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(25));
            service.Message += (_, message) =>
            {
                lock (warnings)
                {
                    warnings.Add(message);
                }
                warned.Set();
            };

            service.Start();
            Assert.IsTrue(warned.WaitOne(TimeSpan.FromSeconds(5)), "the failure warning never fired");

            // more failing sweeps do not warn again inside the same outage
            Thread.Sleep(200);
            lock (warnings)
            {
                Assert.AreEqual(1, warnings.Count);
                Assert.AreEqual(BrokerageMessageType.Warning, warnings[0].Type);
                Assert.AreEqual("OrderPollingFailed", warnings[0].Code);
            }

            // one successful read arms the warning again, so the next outage warns once more
            fail = false;
            Thread.Sleep(200);
            fail = true;
            Assert.IsTrue(warned.WaitOne(TimeSpan.FromSeconds(5)), "the second outage never warned");

            service.Stop();
            lock (warnings)
            {
                Assert.AreEqual(2, warnings.Count);
            }
        }

        [Test]
        public void PerOrderIdSweepReadsOnlyWatchedIdsAndProcessesTheStates()
        {
            AddOrder(100m, "42");
            var readIds = new List<string>();
            using var processed = new ManualResetEventSlim(false);
            // no message handler: the loop hands each state straight into the diff
            using var service = new PerOrderIdPollingService(
                brokerageId =>
                {
                    lock (readIds)
                    {
                        readIds.Add(brokerageId);
                    }
                    return State(brokerageId, OrderStatus.Submitted);
                },
                messageHandler: null,
                _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(25));
            var events = new List<OrderEvent>();
            service.OrderEvents += (_, orderEvents) =>
            {
                lock (events)
                {
                    events.AddRange(orderEvents);
                }
                processed.Set();
            };

            // nothing watched: sweeps read nothing
            service.Start();
            Thread.Sleep(100);
            lock (readIds)
            {
                Assert.IsEmpty(readIds);
            }

            service.Watch("42");
            Assert.IsTrue(processed.Wait(TimeSpan.FromSeconds(5)), "the watched id never produced an event");
            service.Stop();

            lock (readIds)
            {
                Assert.IsTrue(readIds.All(id => id == "42"));
            }
            lock (events)
            {
                Assert.AreEqual(OrderStatus.Submitted, events[0].Status);
            }
        }

        [Test]
        public void StartAndStopAreIdempotentAndDisposeIsFinal()
        {
            Assert.IsFalse(_service.IsPolling);

            _service.Start();
            _service.Start();
            Assert.IsTrue(_service.IsPolling);

            _service.Stop();
            _service.Stop();
            Assert.IsFalse(_service.IsPolling);

            _service.Start();
            Assert.IsTrue(_service.IsPolling);
            _service.Stop();

            _service.Dispose();
            _service.Start();
            Assert.IsFalse(_service.IsPolling);
        }

        [Test]
        public void StartWithPreLoadSeedsTheRegistryThenStarts()
        {
            AddOrder(233m, "42", OrderStatus.Submitted);
            var seeded = new List<string>();

            _service.Start(order =>
            {
                seeded.Add(order.BrokerId[0]);
                // the stream already reported 100 of the 233 shares
                return State(order.BrokerId[0], order.Status, filled: 100m);
            });

            Assert.IsTrue(_service.IsPolling);
            CollectionAssert.AreEqual(new[] { "42" }, seeded);

            // the first sweep continues from the seed: only the other 133 shares are reported
            _service.ProcessOrderState(State("42", OrderStatus.Filled, filled: 233m, price: 310m));
            Assert.AreEqual(1, _orderEvents.Count);
            Assert.AreEqual(OrderStatus.Filled, _orderEvents[0].Status);
            Assert.AreEqual(133m, _orderEvents[0].FillQuantity);
        }

        [Test]
        public void StartWithPreLoadDrainsTheMessageHandlerBeforeSeeding()
        {
            AddOrder(233m, "42", OrderStatus.Submitted);
            var steps = new List<string>();
            using var handler = new BrokerageConcurrentMessageHandler();
            using var service = new AllOrdersPollingService(() => Array.Empty<BrokerOrderState>(), handler, _orderProvider,
                pollInterval: TimeSpan.FromMilliseconds(50));
            service.OrderEvents += (_, orderEvents) =>
            {
                lock (steps)
                {
                    steps.AddRange(orderEvents.Select(orderEvent => $"event:{orderEvent.FillQuantity}"));
                }
            };

            using var lockedEvent = new ManualResetEventSlim(false);
            using var releaseEvent = new ManualResetEventSlim(false);
            // an order request is in flight while the stream dies
            var orderRequest = Task.Run(() => handler.WithLockedStream(() =>
            {
                lockedEvent.Set();
                releaseEvent.Wait();
            }));
            lockedEvent.Wait();

            // the stream delivered a fill that is still buffered behind the order request
            handler.HandleNewMessage(State("42", OrderStatus.PartiallyFilled, filled: 100m, price: 310m));

            var seededStart = Task.Run(() => service.Start(openOrder =>
            {
                lock (steps)
                {
                    steps.Add("seed");
                }
                return State(openOrder.BrokerId[0], openOrder.Status, filled: 100m);
            }));

            // the handover waits for the order request instead of seeding early
            Assert.IsFalse(seededStart.Wait(TimeSpan.FromMilliseconds(100)));

            releaseEvent.Set();
            Task.WaitAll(orderRequest, seededStart);

            Assert.IsTrue(service.IsPolling);
            lock (steps)
            {
                // the buffered fill was processed before the seeds were taken
                CollectionAssert.AreEqual(new[] { "event:100", "seed" }, steps);
            }
        }

        [Test]
        public void StartWithNullPreLoadJustStarts()
        {
            _service.Start(preLoadOpenOrders: null);

            Assert.IsTrue(_service.IsPolling);
            Assert.IsEmpty(_orderEvents);
        }

        [Test]
        public void StartWithPreLoadWhilePollingDoesNothing()
        {
            AddOrder(100m, "42");
            _service.Start();

            var callCount = 0;
            _service.Start(_ => { callCount++; return null; });

            Assert.IsTrue(_service.IsPolling);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void SeedWithoutBrokerageIdIsSkipped()
        {
            AddOrder(100m, "42");
            AddOrder(100m, "43");

            _service.Start(preLoadOpenOrders: order => order.BrokerId[0] == "42"
                ? null
                : new BrokerOrderState { Status = OrderStatus.Submitted });

            Assert.IsTrue(_service.IsPolling);
            Assert.IsFalse(_service.TryGetLastOrderState("42", out _));
            Assert.IsFalse(_service.TryGetLastOrderState("43", out _));
        }
    }
}
