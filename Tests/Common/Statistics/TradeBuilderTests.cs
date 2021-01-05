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
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class TradeBuilderTests
    {
        private readonly OrderFee _orderFee = new OrderFee(new CashAmount(1, Currencies.USD));
        private const decimal ConversionRate = 1;
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void AllInAllOutLong(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(1.08m, trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade.Direction);
            Assert.AreEqual(1000, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(1.09m, trade.ExitPrice);
            Assert.AreEqual(10, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-5, trade.MAE);
            Assert.AreEqual(20m, trade.MFE);
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void AllInAllOutShort(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(1.08m, trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Short, trade.Direction);
            Assert.AreEqual(1000, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(1.09m, trade.ExitPrice);
            Assert.AreEqual(-10, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-20, trade.MAE);
            Assert.AreEqual(5, trade.MFE);
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInAllOutLong(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 2k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            if (groupingMethod == FillGroupingMethod.FillToFill)
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 0 : 1];

                Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(1.08m, trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                Assert.AreEqual(1000, trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                Assert.AreEqual(1.09m, trade1.ExitPrice);
                Assert.AreEqual(10, trade1.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 2 : 1, trade1.TotalFees);
                Assert.AreEqual(-15, trade1.MAE);
                Assert.AreEqual(20, trade1.MFE);

                var trade2 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 1 : 0];

                Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                Assert.AreEqual(1.07m, trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                Assert.AreEqual(1000, trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(1.09m, trade2.ExitPrice);
                Assert.AreEqual(20, trade2.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1 : 2, trade2.TotalFees);
                Assert.AreEqual(-5, trade2.MAE);
                Assert.AreEqual(30, trade2.MFE);
            }
            else
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(1.075m, trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade.Direction);
                Assert.AreEqual(2000, trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(1.09m, trade.ExitPrice);
                Assert.AreEqual(30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-20, trade.MAE);
                Assert.AreEqual(50, trade.MFE);
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInAllOutShort(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 2k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            if (groupingMethod == FillGroupingMethod.FillToFill)
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 0 : 1];

                Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(1.08m, trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                Assert.AreEqual(1000, trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                Assert.AreEqual(1.09m, trade1.ExitPrice);
                Assert.AreEqual(-10, trade1.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 2 : 1, trade1.TotalFees);
                Assert.AreEqual(-20, trade1.MAE);
                Assert.AreEqual(15, trade1.MFE);

                var trade2 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 1 : 0];

                Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                Assert.AreEqual(1.07m, trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                Assert.AreEqual(1000, trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(1.09m, trade2.ExitPrice);
                Assert.AreEqual(-20, trade2.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1 : 2, trade2.TotalFees);
                Assert.AreEqual(-30, trade2.MAE);
                Assert.AreEqual(5, trade2.MFE);
            }
            else
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(1.075m, trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade.Direction);
                Assert.AreEqual(2000, trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(1.09m, trade.ExitPrice);
                Assert.AreEqual(-30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-50, trade.MAE);
                Assert.AreEqual(20, trade.MFE);
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void AllInScaleOutLong(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 2k, Sell 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 2k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            if (groupingMethod == FillGroupingMethod.FlatToFlat)
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(1.07m, trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade.Direction);
                Assert.AreEqual(2000, trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(1.085m, trade.ExitPrice);
                Assert.AreEqual(30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-10, trade.MAE);
                Assert.AreEqual(60, trade.MFE);
            }
            else
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(1.07m, trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                Assert.AreEqual(1000, trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(10), trade1.ExitTime);
                Assert.AreEqual(1.08m, trade1.ExitPrice);
                Assert.AreEqual(10, trade1.ProfitLoss);
                Assert.AreEqual(2, trade1.TotalFees);
                Assert.AreEqual(0, trade1.MAE);
                Assert.AreEqual(10, trade1.MFE);

                var trade2 = builder.ClosedTrades[1];

                Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                Assert.AreEqual(time, trade2.EntryTime);
                Assert.AreEqual(1.07m, trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                Assert.AreEqual(1000, trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(1.09m, trade2.ExitPrice);
                Assert.AreEqual(20, trade2.ProfitLoss);
                Assert.AreEqual(1, trade2.TotalFees);
                Assert.AreEqual(-5, trade2.MAE);
                Assert.AreEqual(30, trade2.MFE);
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void AllInScaleOutShort(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 2k, Buy 1k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 2k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            if (groupingMethod == FillGroupingMethod.FlatToFlat)
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(1.07m, trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade.Direction);
                Assert.AreEqual(2000, trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(1.085, trade.ExitPrice);
                Assert.AreEqual(-30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-60, trade.MAE);
                Assert.AreEqual(10, trade.MFE);
            }
            else
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(1.07m, trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                Assert.AreEqual(1000, trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(10), trade1.ExitTime);
                Assert.AreEqual(1.08m, trade1.ExitPrice);
                Assert.AreEqual(-10, trade1.ProfitLoss);
                Assert.AreEqual(2, trade1.TotalFees);
                Assert.AreEqual(-10, trade1.MAE);
                Assert.AreEqual(0, trade1.MFE);

                var trade2 = builder.ClosedTrades[1];

                Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                Assert.AreEqual(time, trade2.EntryTime);
                Assert.AreEqual(1.07m, trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                Assert.AreEqual(1000, trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(1.09m, trade2.ExitPrice);
                Assert.AreEqual(-20, trade2.ProfitLoss);
                Assert.AreEqual(1, trade2.TotalFees);
                Assert.AreEqual(-30, trade2.MAE);
                Assert.AreEqual(5, trade2.MFE);
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ReversalLongToShort(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Sell 2k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 2k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            Assert.AreEqual(2, builder.ClosedTrades.Count);

            var trade1 = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
            Assert.AreEqual(time, trade1.EntryTime);
            Assert.AreEqual(1.07m, trade1.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade1.Direction);
            Assert.AreEqual(1000, trade1.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade1.ExitTime);
            Assert.AreEqual(1.08m, trade1.ExitPrice);
            Assert.AreEqual(10, trade1.ProfitLoss);
            Assert.AreEqual(2, trade1.TotalFees);
            Assert.AreEqual(0, trade1.MAE);
            Assert.AreEqual(10, trade1.MFE);

            var trade2 = builder.ClosedTrades[1];

            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
            Assert.AreEqual(1.08m, trade2.EntryPrice);
            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
            Assert.AreEqual(1000, trade2.Quantity);
            Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
            Assert.AreEqual(1.09m, trade2.ExitPrice);
            Assert.AreEqual(-10, trade2.ProfitLoss);
            Assert.AreEqual(1, trade2.TotalFees);
            Assert.AreEqual(-20, trade2.MAE);
            Assert.AreEqual(15, trade2.MFE);
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ReversalShortToLong(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Buy 2k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 2k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            Assert.AreEqual(2, builder.ClosedTrades.Count);

            var trade1 = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
            Assert.AreEqual(time, trade1.EntryTime);
            Assert.AreEqual(1.07m, trade1.EntryPrice);
            Assert.AreEqual(TradeDirection.Short, trade1.Direction);
            Assert.AreEqual(1000, trade1.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade1.ExitTime);
            Assert.AreEqual(1.08m, trade1.ExitPrice);
            Assert.AreEqual(-10, trade1.ProfitLoss);
            Assert.AreEqual(2, trade1.TotalFees);
            Assert.AreEqual(-10, trade1.MAE);
            Assert.AreEqual(0, trade1.MFE);

            var trade2 = builder.ClosedTrades[1];

            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
            Assert.AreEqual(1.08m, trade2.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
            Assert.AreEqual(1000, trade2.Quantity);
            Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
            Assert.AreEqual(1.09m, trade2.ExitPrice);
            Assert.AreEqual(10, trade2.ProfitLoss);
            Assert.AreEqual(1, trade2.TotalFees);
            Assert.AreEqual(-15, trade2.MAE);
            Assert.AreEqual(20, trade2.MFE);
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut1Long(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Buy 1k, Sell 1k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 1k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 2k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time.AddMinutes(30), trade2.EntryTime);
                        Assert.AreEqual(1.08m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(1000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(10, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(-15, trade2.MAE);
                        Assert.AreEqual(20, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(30) : time, trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(1000, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                        Assert.AreEqual(1.09m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 20, trade3.ProfitLoss);
                        Assert.AreEqual(1, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -15 : -5, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 30, trade3.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.0766666666666666666666666667m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(3000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(1.09m, trade.ExitPrice);
                        Assert.AreEqual(40, trade.ProfitLoss);
                        Assert.AreEqual(5, trade.TotalFees);
                        Assert.AreEqual(-35, trade.MAE);
                        Assert.AreEqual(70, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.075m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(2000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 50, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut1Short(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Sell 1k, Buy 1k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 1k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 2k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time.AddMinutes(30), trade2.EntryTime);
                        Assert.AreEqual(1.08m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(1000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(-10, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(-20, trade2.MAE);
                        Assert.AreEqual(15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(30) : time, trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(1000, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                        Assert.AreEqual(1.09m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -20, trade3.ProfitLoss);
                        Assert.AreEqual(1, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -30, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 15 : 5, trade3.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.0766666666666666666666666667m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(3000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(1.09m, trade.ExitPrice);
                        Assert.AreEqual(-40, trade.ProfitLoss);
                        Assert.AreEqual(5, trade.TotalFees);
                        Assert.AreEqual(-70, trade.MAE);
                        Assert.AreEqual(35, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.075m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(2000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -50, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut2Long(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Buy 2k, Sell 1k, Buy 1k, Sell 3k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 2k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 1k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 3k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -3000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(3, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                            Assert.AreEqual(time, trade1.EntryTime);
                            Assert.AreEqual(1.07m, trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                            Assert.AreEqual(1000, trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(1.09m, trade1.ExitPrice);
                            Assert.AreEqual(20, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-5, trade1.MAE);
                            Assert.AreEqual(30, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                            Assert.AreEqual(1.08m, trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                            Assert.AreEqual(2000, trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(1.09m, trade2.ExitPrice);
                            Assert.AreEqual(20, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-30, trade2.MAE);
                            Assert.AreEqual(40, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade3.EntryTime);
                            Assert.AreEqual(1.08m, trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                            Assert.AreEqual(1000, trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(1.09m, trade3.ExitPrice);
                            Assert.AreEqual(10, trade3.ProfitLoss);
                            Assert.AreEqual(1, trade3.TotalFees);
                            Assert.AreEqual(-15, trade3.MAE);
                            Assert.AreEqual(20, trade3.MFE);
                        }
                        else
                        {
                            Assert.AreEqual(4, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade1.EntryTime);
                            Assert.AreEqual(1.08m, trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                            Assert.AreEqual(1000, trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(1.09m, trade1.ExitPrice);
                            Assert.AreEqual(10, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-15, trade1.MAE);
                            Assert.AreEqual(20, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade2.EntryTime);
                            Assert.AreEqual(1.08m, trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                            Assert.AreEqual(1000, trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(1.09m, trade2.ExitPrice);
                            Assert.AreEqual(10, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-15, trade2.MAE);
                            Assert.AreEqual(20, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade3.EntryTime);
                            Assert.AreEqual(1.08m, trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                            Assert.AreEqual(1000, trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(1.09m, trade3.ExitPrice);
                            Assert.AreEqual(10, trade3.ProfitLoss);
                            Assert.AreEqual(0, trade3.TotalFees);
                            Assert.AreEqual(-15, trade3.MAE);
                            Assert.AreEqual(20, trade3.MFE);

                            var trade4 = builder.ClosedTrades[3];

                            Assert.AreEqual(Symbols.EURUSD, trade4.Symbol);
                            Assert.AreEqual(time, trade4.EntryTime);
                            Assert.AreEqual(1.07m, trade4.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade4.Direction);
                            Assert.AreEqual(1000, trade4.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade4.ExitTime);
                            Assert.AreEqual(1.09m, trade4.ExitPrice);
                            Assert.AreEqual(20, trade4.ProfitLoss);
                            Assert.AreEqual(1, trade4.TotalFees);
                            Assert.AreEqual(-5, trade4.MAE);
                            Assert.AreEqual(30, trade4.MFE);
                        }
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.0775m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(4000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(1.09m, trade.ExitPrice);
                        Assert.AreEqual(50, trade.ProfitLoss);
                        Assert.AreEqual(5, trade.TotalFees);
                        Assert.AreEqual(-50, trade.MAE);
                        Assert.AreEqual(90, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.0766666666666666666666666667m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(3000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 40, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -45 : -35, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 60 : 70, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut2Short(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Sell 2k, Buy 1k, Sell 1k, Buy 3k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 2k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 1k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 3k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 3000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(3, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                            Assert.AreEqual(time, trade1.EntryTime);
                            Assert.AreEqual(1.07m, trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                            Assert.AreEqual(1000, trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(1.09m, trade1.ExitPrice);
                            Assert.AreEqual(-20, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-30, trade1.MAE);
                            Assert.AreEqual(5, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                            Assert.AreEqual(1.08m, trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                            Assert.AreEqual(2000, trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(1.09m, trade2.ExitPrice);
                            Assert.AreEqual(-20, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-40, trade2.MAE);
                            Assert.AreEqual(30, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade3.EntryTime);
                            Assert.AreEqual(1.08m, trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                            Assert.AreEqual(1000, trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(1.09m, trade3.ExitPrice);
                            Assert.AreEqual(-10, trade3.ProfitLoss);
                            Assert.AreEqual(1, trade3.TotalFees);
                            Assert.AreEqual(-20, trade3.MAE);
                            Assert.AreEqual(15, trade3.MFE);
                        }
                        else
                        {
                            Assert.AreEqual(4, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade1.EntryTime);
                            Assert.AreEqual(1.08m, trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                            Assert.AreEqual(1000, trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(1.09m, trade1.ExitPrice);
                            Assert.AreEqual(-10, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-20, trade1.MAE);
                            Assert.AreEqual(15, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade2.EntryTime);
                            Assert.AreEqual(1.08m, trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                            Assert.AreEqual(1000, trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(1.09m, trade2.ExitPrice);
                            Assert.AreEqual(-10, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-20, trade2.MAE);
                            Assert.AreEqual(15, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade3.EntryTime);
                            Assert.AreEqual(1.08m, trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                            Assert.AreEqual(1000, trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(1.09m, trade3.ExitPrice);
                            Assert.AreEqual(-10, trade3.ProfitLoss);
                            Assert.AreEqual(0, trade3.TotalFees);
                            Assert.AreEqual(-20, trade3.MAE);
                            Assert.AreEqual(15, trade3.MFE);

                            var trade4 = builder.ClosedTrades[3];

                            Assert.AreEqual(Symbols.EURUSD, trade4.Symbol);
                            Assert.AreEqual(time, trade4.EntryTime);
                            Assert.AreEqual(1.07m, trade4.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade4.Direction);
                            Assert.AreEqual(1000, trade4.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade4.ExitTime);
                            Assert.AreEqual(1.09m, trade4.ExitPrice);
                            Assert.AreEqual(-20, trade4.ProfitLoss);
                            Assert.AreEqual(1, trade4.TotalFees);
                            Assert.AreEqual(-30, trade4.MAE);
                            Assert.AreEqual(5, trade4.MFE);
                        }
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.0775m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(4000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(1.09m, trade.ExitPrice);
                        Assert.AreEqual(-50, trade.ProfitLoss);
                        Assert.AreEqual(5, trade.TotalFees);
                        Assert.AreEqual(-90, trade.MAE);
                        Assert.AreEqual(50, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.0766666666666666666666666667m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(3000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -40, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -60 : -70, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 45 : 35, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut3Long(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Buy 1k, Buy 1k, Sell 2k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 2k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.10m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 1k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 2k
            builder.ProcessFill(new OrderEvent(6, Symbols.EURUSD, time.AddMinutes(50), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(4, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(20), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.09m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(1.10m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -25, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 10, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                        Assert.AreEqual(1.08m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(1000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(1.10m, trade2.ExitPrice);
                        Assert.AreEqual(20, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(-15, trade2.MAE);
                        Assert.AreEqual(20, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time.AddMinutes(40), trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.09m : 1.08m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(1000, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade3.ExitTime);
                        Assert.AreEqual(1.09m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 0 : 10, trade3.ProfitLoss);
                        Assert.AreEqual(2, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -25 : -15, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 20, trade3.MFE);

                        var trade4 = builder.ClosedTrades[3];

                        Assert.AreEqual(Symbols.EURUSD, trade4.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(40) : time, trade4.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade4.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade4.Direction);
                        Assert.AreEqual(1000, trade4.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade4.ExitTime);
                        Assert.AreEqual(1.09m, trade4.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 20, trade4.ProfitLoss);
                        Assert.AreEqual(1, trade4.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -15 : -5, trade4.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 30, trade4.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.08m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(4000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade.ExitTime);
                        Assert.AreEqual(1.095m, trade.ExitPrice);
                        Assert.AreEqual(60, trade.ProfitLoss);
                        Assert.AreEqual(6, trade.TotalFees);
                        Assert.AreEqual(-60, trade.MAE);
                        Assert.AreEqual(80, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.075m : 1.085m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(2000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(1.10m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 50 : 30, trade1.ProfitLoss);
                        Assert.AreEqual(4, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -40, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 50 : 30, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.085m : 1.075m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(2000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -20, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 50, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut3Short(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Sell 1k, Sell 1k, Buy 2k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 2k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.10m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 1k
            builder.ProcessFill(new OrderEvent(5, Symbols.EURUSD, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 2k
            builder.ProcessFill(new OrderEvent(6, Symbols.EURUSD, time.AddMinutes(50), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.09m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(4, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(20), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.09m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(1.10m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -10, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 25, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                        Assert.AreEqual(1.08m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(1000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(1.10m, trade2.ExitPrice);
                        Assert.AreEqual(-20, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(-20, trade2.MAE);
                        Assert.AreEqual(15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time.AddMinutes(40), trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.09m : 1.08m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(1000, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade3.ExitTime);
                        Assert.AreEqual(1.09m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 0 : -10, trade3.ProfitLoss);
                        Assert.AreEqual(2, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -20, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 25 : 15, trade3.MFE);

                        var trade4 = builder.ClosedTrades[3];

                        Assert.AreEqual(Symbols.EURUSD, trade4.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(40) : time, trade4.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade4.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade4.Direction);
                        Assert.AreEqual(1000, trade4.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade4.ExitTime);
                        Assert.AreEqual(1.09m, trade4.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -20, trade4.ProfitLoss);
                        Assert.AreEqual(1, trade4.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -30, trade4.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 15 : 5, trade4.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.08m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(4000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade.ExitTime);
                        Assert.AreEqual(1.095m, trade.ExitPrice);
                        Assert.AreEqual(-60, trade.ProfitLoss);
                        Assert.AreEqual(6, trade.TotalFees);
                        Assert.AreEqual(-80, trade.MAE);
                        Assert.AreEqual(60, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.075m : 1.085m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(2000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(1.10m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -50 : -30, trade1.ProfitLoss);
                        Assert.AreEqual(4, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -50 : -30, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 40, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.085m : 1.075m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(2000, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -50, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 20, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut4Long(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Buy 1k, Buy 1k, Sell 1.5k, Sell 0.5k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1.5k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1500, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Sell 0.5k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.10m, fillQuantity: -500, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(500, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 10, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -7.5 : -2.5, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(500, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade3.ExitTime);
                        Assert.AreEqual(1.10m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade3.ProfitLoss);
                        Assert.AreEqual(1, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -7.5 : -2.5, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade3.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.075m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(2000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade.ExitTime);
                        Assert.AreEqual(1.0925m, trade.ExitPrice);
                        Assert.AreEqual(35, trade.ProfitLoss);
                        Assert.AreEqual(4, trade.TotalFees);
                        Assert.AreEqual(-20, trade.MAE);
                        Assert.AreEqual(50, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.0733333333333333333333333333m : 1.0766666666666666666666666667m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(1500, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 25 : 20, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -12.5 : -17.5, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 35, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(500, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(1.10m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -7.5 : -2.5, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void ScaleInScaleOut4Short(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            // Sell 1k, Sell 1k, Buy 1.5k, Buy 0.5k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.065m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Buy 1.5k
            builder.ProcessFill(new OrderEvent(3, Symbols.EURUSD, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: 1500, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            // Buy 0.5k
            builder.ProcessFill(new OrderEvent(4, Symbols.EURUSD, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.10m, fillQuantity: 500, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.07m : 1.08m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1000, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(500, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                        Assert.AreEqual(1.09m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -10, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 7.5 : 2.5, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.EURUSD, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade3.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(500, trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade3.ExitTime);
                        Assert.AreEqual(1.10m, trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade3.ProfitLoss);
                        Assert.AreEqual(1, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 7.5 : 2.5, trade3.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToFlat:
                    {
                        Assert.AreEqual(1, builder.ClosedTrades.Count);

                        var trade = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(1.075m, trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(2000, trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade.ExitTime);
                        Assert.AreEqual(1.0925m, trade.ExitPrice);
                        Assert.AreEqual(-35, trade.ProfitLoss);
                        Assert.AreEqual(4, trade.TotalFees);
                        Assert.AreEqual(-50, trade.MAE);
                        Assert.AreEqual(20, trade.MFE);
                    }
                    break;

                case FillGroupingMethod.FlatToReduced:
                    {
                        Assert.AreEqual(2, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.EURUSD, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.0733333333333333333333333333m : 1.0766666666666666666666666667m, trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(1500, trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(1.09m, trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -25 : -20, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -35, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 12.5 : 17.5, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.EURUSD, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1.08m : 1.07m, trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(500, trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(1.10m, trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 7.5 : 2.5, trade2.MFE);
                    }
                    break;
            }
        }

        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FillToFill, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToFlat, FillMatchingMethod.LIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.FIFO)]
        [TestCase(FillGroupingMethod.FlatToReduced, FillMatchingMethod.LIFO)]
        public void AllInAllOutLongWithMultiplier(FillGroupingMethod groupingMethod, FillMatchingMethod matchingMethod)
        {
            var multiplier = 10;

            // Buy 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.EURUSD, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount, multiplier);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.EURUSD));

            builder.SetMarketPrice(Symbols.EURUSD, 1.075m);
            builder.SetMarketPrice(Symbols.EURUSD, 1.10m);

            // Sell 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.EURUSD, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell, fillPrice: 1.09m, fillQuantity: -1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount, multiplier);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.EURUSD));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.EURUSD, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(1.08m, trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade.Direction);
            Assert.AreEqual(1000, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(1.09m, trade.ExitPrice);
            Assert.AreEqual(10 * multiplier, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-5 * multiplier, trade.MAE);
            Assert.AreEqual(20m * multiplier, trade.MFE);
        }

    }
}
