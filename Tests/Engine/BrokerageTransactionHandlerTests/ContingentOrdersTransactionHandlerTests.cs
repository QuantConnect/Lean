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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    /// <summary>
    /// End to end tests of contingent orders through the algorithm api, the transaction handler and the backtesting brokerage
    /// </summary>
    [TestFixture]
    public class ContingentOrdersTransactionHandlerTests
    {
        private BrokerageTransactionHandlerTests.TestAlgorithm _algorithm;
        private BrokerageTransactionHandler _transactionHandler;
        private BacktestingBrokerage _brokerage;
        private Security _security;
        private Symbol _symbol;
        private DateTime _time;

        [SetUp]
        public void SetUp()
        {
            _time = new DateTime(2024, 1, 3, 15, 0, 0);
            _algorithm = new BrokerageTransactionHandlerTests.TestAlgorithm
            {
                HistoryProvider = new BrokerageTransactionHandlerTests.EmptyHistoryProvider()
            };
            _algorithm.SetCash(1000000);
            _algorithm.SetDateTime(_time);
            _security = _algorithm.AddSecurity(SecurityType.Forex, "EURUSD");
            _symbol = _security.Symbol;
            _algorithm.Portfolio.CashBook["EUR"].ConversionRate = 1.1m;

            Initialize(new BacktestingBrokerage(_algorithm));
            SetPrice(1.10m);
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler?.Exit();
            _brokerage?.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BracketLifecycle(bool takeProfitFills)
        {
            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            var entry = tickets[0];
            var takeProfit = tickets[1];
            var stopLoss = tickets[2];

            Step(1.10m);
            Assert.IsTrue(tickets.All(x => x.Status == OrderStatus.Submitted));
            Assert.IsFalse(entry.Contingency.IsWaitingForTrigger);
            Assert.IsTrue(takeProfit.Contingency.IsWaitingForTrigger);
            Assert.IsTrue(stopLoss.Contingency.IsWaitingForTrigger);
            // held orders are not accounted for
            Assert.AreEqual(1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));

            // the children are held even if the price goes through their prices
            Step(1.13m);
            Step(1.10m);
            Assert.IsTrue(tickets.All(x => x.Status == OrderStatus.Submitted));

            // the entry fills, triggering the children
            Step(1.08m);
            var triggeredTime = _time;
            Assert.AreEqual(OrderStatus.Filled, entry.Status);
            foreach (var child in new[] { takeProfit, stopLoss })
            {
                Assert.AreEqual(OrderStatus.Submitted, child.Status);
                Assert.IsFalse(child.Contingency.IsWaitingForTrigger);
                Assert.AreEqual(triggeredTime, child.Contingency.Links.Single(x => x.Role == ContingencyRole.Child).TriggeredTime);
            }
            // at most one will fill
            Assert.AreEqual(-1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));
            Assert.AreEqual(1000, _security.Holdings.Quantity);

            Step(takeProfitFills ? 1.13m : 1.04m);
            var filled = takeProfitFills ? takeProfit : stopLoss;
            var canceled = takeProfitFills ? stopLoss : takeProfit;
            Assert.AreEqual(OrderStatus.Filled, filled.Status);
            Assert.AreEqual(OrderStatus.Canceled, canceled.Status);
            StringAssert.Contains($"Contingent sibling order {filled.OrderId} was filled", canceled.OrderEvents.Last().Message);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());

            // the cancel event comes right after the fill
            var events = _algorithm.OrderEvents;
            var fillIndex = events.FindIndex(x => x.OrderId == filled.OrderId && x.Status == OrderStatus.Filled);
            Assert.AreEqual(canceled.OrderId, events[fillIndex + 1].OrderId);
            Assert.AreEqual(OrderStatus.Canceled, events[fillIndex + 1].Status);
        }

        [Test]
        public void StopLossFillsFirstWhenBothCouldFill()
        {
            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            Step(1.10m);
            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, tickets[0].Status);
            Assert.IsFalse(tickets[1].Contingency.IsWaitingForTrigger);

            // a wide bar goes through both the take profit and the stop loss: we can't know which one happened first, we are pessimistic
            _time = _time.AddMinutes(1);
            _algorithm.SetDateTime(_time);
            var bar = new Bar(1.10m, 1.15m, 1.02m, 1.10m);
            _security.SetMarketPrice(new QuoteBar(_time.AddMinutes(-1), _symbol, bar, 0, bar, 0, TimeSpan.FromMinutes(1)));
            _transactionHandler.ProcessSynchronousEvents();

            Assert.AreEqual(OrderStatus.Canceled, tickets[1].Status);
            Assert.AreEqual(OrderStatus.Filled, tickets[2].Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
        }

        [Test]
        public void TriggeredOrdersRequireNewDataToFill()
        {
            // the take profit is marketable: it would fill right away if it was working
            var entry = _algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Bracket(takeProfitPrice: 1.01m, stopLossPrice: 1m);
            var tickets = _algorithm.Order(entry);
            var takeProfit = tickets[1];

            Step(1.10m);
            Step(1.10m);
            Assert.AreEqual(OrderStatus.Submitted, takeProfit.Status);

            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, tickets[0].Status);
            Assert.AreEqual(OrderStatus.Submitted, takeProfit.Status);

            // scanning again with the same data does not fill it
            _transactionHandler.ProcessSynchronousEvents();
            Assert.AreEqual(OrderStatus.Submitted, takeProfit.Status);

            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, takeProfit.Status);
            Assert.AreEqual(OrderStatus.Canceled, tickets[2].Status);
        }

        [Test]
        public void TriggeredMarketOrdersFillRightAway()
        {
            var child = _algorithm.OrderFactory.MarketOrder(_symbol, -1000);
            var parent = _algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Triggers(child);
            var tickets = _algorithm.Order(parent);
            Assert.IsTrue(parent.OrderId > 0, tickets[0].SubmitRequest.Response.ToString());

            Step(1.10m);
            Assert.AreEqual(OrderStatus.Submitted, Ticket(child).Status);

            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, Ticket(parent).Status);
            Assert.AreEqual(OrderStatus.Filled, Ticket(child).Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
        }

        [Test]
        public void CancelingTheParentCancelsItsDescendants()
        {
            var grandChild = _algorithm.OrderFactory.MarketOrder(_symbol, 1000);
            var child = _algorithm.OrderFactory.LimitOrder(_symbol, -1000, 1.2m).Triggers(grandChild);
            var parent = _algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Triggers(child);
            var tickets = _algorithm.Order(parent);
            Step(1.10m);

            Assert.IsTrue(Ticket(parent).Cancel().IsSuccess);
            Step(1.10m);

            Assert.IsTrue(tickets.All(x => x.Status == OrderStatus.Canceled));
            StringAssert.Contains($"Contingent parent order {Ticket(parent).OrderId} was canceled", Ticket(child).OrderEvents.Last().Message);
            StringAssert.Contains($"Contingent parent order {Ticket(child).OrderId} was canceled", Ticket(grandChild).OrderEvents.Last().Message);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());

            // nothing fills anymore
            Step(1.05m);
            Assert.AreEqual(0, _security.Holdings.Quantity);
        }

        [Test]
        public void CancelingASiblingCancelsTheOther()
        {
            // the contingency is canceled as a whole, like brokerages do
            var tickets = _algorithm.OneCancelsOtherOrder(new List<SubmitOrderRequest> { _algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.05m), _algorithm.OrderFactory.LimitOrder(_symbol, 2000, 1.04m) });
            Step(1.10m);

            Assert.IsTrue(tickets[0].Cancel().IsSuccess);
            Step(1.10m);
            Assert.AreEqual(OrderStatus.Canceled, tickets[0].Status);
            Assert.AreEqual(OrderStatus.Canceled, tickets[1].Status);
            StringAssert.Contains($"Contingent sibling order {tickets[0].OrderId} was canceled", tickets[1].OrderEvents.Last().Message);

            Step(1.03m);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());
        }

        [Test]
        public void HeldOrdersCanBeUpdated()
        {
            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            Step(1.10m);

            Assert.IsTrue(tickets[1].UpdateLimitPrice(1.2m).IsSuccess);
            Assert.IsTrue(tickets[2].UpdateStopPrice(1.0m).IsSuccess);
            Step(1.10m);

            Assert.AreEqual(1.2m, tickets[1].Get(OrderField.LimitPrice));
            Assert.AreEqual(1.0m, tickets[2].Get(OrderField.StopPrice));
            Assert.IsTrue(tickets.Skip(1).All(x => x.Status == OrderStatus.UpdateSubmitted && x.Contingency.IsWaitingForTrigger));

            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, tickets[0].Status);
            // the original prices would of filled, not the updated ones
            Step(1.13m);
            Step(1.04m);
            Assert.IsTrue(tickets.Skip(1).All(x => x.Status == OrderStatus.UpdateSubmitted && !x.Contingency.IsWaitingForTrigger));
        }

        [Test]
        public void HeldTrailingStopStartsTrailingOnceTriggered()
        {
            var trailingStop = _algorithm.OrderFactory.TrailingStopOrder(_symbol, -1000, 0.01m, trailingAsPercentage: false);
            _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Triggers(trailingStop));
            Step(1.10m);
            Assert.AreEqual(0, Ticket(trailingStop).Get(OrderField.StopPrice));

            Step(1.08m);
            Assert.IsFalse(Ticket(trailingStop).Contingency.IsWaitingForTrigger);
            Assert.AreEqual(1.07m, Ticket(trailingStop).Get(OrderField.StopPrice));

            // trails the price up
            Step(1.15m);
            Assert.AreEqual(1.14m, Ticket(trailingStop).Get(OrderField.StopPrice));
            Assert.AreEqual(OrderStatus.Submitted, Ticket(trailingStop).Status);

            Step(1.13m);
            Assert.AreEqual(OrderStatus.Filled, Ticket(trailingStop).Status);
        }

        [Test]
        public void InsufficientBuyingPowerInvalidatesAllOrders()
        {
            var tickets = _algorithm.BracketOrder(_symbol, 1000000000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            Step(1.10m);

            Assert.IsTrue(tickets.All(x => x.Status == OrderStatus.Invalid));
            Assert.IsTrue(tickets.All(x => x.OrderEvents.Last().Message.Contains("Insufficient buying power", StringComparison.InvariantCulture)));
        }

        [Test]
        public void HeldOrdersDoNotRequireBuyingPowerUntilTriggered()
        {
            // the children are huge, but they are not validated until triggered: by the brokerage when filling
            var child = _algorithm.OrderFactory.LimitOrder(_symbol, 1000000000, 1.2m);
            var parent = _algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Triggers(child);
            _algorithm.Order(parent);
            Step(1.10m);
            Assert.AreEqual(OrderStatus.Submitted, Ticket(parent).Status);
            Assert.AreEqual(OrderStatus.Submitted, Ticket(child).Status);

            Step(1.08m);
            Assert.AreEqual(OrderStatus.Filled, Ticket(parent).Status);
            Step(1.08m);
            Assert.AreEqual(OrderStatus.Invalid, Ticket(child).Status);
        }

        [Test]
        public void BrokerageModelRejectionInvalidatesAllOrders()
        {
            _algorithm.SetBrokerageModel(new RejectStopOrdersBrokerageModel());

            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            Step(1.10m);

            Assert.IsTrue(tickets.All(x => x.Status == OrderStatus.Invalid));
            Assert.IsTrue(tickets.All(x => x.OrderEvents.Last().Message.Contains("BrokerageModel declared unable to submit order", StringComparison.InvariantCulture)));
        }

        [Test]
        public void OpenOrdersRemainingQuantityCountsTheLargestSibling()
        {
            var brokerage = UseContingentTestBrokerage();
            var tickets = _algorithm.OneCancelsOtherOrder(new[]
            {
                _algorithm.OrderFactory.LimitOrder(_symbol, -1000, 1.12m),
                _algorithm.OrderFactory.StopMarketOrder(_symbol, -2000, 1.05m)
            });
            _transactionHandler.ProcessSynchronousEvents();

            // at most one of them fills: the largest, not the first nor the sum
            Assert.AreEqual(-2000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));

            // what competes is the remaining quantity
            PublishFill(brokerage, tickets[1], -1500, OrderStatus.PartiallyFilled);
            Assert.AreEqual(-1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));

            // a filter can leave a sibling out
            Assert.AreEqual(-500, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(ticket => ticket.OrderId == tickets[1].OrderId));
        }

        [Test]
        public void OpenOrdersRemainingQuantityAddsPlainOrdersAndEachSet()
        {
            UseContingentTestBrokerage();
            _algorithm.LimitOrder(_symbol, -300, 1.12m);
            _algorithm.OneCancelsOtherOrder(new[]
            {
                _algorithm.OrderFactory.LimitOrder(_symbol, -1000, 1.12m),
                _algorithm.OrderFactory.StopMarketOrder(_symbol, -2000, 1.05m)
            });
            _algorithm.OneCancelsOtherOrder(new[]
            {
                _algorithm.OrderFactory.LimitOrder(_symbol, -500, 1.13m),
                _algorithm.OrderFactory.StopMarketOrder(_symbol, -700, 1.04m)
            });
            _transactionHandler.ProcessSynchronousEvents();

            // the plain order in full, plus the largest sibling of each set
            Assert.AreEqual(-300 - 2000 - 700, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));
        }

        [Test]
        public void OpenOrdersRemainingQuantityOfSiblingsForDifferentSymbols()
        {
            UseContingentTestBrokerage();
            var other = _algorithm.AddSecurity(SecurityType.Forex, "GBPUSD");
            _algorithm.Portfolio.CashBook["GBP"].ConversionRate = 1.3m;
            other.SetMarketPrice(new Tick(_time, other.Symbol, 1.30m, 1.30m, 1.30m));

            _algorithm.OneCancelsOtherOrder(new[]
            {
                _algorithm.OrderFactory.LimitOrder(_symbol, -1000, 1.12m),
                _algorithm.OrderFactory.LimitOrder(other.Symbol, -2000, 1.32m)
            });
            _transactionHandler.ProcessSynchronousEvents();

            // each symbol counts its own member
            Assert.AreEqual(-1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));
            Assert.AreEqual(-2000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(other.Symbol));
            Assert.AreEqual(-3000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity());
        }

        [Test]
        public void OpenOrdersRemainingQuantityCountsChildrenOnceTriggered()
        {
            var brokerage = UseContingentTestBrokerage();
            var tickets = _algorithm.OneTriggersOtherOrder(_algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m),
                new[] { _algorithm.OrderFactory.LimitOrder(_symbol, -1000, 1.12m) });
            _transactionHandler.ProcessSynchronousEvents();

            // the child is held, it's not working
            Assert.AreEqual(1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));

            PublishFill(brokerage, tickets[0], 1000, OrderStatus.Filled);
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[1].OrderId, ContingencyTriggered = true });
            Assert.AreEqual(-1000, _algorithm.Transactions.GetOpenOrdersRemainingQuantity(_symbol));
        }

        [TestCase(OrderStatus.PartiallyFilled)]
        [TestCase(OrderStatus.Filled)]
        public void FillOfAHeldOrderMarksItTriggered(OrderStatus status)
        {
            TearDown();
            var brokerage = new ContingentTestBrokerage(_algorithm);
            Initialize(null, brokerage);

            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            _transactionHandler.ProcessSynchronousEvents();
            Assert.IsTrue(tickets[1].Contingency.IsWaitingForTrigger);

            // the brokerage fills the exit without notifying it was triggered first
            var order = brokerage.PlacedOrders.Single(x => x.Id == tickets[1].OrderId);
            brokerage.PublishOrderEvent(new OrderEvent(order, _algorithm.UtcTime, OrderFee.Zero)
            {
                Status = status,
                FillQuantity = status == OrderStatus.Filled ? order.Quantity : order.Quantity / 2,
                FillPrice = 1.12m
            });

            Assert.IsFalse(tickets[1].Contingency.IsWaitingForTrigger);
            Assert.AreEqual(_algorithm.UtcTime, tickets[1].Contingency.Links.Single(x => x.Role == ContingencyRole.Child).TriggeredTime);
            // its sibling was not filled, it's still held
            Assert.IsTrue(tickets[2].Contingency.IsWaitingForTrigger);
        }

        [Test]
        public void BrokerageOrderUpdatesAreApplied()
        {
            TearDown();
            var brokerage = new ContingentTestBrokerage(_algorithm);
            Initialize(null, brokerage);

            var tickets = _algorithm.BracketOrder(_symbol, 1000, takeProfitPrice: 1.12m, stopLossPrice: 1.05m, limitPrice: 1.09m);
            _transactionHandler.ProcessSynchronousEvents();
            Assert.AreEqual(3, brokerage.PlacedOrders.Count);
            Assert.IsTrue(tickets[1].Contingency.IsWaitingForTrigger);

            // the brokerage triggers the order and resizes it
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[1].OrderId, ContingencyTriggered = true });
            Assert.IsFalse(tickets[1].Contingency.IsWaitingForTrigger);
            // the algorithm time at which it was triggered
            Assert.AreEqual(_algorithm.UtcTime, tickets[1].Contingency.Links.Single(x => x.Role == ContingencyRole.Child).TriggeredTime);
            Assert.IsTrue(tickets[2].Contingency.IsWaitingForTrigger);
            Assert.AreEqual(1.12m, tickets[1].Get(OrderField.LimitPrice));

            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[1].OrderId, Quantity = -400 });
            Assert.AreEqual(-400, tickets[1].Quantity);

            // invalid quantities are ignored: different side and zero
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[1].OrderId, Quantity = 400 });
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[1].OrderId, Quantity = 0 });
            Assert.AreEqual(-400, tickets[1].Quantity);

            // an order which isn't a contingent child ignores the trigger
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = tickets[0].OrderId, ContingencyTriggered = true });
            Assert.IsFalse(tickets[0].Contingency.IsWaitingForTrigger);
        }

        [Test]
        public void ContingencyOrderUpdateDoesNotResetStopLimitTriggerNorTrailingStopPrice()
        {
            TearDown();
            var brokerage = new ContingentTestBrokerage(_algorithm);
            Initialize(null, brokerage);

            var stopLimit = _algorithm.OrderFactory.StopLimitOrder(_symbol, -1000, 1.05m, 1.04m);
            var trailingStop = _algorithm.OrderFactory.TrailingStopOrder(_symbol, -1000, 1.06m, 0.01m, false);
            _algorithm.Order(_algorithm.OrderFactory.LimitOrder(_symbol, 1000, 1.09m).Triggers(stopLimit, trailingStop));
            _transactionHandler.ProcessSynchronousEvents();

            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = Ticket(stopLimit).OrderId, StopTriggered = true });
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = Ticket(stopLimit).OrderId, ContingencyTriggered = true });
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = Ticket(trailingStop).OrderId, Quantity = -500 });

            var order = (StopLimitOrder)_algorithm.Transactions.GetOrderById(Ticket(stopLimit).OrderId);
            Assert.IsTrue(order.StopTriggered);
            Assert.IsFalse(order.IsWaitingForTrigger());
            Assert.AreEqual(1.06m, Ticket(trailingStop).Get(OrderField.StopPrice));
            Assert.AreEqual(-500, Ticket(trailingStop).Quantity);

            // the trigger can carry the trailing stop price
            brokerage.PublishOrderUpdate(new OrderUpdateEvent { OrderId = Ticket(trailingStop).OrderId, ContingencyTriggered = true, TrailingStopPrice = 1.07m });
            Assert.AreEqual(1.07m, Ticket(trailingStop).Get(OrderField.StopPrice));
        }

        [Test]
        public void OpenOrdersFromTheBrokerageKeepTheirContingencies()
        {
            // like on a live deployment restart, the brokerage provides the existing open orders
            var bracket = QuantConnect.Tests.Common.Orders.ContingentOrderTests.CreateBracket();
            var set = new OrderContingency(3, []);
            var orders = new List<Order>
            {
                new LimitOrder(_symbol, -1000, 1.12m, _time) { Contingency = set.WithLinks(bracket[1].Contingency.Links) },
                new StopMarketOrder(_symbol, -1000, 1.05m, _time) { Contingency = set.WithLinks(bracket[2].Contingency.Links) }
            };

            foreach (var order in orders)
            {
                _transactionHandler.AddOpenOrder(order, _algorithm);
            }

            // the shared set gets a new id, once, and the new lean order ids
            Assert.AreEqual(1, set.Id);
            CollectionAssert.AreEquivalent(orders.Select(x => x.Id), set.OrderIds);
            var tickets = _algorithm.Transactions.GetOpenOrderTickets().ToList();
            Assert.AreEqual(2, tickets.Count);
            Assert.IsTrue(tickets.All(x => x.Contingency.Id == 1 && x.Contingency.OrderIds == set.OrderIds && x.Contingency.Links.Count == 2 && x.Contingency.IsWaitingForTrigger));
        }

        private OrderTicket Ticket(SubmitOrderRequest request)
        {
            return _algorithm.Transactions.GetOrderTicket(request.OrderId);
        }

        private void Initialize(BacktestingBrokerage backtestingBrokerage, IBrokerage brokerage = null)
        {
            _brokerage = backtestingBrokerage;
            _transactionHandler = brokerage == null ? new BacktestingTransactionHandler() : new SynchronousTransactionHandler();
            _transactionHandler.Initialize(_algorithm, brokerage ?? backtestingBrokerage, new BacktestingResultHandler());
            _algorithm.Transactions.SetOrderProcessor(_transactionHandler);
        }

        /// <summary>
        /// A brokerage which accepts the orders without filling them, the fills are published by the test
        /// </summary>
        private ContingentTestBrokerage UseContingentTestBrokerage()
        {
            TearDown();
            var brokerage = new ContingentTestBrokerage(_algorithm);
            Initialize(null, brokerage);
            return brokerage;
        }

        private void PublishFill(ContingentTestBrokerage brokerage, OrderTicket ticket, decimal fillQuantity, OrderStatus status)
        {
            var order = brokerage.PlacedOrders.Single(x => x.Id == ticket.OrderId);
            brokerage.PublishOrderEvent(new OrderEvent(order, _algorithm.UtcTime, OrderFee.Zero)
            {
                Status = status,
                FillQuantity = fillQuantity,
                FillPrice = _security.Price
            });
        }

        private void Step(decimal price)
        {
            _time = _time.AddMinutes(1);
            _algorithm.SetDateTime(_time);
            SetPrice(price);
            _transactionHandler.ProcessSynchronousEvents();
        }

        private void SetPrice(decimal price)
        {
            _security.SetMarketPrice(new Tick(_time, _symbol, price, price, price));
        }

        /// <summary>
        /// Allows using a brokerage different than the backtesting one, processing the order requests synchronously
        /// </summary>
        private class SynchronousTransactionHandler : BrokerageTransactionHandler
        {
            protected override bool SynchronousProcessing => true;

            protected override void WaitForOrderSubmission(OrderTicket ticket)
            {
                ProcessPendingRequests();
            }

            public override void ProcessSynchronousEvents()
            {
                ProcessPendingRequests();
            }
        }

        private class RejectStopOrdersBrokerageModel : DefaultBrokerageModel
        {
            public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
            {
                message = null;
                return order.Type != OrderType.StopMarket;
            }
        }

        private class ContingentTestBrokerage : BrokerageTransactionHandlerTests.NoSubmitTestBrokerage
        {
            public List<Order> PlacedOrders { get; } = new();
            public ContingentTestBrokerage(IAlgorithm algorithm) : base(algorithm)
            {
            }
            public override bool PlaceOrder(Order order)
            {
                PlacedOrders.Add(order);
                return true;
            }
            public void PublishOrderUpdate(OrderUpdateEvent orderUpdateEvent)
            {
                OnOrderUpdated(orderUpdateEvent);
            }
            public void PublishOrderEvent(OrderEvent orderEvent)
            {
                OnOrderEvent(orderEvent);
            }
        }
    }
}
