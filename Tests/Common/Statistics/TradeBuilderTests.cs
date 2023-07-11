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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class TradeBuilderTests
    {
        private readonly OrderFee _orderFee = new OrderFee(new CashAmount(1, Currencies.USD));
        private const decimal ConversionRate = 1;
        private readonly DateTime _startTime = new DateTime(2015, 08, 06, 15, 30, 0);
        private SecurityManager _securityManager;

        [SetUp]
        public void SetUp()
        {
            _securityManager = new SecurityManager(new TimeKeeper(_startTime));
        }

        [Test]
        public void AllInAllOutLong(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.SPY, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade.Direction);
            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
            Assert.AreEqual(10, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-5, trade.MAE);
            Assert.AreEqual(20m, trade.MFE);
        }

        [Test]
        public void AllInAllOutShort(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.SPY, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Short, trade.Direction);
            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
            Assert.AreEqual(-10, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-20, trade.MAE);
            Assert.AreEqual(5, trade.MFE);
        }

        [Test]
        public void ScaleInAllOutLong(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 2k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            if (groupingMethod == FillGroupingMethod.FillToFill)
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 0 : 1];

                Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                Assert.AreEqual(10, trade1.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 2 : 1, trade1.TotalFees);
                Assert.AreEqual(-15, trade1.MAE);
                Assert.AreEqual(20, trade1.MFE);

                var trade2 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 1 : 0];

                Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                Assert.AreEqual(20, trade2.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1 : 2, trade2.TotalFees);
                Assert.AreEqual(-5, trade2.MAE);
                Assert.AreEqual(30, trade2.MFE);
            }
            else
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.075m, split), trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
                Assert.AreEqual(30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-20, trade.MAE);
                Assert.AreEqual(50, trade.MFE);
            }
        }

        [Test]
        public void ScaleInAllOutShort(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            if (groupingMethod == FillGroupingMethod.FillToFill)
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                var trade1 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 0 : 1];

                Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                Assert.AreEqual(time, trade1.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade1.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                Assert.AreEqual(-10, trade1.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 2 : 1, trade1.TotalFees);
                Assert.AreEqual(-20, trade1.MAE);
                Assert.AreEqual(15, trade1.MFE);

                var trade2 = builder.ClosedTrades[matchingMethod == FillMatchingMethod.FIFO ? 1 : 0];

                Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                Assert.AreEqual(-20, trade2.ProfitLoss);
                Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 1 : 2, trade2.TotalFees);
                Assert.AreEqual(-30, trade2.MAE);
                Assert.AreEqual(5, trade2.MFE);
            }
            else
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.075m, split), trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
                Assert.AreEqual(-30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-50, trade.MAE);
                Assert.AreEqual(20, trade.MFE);
            }
        }

        [Test]
        public void AllInScaleOutLong(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 2k, Sell 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: 2000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            if (groupingMethod == FillGroupingMethod.FlatToFlat)
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.085m, split), trade.ExitPrice);
                Assert.AreEqual(30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-10, trade.MAE);
                Assert.AreEqual(60, trade.MFE);
            }
            else
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                // This first trade was closed before the split
                var trade1 = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade1.Symbol);
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

                Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                Assert.AreEqual(time, trade2.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                Assert.AreEqual(20, trade2.ProfitLoss);
                Assert.AreEqual(1, trade2.TotalFees);
                Assert.AreEqual(-5, trade2.MAE);
                Assert.AreEqual(30, trade2.MFE);
            }
        }

        [Test]
        public void AllInScaleOutShort(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 2k, Buy 1k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 2k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -2000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            if (groupingMethod == FillGroupingMethod.FlatToFlat)
            {
                Assert.AreEqual(1, builder.ClosedTrades.Count);

                var trade = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade.Symbol);
                Assert.AreEqual(time, trade.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.085m, split), trade.ExitPrice);
                Assert.AreEqual(-30, trade.ProfitLoss);
                Assert.AreEqual(3, trade.TotalFees);
                Assert.AreEqual(-60, trade.MAE);
                Assert.AreEqual(10, trade.MFE);
            }
            else
            {
                Assert.AreEqual(2, builder.ClosedTrades.Count);

                // This first trade was closed before the split
                var trade1 = builder.ClosedTrades[0];

                Assert.AreEqual(Symbols.SPY, trade1.Symbol);
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

                Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                Assert.AreEqual(time, trade2.EntryTime);
                Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade2.EntryPrice);
                Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                Assert.AreEqual(-20, trade2.ProfitLoss);
                Assert.AreEqual(1, trade2.TotalFees);
                Assert.AreEqual(-30, trade2.MAE);
                Assert.AreEqual(5, trade2.MFE);
            }
        }

        [Test]
        public void ReversalLongToShort(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Sell 2k, Buy 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 2k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -2000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            Assert.AreEqual(2, builder.ClosedTrades.Count);

            // This first trade was closed before the split
            var trade1 = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
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

            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
            Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
            Assert.AreEqual(-10, trade2.ProfitLoss);
            Assert.AreEqual(1, trade2.TotalFees);
            Assert.AreEqual(-20, trade2.MAE);
            Assert.AreEqual(15, trade2.MFE);
        }

        [Test]
        public void ReversalShortToLong(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Buy 2k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 2000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            Assert.AreEqual(2, builder.ClosedTrades.Count);

            // This first trade was closed before the split
            var trade1 = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
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

            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
            Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
            Assert.AreEqual(10, trade2.ProfitLoss);
            Assert.AreEqual(1, trade2.TotalFees);
            Assert.AreEqual(-15, trade2.MAE);
            Assert.AreEqual(20, trade2.MFE);
        }

        [Test]
        public void ScaleInScaleOut1Long(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Buy 1k, Sell 1k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 2k
            builder.ProcessFill(
                new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10),
                            trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time.AddMinutes(30),
                            trade2.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(10, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(-15, trade2.MAE);
                        Assert.AreEqual(20, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(30) : time,
                            trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.Less(
                            Math.Abs(AdjustPriceToSplit(1.0766666666666666666666666667m, split) - trade.EntryPrice),
                            1e-27m);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(3000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10),
                            trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time,
                            trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.075m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 50, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut1Short(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Sell 1k, Buy 1k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10),
                            trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time.AddMinutes(30), trade2.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(-10, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(-20, trade2.MAE);
                        Assert.AreEqual(15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(30) : time, trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.Less(
                            Math.Abs(AdjustPriceToSplit(1.0766666666666666666666666667m, split) - trade.EntryPrice),
                            1e-27m);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(3000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.075m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -50, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut2Long(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Buy 2k, Sell 1k, Buy 1k, Sell 3k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 2k
            builder.ProcessFill(new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 2000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell, fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 1k
            builder.ProcessFill(new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy, fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 3k
            builder.ProcessFill(new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell, fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-3000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(3, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                            Assert.AreEqual(time, trade1.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                            Assert.AreEqual(20, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-5, trade1.MAE);
                            Assert.AreEqual(30, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                            Assert.AreEqual(20, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-30, trade2.MAE);
                            Assert.AreEqual(40, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade3.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                            Assert.AreEqual(10, trade3.ProfitLoss);
                            Assert.AreEqual(1, trade3.TotalFees);
                            Assert.AreEqual(-15, trade3.MAE);
                            Assert.AreEqual(20, trade3.MFE);
                        }
                        else
                        {
                            Assert.AreEqual(4, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade1.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                            Assert.AreEqual(10, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-15, trade1.MAE);
                            Assert.AreEqual(20, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade2.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                            Assert.AreEqual(10, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-15, trade2.MAE);
                            Assert.AreEqual(20, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade3.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                            Assert.AreEqual(10, trade3.ProfitLoss);
                            Assert.AreEqual(0, trade3.TotalFees);
                            Assert.AreEqual(-15, trade3.MAE);
                            Assert.AreEqual(20, trade3.MFE);

                            var trade4 = builder.ClosedTrades[3];

                            Assert.AreEqual(Symbols.SPY, trade4.Symbol);
                            Assert.AreEqual(time, trade4.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade4.EntryPrice);
                            Assert.AreEqual(TradeDirection.Long, trade4.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade4.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade4.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade4.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.0775m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(4000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        }
                        else
                        {
                            Assert.Less(
                                Math.Abs(AdjustPriceToSplit(1.0766666666666666666666666667m, split) - trade2.EntryPrice),
                                1e-27m);
                        }
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(3000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 40, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -45 : -35, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 60 : 70, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut2Short(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Sell 2k, Buy 1k, Sell 1k, Buy 3k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 2k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -2000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 3k
            builder.ProcessFill(
                new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(3000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(3, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                            Assert.AreEqual(time, trade1.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                            Assert.AreEqual(-20, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-30, trade1.MAE);
                            Assert.AreEqual(5, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                            Assert.AreEqual(-20, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-40, trade2.MAE);
                            Assert.AreEqual(30, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade3.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                            Assert.AreEqual(-10, trade3.ProfitLoss);
                            Assert.AreEqual(1, trade3.TotalFees);
                            Assert.AreEqual(-20, trade3.MAE);
                            Assert.AreEqual(15, trade3.MFE);
                        }
                        else
                        {
                            Assert.AreEqual(4, builder.ClosedTrades.Count);

                            var trade1 = builder.ClosedTrades[0];

                            Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade1.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade1.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                            Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                            Assert.AreEqual(-10, trade1.ProfitLoss);
                            Assert.AreEqual(2, trade1.TotalFees);
                            Assert.AreEqual(-20, trade1.MAE);
                            Assert.AreEqual(15, trade1.MFE);

                            var trade2 = builder.ClosedTrades[1];

                            Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                            Assert.AreEqual(time.AddMinutes(30), trade2.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                            Assert.AreEqual(-10, trade2.ProfitLoss);
                            Assert.AreEqual(2, trade2.TotalFees);
                            Assert.AreEqual(-20, trade2.MAE);
                            Assert.AreEqual(15, trade2.MFE);

                            var trade3 = builder.ClosedTrades[2];

                            Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                            Assert.AreEqual(time.AddMinutes(10), trade3.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade3.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade3.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                            Assert.AreEqual(-10, trade3.ProfitLoss);
                            Assert.AreEqual(0, trade3.TotalFees);
                            Assert.AreEqual(-20, trade3.MAE);
                            Assert.AreEqual(15, trade3.MFE);

                            var trade4 = builder.ClosedTrades[3];

                            Assert.AreEqual(Symbols.SPY, trade4.Symbol);
                            Assert.AreEqual(time, trade4.EntryTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.07m, split), trade4.EntryPrice);
                            Assert.AreEqual(TradeDirection.Short, trade4.Direction);
                            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade4.Quantity);
                            Assert.AreEqual(time.AddMinutes(40), trade4.ExitTime);
                            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade4.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.0775m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(4000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        if (matchingMethod == FillMatchingMethod.FIFO)
                        {
                            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        }
                        else
                        {
                            Assert.Less(
                                Math.Abs(AdjustPriceToSplit(1.0766666666666666666666666667m, split) - trade2.EntryPrice),
                                1e-27m);
                        }
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(3000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(40), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -40, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -60 : -70, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 45 : 35, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut3Long(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Buy 1k, Buy 1k, Sell 2k, Buy 1k, Sell 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 1k
            builder.ProcessFill(new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy, fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1k
            builder.ProcessFill(new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy, fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 2k
            builder.ProcessFill(new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell, fillPrice: AdjustPriceToSplit(1.10m, split), fillQuantity: AdjustQuantityToSplit(-2000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 1k
            builder.ProcessFill(new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Buy, fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(1000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 2k
            builder.ProcessFill(new OrderEvent(6, Symbols.SPY, time.AddMinutes(50), OrderStatus.Filled, OrderDirection.Sell, fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-2000, split), orderFee: _orderFee), ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(4, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(20), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.09m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -25, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 10, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade2.ExitPrice);
                        Assert.AreEqual(20, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(-15, trade2.MAE);
                        Assert.AreEqual(20, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time.AddMinutes(40), trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.09m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 0 : 10, trade3.ProfitLoss);
                        Assert.AreEqual(2, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -25 : -15, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 20, trade3.MFE);

                        var trade4 = builder.ClosedTrades[3];

                        Assert.AreEqual(Symbols.SPY, trade4.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(40) : time, trade4.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade4.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade4.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade4.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade4.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade4.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(4000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.095m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.075m, split)
                                : AdjustPriceToSplit(1.085m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 50 : 30, trade1.ProfitLoss);
                        Assert.AreEqual(4, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -40, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 50 : 30, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.085m, split)
                                : AdjustPriceToSplit(1.075m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -20, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 50, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut3Short(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Sell 1k, Sell 1k, Buy 2k, Sell 1k, Buy 2k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.10m, split), fillQuantity: AdjustQuantityToSplit(2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(5, Symbols.SPY, time.AddMinutes(40), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.08m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 2k
            builder.ProcessFill(
                new OrderEvent(6, Symbols.SPY, time.AddMinutes(50), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(2000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(4, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(20), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.09m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -10, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 25, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(time.AddMinutes(10), trade2.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade2.ExitPrice);
                        Assert.AreEqual(-20, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(-20, trade2.MAE);
                        Assert.AreEqual(15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time.AddMinutes(40), trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.09m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade3.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 0 : -10, trade3.ProfitLoss);
                        Assert.AreEqual(2, trade3.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -20, trade3.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 25 : 15, trade3.MFE);

                        var trade4 = builder.ClosedTrades[3];

                        Assert.AreEqual(Symbols.SPY, trade4.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(40) : time, trade4.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade4.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade4.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade4.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade4.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade4.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(4000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.095m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.075m, split)
                                : AdjustPriceToSplit(1.085m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -50 : -30, trade1.ProfitLoss);
                        Assert.AreEqual(4, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -50 : -30, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 40, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(20) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.085m, split)
                                : AdjustPriceToSplit(1.075m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(50), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -30, trade2.ProfitLoss);
                        Assert.AreEqual(2, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -50, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 20, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut4Long(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Buy 1k, Buy 1k, Sell 1.5k, Sell 0.5k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1.5k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1500, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Sell 0.5k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.10m, split), fillQuantity: AdjustQuantityToSplit(-500, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 20 : 10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -15, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 30 : 20, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 10, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -7.5 : -2.5, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade3.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.075m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.0925m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.Less(
                            Math.Abs(AdjustPriceToSplit(matchingMethod == FillMatchingMethod.FIFO ? 1.0733333333333333333333333333m : 1.0766666666666666666666666667m, split) - trade1.EntryPrice),
                            1e-27m);
                        Assert.AreEqual(TradeDirection.Long, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1500, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 25 : 20, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -12.5 : -17.5, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 40 : 35, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Long, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -7.5 : -2.5, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 10 : 15, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void ScaleInScaleOut4Short(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            // Sell 1k, Sell 1k, Buy 1.5k, Buy 0.5k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.07m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: -1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.065m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Buy 1.5k
            builder.ProcessFill(
                new OrderEvent(3, Symbols.SPY, time.AddMinutes(20), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(1500, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            // Buy 0.5k
            builder.ProcessFill(
                new OrderEvent(4, Symbols.SPY, time.AddMinutes(30), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.10m, split), fillQuantity: AdjustQuantityToSplit(500, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            switch (groupingMethod)
            {
                case FillGroupingMethod.FillToFill:
                    {
                        Assert.AreEqual(3, builder.ClosedTrades.Count);

                        var trade1 = builder.ClosedTrades[0];

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.07m, split)
                                : AdjustPriceToSplit(1.08m, split),
                            trade1.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -20 : -10, trade1.ProfitLoss);
                        Assert.AreEqual(2, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -30 : -20, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 5 : 15, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -5 : -10, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 7.5 : 2.5, trade2.MFE);

                        var trade3 = builder.ClosedTrades[2];

                        Assert.AreEqual(Symbols.SPY, trade3.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade3.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade3.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade3.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade3.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade3.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade3.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade.Symbol);
                        Assert.AreEqual(time, trade.EntryTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.075m, split), trade.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(2000, split), trade.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.0925m, split), trade.ExitPrice);
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

                        Assert.AreEqual(Symbols.SPY, trade1.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time : time.AddMinutes(10), trade1.EntryTime);
                        Assert.Less(
                            Math.Abs(AdjustPriceToSplit(matchingMethod == FillMatchingMethod.FIFO ? 1.0733333333333333333333333333m : 1.0766666666666666666666666667m, split) - trade1.EntryPrice),
                            1e-27m);
                        Assert.AreEqual(TradeDirection.Short, trade1.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(1500, split), trade1.Quantity);
                        Assert.AreEqual(time.AddMinutes(20), trade1.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade1.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -25 : -20, trade1.ProfitLoss);
                        Assert.AreEqual(3, trade1.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -40 : -35, trade1.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 12.5 : 17.5, trade1.MFE);

                        var trade2 = builder.ClosedTrades[1];

                        Assert.AreEqual(Symbols.SPY, trade2.Symbol);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? time.AddMinutes(10) : time, trade2.EntryTime);
                        Assert.AreEqual(
                            matchingMethod == FillMatchingMethod.FIFO
                                ? AdjustPriceToSplit(1.08m, split)
                                : AdjustPriceToSplit(1.07m, split),
                            trade2.EntryPrice);
                        Assert.AreEqual(TradeDirection.Short, trade2.Direction);
                        Assert.AreEqual(AdjustQuantityToSplit(500, split), trade2.Quantity);
                        Assert.AreEqual(time.AddMinutes(30), trade2.ExitTime);
                        Assert.AreEqual(AdjustPriceToSplit(1.10m, split), trade2.ExitPrice);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.ProfitLoss);
                        Assert.AreEqual(1, trade2.TotalFees);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? -10 : -15, trade2.MAE);
                        Assert.AreEqual(matchingMethod == FillMatchingMethod.FIFO ? 7.5 : 2.5, trade2.MFE);
                    }
                    break;
            }
        }

        [Test]
        public void AllInAllOutLongWithMultiplier(
            [Values] FillGroupingMethod groupingMethod,
            [Values] FillMatchingMethod matchingMethod,
            // 0 for no split
            [Values(0, 0.5, 0.333)] double splitFactor)
        {
            var multiplier = 10;

            // Buy 1k, Sell 1k

            var builder = new TradeBuilder(groupingMethod, matchingMethod);
            builder.SetSecurityManager(_securityManager);
            var time = _startTime;

            // Buy 1k
            builder.ProcessFill(
                new OrderEvent(1, Symbols.SPY, time, OrderStatus.Filled, OrderDirection.Buy,
                    fillPrice: 1.08m, fillQuantity: 1000, orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount, multiplier);

            Assert.IsTrue(builder.HasOpenPosition(Symbols.SPY));

            builder.SetMarketPrice(Symbols.SPY, 1.075m);
            builder.SetMarketPrice(Symbols.SPY, 1.10m);

            Split split = null;
            if (splitFactor != 0)
            {
                // apply a 2:1 split
                split = new Split(Symbols.SPY, time.AddMinutes(5), 1.10m, (decimal)splitFactor, SplitType.SplitOccurred);
                builder.ApplySplit(split, false, DataNormalizationMode.Raw);
            }

            // Sell 1k
            builder.ProcessFill(
                new OrderEvent(2, Symbols.SPY, time.AddMinutes(10), OrderStatus.Filled, OrderDirection.Sell,
                    fillPrice: AdjustPriceToSplit(1.09m, split), fillQuantity: AdjustQuantityToSplit(-1000, split), orderFee: _orderFee),
                ConversionRate, _orderFee.Value.Amount, multiplier);

            Assert.IsFalse(builder.HasOpenPosition(Symbols.SPY));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(Symbols.SPY, trade.Symbol);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(AdjustPriceToSplit(1.08m, split), trade.EntryPrice);
            Assert.AreEqual(TradeDirection.Long, trade.Direction);
            Assert.AreEqual(AdjustQuantityToSplit(1000, split), trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(AdjustPriceToSplit(1.09m, split), trade.ExitPrice);
            Assert.AreEqual(10 * multiplier, trade.ProfitLoss);
            Assert.AreEqual(2, trade.TotalFees);
            Assert.AreEqual(-5 * multiplier, trade.MAE);
            Assert.AreEqual(20m * multiplier, trade.MFE);
        }

        [Test]
        public void ITMOptionAssignment(
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection,
            [Values] bool win)
        {
            var time = _startTime;
            var option = GetOption();
            var underlying = option.Underlying;

            option.SetMarketPrice(new Tick { Value = 100m });

            var underlyingPrice = 0m;
            if (win)
            {
                underlyingPrice = orderDirection == OrderDirection.Buy ? 300m : 290m;
            }
            else
            {
                underlyingPrice = orderDirection == OrderDirection.Buy ? 290m : 300m;
            }
            underlying.SetMarketPrice(new Tick { Value = underlyingPrice });

            var builder = new TradeBuilder(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO);
            builder.SetSecurityManager(_securityManager);

            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time, OrderStatus.Filled, orderDirection, 100m, quantity, _orderFee) { IsInTheMoney = true },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsTrue(builder.HasOpenPosition(option.Symbol));

            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, -quantity, 0, 0, time, ""));
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time.AddMinutes(10), OrderStatus.Filled, closingOrderDirection, 0m, -quantity, _orderFee)
                {
                    IsInTheMoney = true,
                    Ticket = ticket
                },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsFalse(builder.HasOpenPosition(option.Symbol));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(option.Symbol, trade.Symbol);
            Assert.AreEqual(win, trade.IsWin);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(100m, trade.EntryPrice);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? TradeDirection.Long : TradeDirection.Short, trade.Direction);
            Assert.AreEqual(10, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(0, trade.ExitPrice);
            Assert.AreEqual(Math.Sign(quantity) * -100000m, trade.ProfitLoss);
            Assert.AreEqual(1m, trade.TotalFees);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? -100000m : 0m, trade.MAE);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? 0m : 100000m, trade.MFE);
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void OTMOptionAssignment(OrderDirection orderDirection)
        {
            var time = _startTime;
            var option = GetOption();
            var underlying = option.Underlying;

            option.SetMarketPrice(new Tick { Value = 100m });
            underlying.SetMarketPrice(new Tick { Value = 150 });

            var builder = new TradeBuilder(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO);
            builder.SetSecurityManager(_securityManager);

            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time, OrderStatus.Filled, orderDirection, 100m, quantity, _orderFee) { IsInTheMoney = true },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsTrue(builder.HasOpenPosition(option.Symbol));

            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.OptionExercise, option.Type, option.Symbol, -quantity, 0, 0, time, ""));
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time.AddMinutes(10), OrderStatus.Filled, closingOrderDirection, 0m, -quantity, _orderFee)
                {
                    IsInTheMoney = true,
                    Ticket = ticket
                },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsFalse(builder.HasOpenPosition(option.Symbol));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            Assert.AreEqual(option.Symbol, trade.Symbol);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? false : true, trade.IsWin);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(100m, trade.EntryPrice);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? TradeDirection.Long : TradeDirection.Short, trade.Direction);
            Assert.AreEqual(10, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(0, trade.ExitPrice);
            Assert.AreEqual(Math.Sign(quantity) * -100000m, trade.ProfitLoss);
            Assert.AreEqual(1m, trade.TotalFees);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? -100000m : 0m, trade.MAE);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? 0m : 100000m, trade.MFE);
        }

        [Test]
        public void OptionPositionCloseWithoutExercise(
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection,
            [Values] bool win)
        {
            var time = _startTime;
            var option = GetOption();
            var underlying = option.Underlying;

            underlying.SetMarketPrice(new Tick { Value = 300m });

            var builder = new TradeBuilder(FillGroupingMethod.FillToFill, FillMatchingMethod.FIFO);
            builder.SetSecurityManager(_securityManager);

            var initialOptionPrice = 100m;
            option.SetMarketPrice(new Tick { Value = initialOptionPrice });

            var quantity = orderDirection == OrderDirection.Buy ? 10 : -10;
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time, OrderStatus.Filled, orderDirection, 100m, quantity, _orderFee) { IsInTheMoney = true },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsTrue(builder.HasOpenPosition(option.Symbol));

            // Before closing, update option market price
            var finalOptionPrice = 0m;
            if (win)
            {
                finalOptionPrice = orderDirection == OrderDirection.Buy ? 150m : 50m;
            }
            else
            {
                finalOptionPrice = orderDirection == OrderDirection.Buy ? 50m : 150m;
            }
            option.SetMarketPrice(new Tick { Value = finalOptionPrice });

            var closingOrderDirection = orderDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            var ticket = new OrderTicket(null, new SubmitOrderRequest(OrderType.Market, option.Type, option.Symbol, -quantity, 0, 0, time, ""));
            builder.ProcessFill(
                new OrderEvent(1, option.Symbol, time.AddMinutes(10), OrderStatus.Filled, closingOrderDirection, finalOptionPrice, -quantity, _orderFee)
                {
                    IsInTheMoney = true,
                    Ticket = ticket,
                },
                ConversionRate,
                _orderFee.Value.Amount,
                100m);

            Assert.IsFalse(builder.HasOpenPosition(option.Symbol));

            Assert.AreEqual(1, builder.ClosedTrades.Count);

            var trade = builder.ClosedTrades[0];

            var expectedProfitLoss = (finalOptionPrice - initialOptionPrice) * quantity * 100m;

            Assert.AreEqual(option.Symbol, trade.Symbol);
            Assert.AreEqual(win, trade.IsWin);
            Assert.AreEqual(time, trade.EntryTime);
            Assert.AreEqual(initialOptionPrice, trade.EntryPrice);
            Assert.AreEqual(orderDirection == OrderDirection.Buy ? TradeDirection.Long : TradeDirection.Short, trade.Direction);
            Assert.AreEqual(10, trade.Quantity);
            Assert.AreEqual(time.AddMinutes(10), trade.ExitTime);
            Assert.AreEqual(finalOptionPrice, trade.ExitPrice);
            Assert.AreEqual(expectedProfitLoss, trade.ProfitLoss);
        }

        private Option GetOption()
        {
            var underlying = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY,
                    Resolution.Minute,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    true,
                    true,
                    false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            var option = new Option(
                Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties("", Currencies.USD, 100, 0.01m, 1),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                underlying
            );

            _securityManager.Add(underlying);
            _securityManager.Add(option);

            return option;
        }

        private static decimal AdjustQuantityToSplit(decimal quantity, Split split)
        {
            return split == null ? quantity : quantity / split.SplitFactor;
        }

        private static decimal AdjustPriceToSplit(decimal price, Split split)
        {
            return split == null ? price : price * split.SplitFactor;
        }
    }
}
