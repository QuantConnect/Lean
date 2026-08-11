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
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    /// <summary>
    /// Tests the engine-guaranteed OCO semantics of <see cref="Algorithm.QCAlgorithm.BracketOrder(Symbol, decimal, decimal?, decimal?, decimal?, string, Interfaces.IOrderProperties)"/>:
    /// the entry fill places the exit legs, a leg fill cancels its sibling (also when a single bar spans
    /// both legs), an unrelated order closing the position cancels the remaining legs and a new bracket
    /// is refused while one is still active
    /// </summary>
    [TestFixture]
    public class BracketOrderTests
    {
        // Monday 2013-10-07 10:30 New York, regular equity market hours
        private static readonly DateTime ReferenceUtc = new DateTime(2013, 10, 07, 14, 30, 0);

        private BrokerageTransactionHandlerTests.TestAlgorithm _algorithm;
        private BacktestingTransactionHandler _transactionHandler;
        private BacktestingBrokerage _brokerage;
        private Security _security;
        private Symbol _spy;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new BrokerageTransactionHandlerTests.TestAlgorithm
            {
                HistoryProvider = new BrokerageTransactionHandlerTests.EmptyHistoryProvider()
            };
            _algorithm.SetCash(100000);
            _security = _algorithm.AddEquity("SPY");
            _spy = _security.Symbol;
            _algorithm.SetDateTime(ReferenceUtc);
            SetPrice(100m, 100m, 100m);

            _transactionHandler = new BacktestingTransactionHandler();
            _brokerage = new BacktestingBrokerage(_algorithm);
            _transactionHandler.Initialize(_algorithm, _brokerage, new BacktestingResultHandler());
            _algorithm.Transactions.SetOrderProcessor(_transactionHandler);
            // as in backtesting deployments: fills are synchronous, MarketOrder must not wait
            _algorithm.Transactions.MarketOrderFillTimeout = TimeSpan.Zero;
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler.Exit();
            _brokerage.Dispose();
        }

        [Test]
        public void EntryFillPlacesExitLegsSizedToFilledQuantity()
        {
            // the market entry fills synchronously in backtesting, so the returned bracket already
            // carries the exit legs
            var bracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m);

            Assert.IsTrue(bracket.IsActive);
            Assert.AreEqual(bracket, _algorithm.Transactions.GetBracketOrderTicket(_spy));
            Assert.AreEqual(OrderStatus.Filled, bracket.EntryTicket.Status);
            Assert.AreEqual(10, _security.Holdings.Quantity);

            Assert.IsNotNull(bracket.StopLossTicket);
            Assert.AreEqual(OrderType.StopMarket, bracket.StopLossTicket.OrderType);
            Assert.AreEqual(-10, bracket.StopLossTicket.Quantity);
            Assert.AreEqual(90m, bracket.StopLossTicket.Get(OrderField.StopPrice));

            Assert.IsNotNull(bracket.TakeProfitTicket);
            Assert.AreEqual(OrderType.Limit, bracket.TakeProfitTicket.OrderType);
            Assert.AreEqual(-10, bracket.TakeProfitTicket.Quantity);
            Assert.AreEqual(110m, bracket.TakeProfitTicket.Get(OrderField.LimitPrice));

            // the stop loss is submitted first so it wins deterministically on a bar spanning both legs
            Assert.Less(bracket.StopLossTicket.OrderId, bracket.TakeProfitTicket.OrderId);
            Assert.IsTrue(bracket.IsActive);
        }

        [Test]
        public void TakeProfitFillCancelsStopLoss()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            Step(open: 111m, high: 112m, low: 111m);

            Assert.AreEqual(OrderStatus.Filled, bracket.TakeProfitTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.StopLossTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
            Assert.IsNull(_algorithm.Transactions.GetBracketOrderTicket(_spy));
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());
        }

        [Test]
        public void StopLossFillCancelsTakeProfit()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            Step(open: 89m, high: 89m, low: 88m);

            Assert.AreEqual(OrderStatus.Filled, bracket.StopLossTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.TakeProfitTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());
        }

        [Test]
        public void BarSpanningBothLegsFillsOnlyTheStopLoss()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            // a single wide bar crosses both the stop loss and the take profit: without the engine
            // canceling the sibling in the same scan both legs would fill, flipping the position short
            Step(open: 100m, high: 115m, low: 85m);

            Assert.AreEqual(OrderStatus.Filled, bracket.StopLossTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.TakeProfitTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
        }

        [Test]
        public void ManualPositionCloseCancelsBothLegs()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            _algorithm.MarketOrder(_spy, -10);
            Step();

            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.AreEqual(OrderStatus.Canceled, bracket.StopLossTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.TakeProfitTicket.Status);
            Assert.IsFalse(bracket.IsActive);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());
        }

        [Test]
        public void PartialPositionReductionDownsizesTheLegs()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            _algorithm.MarketOrder(_spy, -4);
            Step();

            Assert.AreEqual(6, _security.Holdings.Quantity);
            Assert.AreEqual(-6, bracket.StopLossTicket.Quantity);
            Assert.AreEqual(-6, bracket.TakeProfitTicket.Quantity);
            Assert.IsTrue(bracket.IsActive);

            // the downsized stop loss closes the remaining position exactly, without flipping it
            Step(open: 89m, high: 89m, low: 88m);
            Assert.AreEqual(OrderStatus.Filled, bracket.StopLossTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
        }

        [Test]
        public void RefusesNewBracketWhileOneIsActive()
        {
            // refused while the entry is still working (limit entry far from the market stays open)
            var pendingBracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 70m, takeProfitPrice: 110m, entryLimitPrice: 80m);
            Step();
            Assert.AreEqual(OrderStatus.Submitted, pendingBracket.EntryTicket.Status);
            Assert.Throws<InvalidOperationException>(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 91m, takeProfitPrice: 111m));

            pendingBracket.Cancel();
            Step();
            Assert.IsFalse(pendingBracket.IsActive);

            // refused while the exit legs are live
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);
            Assert.Throws<InvalidOperationException>(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 91m, takeProfitPrice: 111m));

            // allowed again once the bracket completes
            Step(open: 111m, high: 112m, low: 111m);
            Assert.IsFalse(bracket.IsActive);
            var newBracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 91m, takeProfitPrice: 111m);
            Assert.AreNotEqual(bracket, newBracket);
            Assert.AreEqual(newBracket, _algorithm.Transactions.GetBracketOrderTicket(_spy));
        }

        [Test]
        public void CancelBeforeEntryFillCancelsTheEntryAndCompletes()
        {
            // entry limit far below the market so it does not fill
            var bracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 70m, takeProfitPrice: 110m, entryLimitPrice: 80m);
            Step();
            Assert.AreEqual(OrderStatus.Submitted, bracket.EntryTicket.Status);

            bracket.Cancel();
            Step();

            Assert.AreEqual(OrderStatus.Canceled, bracket.EntryTicket.Status);
            Assert.IsNull(bracket.StopLossTicket);
            Assert.IsNull(bracket.TakeProfitTicket);
            Assert.IsFalse(bracket.IsActive);
            Assert.IsEmpty(_algorithm.Transactions.GetOpenOrders());

            // a new bracket can be placed right away
            Assert.DoesNotThrow(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m));
        }

        [Test]
        public void CancelAfterLegsArePlacedCancelsBothLegs()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: 110m);

            bracket.Cancel();
            Step();

            Assert.AreEqual(OrderStatus.Canceled, bracket.StopLossTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.TakeProfitTicket.Status);
            Assert.IsFalse(bracket.IsActive);
            // the position is left as is, canceling the bracket only cancels its orders
            Assert.AreEqual(10, _security.Holdings.Quantity);
        }

        [Test]
        public void EntryCanceledExternallyBeforeFillCompletesTheBracket()
        {
            var bracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 70m, takeProfitPrice: 110m, entryLimitPrice: 80m);
            Step();

            bracket.EntryTicket.Cancel();
            Step();

            Assert.AreEqual(OrderStatus.Canceled, bracket.EntryTicket.Status);
            Assert.IsFalse(bracket.IsActive);
            Assert.IsNull(_algorithm.Transactions.GetBracketOrderTicket(_spy));
        }

        [Test]
        public void LimitEntryFillsAndPlacesLegs()
        {
            var bracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m, entryLimitPrice: 98m);
            Step();
            Assert.AreEqual(OrderStatus.Submitted, bracket.EntryTicket.Status);

            Step(open: 97.5m, high: 98m, low: 97m);

            Assert.AreEqual(OrderStatus.Filled, bracket.EntryTicket.Status);
            Assert.IsNotNull(bracket.StopLossTicket);
            Assert.IsNotNull(bracket.TakeProfitTicket);
            Assert.AreEqual(-10, bracket.StopLossTicket.Quantity);
        }

        [Test]
        public void ShortBracketStopLossFillCancelsTakeProfit()
        {
            var bracket = FillEntry(-10, stopLossPrice: 110m, takeProfitPrice: 90m);
            Assert.AreEqual(-10, _security.Holdings.Quantity);
            Assert.AreEqual(10, bracket.StopLossTicket.Quantity);
            Assert.AreEqual(10, bracket.TakeProfitTicket.Quantity);

            Step(open: 111m, high: 112m, low: 111m);

            Assert.AreEqual(OrderStatus.Filled, bracket.StopLossTicket.Status);
            Assert.AreEqual(OrderStatus.Canceled, bracket.TakeProfitTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
        }

        [Test]
        public void StopLossOnlyBracketCompletesWhenTheLegFills()
        {
            var bracket = FillEntry(10, stopLossPrice: 90m, takeProfitPrice: null);
            Assert.IsNotNull(bracket.StopLossTicket);
            Assert.IsNull(bracket.TakeProfitTicket);

            Step(open: 89m, high: 89m, low: 88m);

            Assert.AreEqual(OrderStatus.Filled, bracket.StopLossTicket.Status);
            Assert.AreEqual(0, _security.Holdings.Quantity);
            Assert.IsFalse(bracket.IsActive);
        }

        [Test]
        public void MoveStopLossBeforeAndAfterLegPlacement()
        {
            var bracket = _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m);

            // before the legs are placed the pending price is updated in place
            var response = bracket.MoveStopLoss(92m);
            Assert.IsFalse(response.IsError);
            Assert.AreEqual(92m, bracket.StopLossPrice);

            Step();
            Assert.AreEqual(92m, bracket.StopLossTicket.Get(OrderField.StopPrice));

            // after the legs are placed an update request is submitted for the live order
            response = bracket.MoveStopLoss(94m);
            Assert.IsFalse(response.IsError);
            Step();
            Assert.AreEqual(94m, bracket.StopLossTicket.Get(OrderField.StopPrice));
            Assert.AreEqual(94m, bracket.StopLossPrice);
        }

        [Test]
        public void ValidatesExitPricesAgainstEachOtherAndTheEntry()
        {
            // at least one exit is required
            Assert.Throws<ArgumentException>(() => _algorithm.BracketOrder(_spy, 10));
            // long: stop loss must be below the take profit
            Assert.Throws<ArgumentException>(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 110m, takeProfitPrice: 90m));
            // short: stop loss must be above the take profit
            Assert.Throws<ArgumentException>(() => _algorithm.BracketOrder(_spy, -10, stopLossPrice: 90m, takeProfitPrice: 110m));
            // long: stop loss below the entry limit, take profit above it
            Assert.Throws<ArgumentException>(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 99m, takeProfitPrice: 110m, entryLimitPrice: 98m));
            Assert.Throws<ArgumentException>(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 97m, entryLimitPrice: 98m));

            // nothing was registered by the refused calls
            Assert.IsNull(_algorithm.Transactions.GetBracketOrderTicket(_spy));
            Assert.DoesNotThrow(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m));
        }

        [Test]
        public void InvalidEntryProducesAnInactiveBracket()
        {
            // zero quantity fails the pre order checks
            var bracket = _algorithm.BracketOrder(_spy, 0, stopLossPrice: 90m, takeProfitPrice: 110m);

            Assert.IsFalse(bracket.IsActive);
            Assert.AreEqual(OrderStatus.Invalid, bracket.EntryTicket.Status);
            Assert.IsNull(_algorithm.Transactions.GetBracketOrderTicket(_spy));

            // and does not block a subsequent bracket
            Assert.DoesNotThrow(() => _algorithm.BracketOrder(_spy, 10, stopLossPrice: 90m, takeProfitPrice: 110m));
        }

        /// <summary>
        /// Places a bracket order and processes events until the entry is filled and the exit legs are placed
        /// </summary>
        private BracketOrderTicket FillEntry(decimal quantity, decimal? stopLossPrice, decimal? takeProfitPrice)
        {
            var bracket = _algorithm.BracketOrder(_spy, quantity, stopLossPrice, takeProfitPrice);
            Step();
            Assert.AreEqual(OrderStatus.Filled, bracket.EntryTicket.Status);
            return bracket;
        }

        /// <summary>
        /// Advances the algorithm one minute, optionally publishing a new price bar, and processes the
        /// transaction handler's synchronous events (request draining plus the brokerage fill scan)
        /// </summary>
        private void Step(decimal? open = null, decimal? high = null, decimal? low = null)
        {
            _algorithm.SetDateTime(_algorithm.UtcTime.AddMinutes(1));
            if (open.HasValue)
            {
                SetPrice(open.Value, high ?? open.Value, low ?? open.Value);
            }
            else
            {
                var price = _security.Price == 0 ? 100m : _security.Price;
                SetPrice(price, price, price);
            }
            _transactionHandler.ProcessSynchronousEvents();
        }

        private void SetPrice(decimal open, decimal high, decimal low)
        {
            var close = (high + low) / 2;
            _security.SetMarketPrice(new TradeBar(_algorithm.Time.AddMinutes(-1), _spy, open, high, low, close, 100));
        }
    }
}
