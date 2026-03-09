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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public partial class EquityFillModelTests
    {
        [Test]
        public void PerformsLimitFillBuy()
        {
            var model = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, Noon);

            var parameters = GetFillModelParameters(order);

            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));

            var fill = model.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            equity.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(equity, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsLimitFillSell()
        {
            var model = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, -100, 101.5m, Noon);

            var parameters = GetFillModelParameters(order);

            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 101m, 101m, 101m, 100));

            var fill = model.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            equity.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(equity, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void PerformsLimitFillWithTickTradeData(decimal orderQuantity, decimal limitPrice)
        {
            var fillModel = new EquityFillModel();

            var configTick = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(configTick);

            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with this price
            var tradeTick = new Tick { TickType = TickType.Trade, Time = time, Value = 291m };
            equity.SetMarketPrice(tradeTick);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(configTick),
                Time.OneHour,
                null)).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // Create a series of price where the last value will not fill
            // and the fill model need to use the minimum/maximum instead
            var trades = new[] { 0m, -0.1m, 0m, 0.1m, 0m }
                .Select(delta => new Tick 
                    { 
                        TickType = TickType.Trade,
                        Time = time, 
                        Value = limitPrice - delta * Math.Sign(orderQuantity)
                    })
                .ToList();

            equity.Update(trades, typeof(Tick));

            fill = fillModel.LimitFill(equity, order);

            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void LimitOrderDoesNotFillUsingQuoteBar(decimal orderQuantity, decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);
            
            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with these prices
            var tradeBar = new TradeBar(time.AddMinutes(-10), Symbols.SPY, 291m, 291m, 291m, 291m, 12345);
            equity.SetMarketPrice(tradeBar);

            var fill = fillModel.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            
            var quoteBar = new QuoteBar(time, Symbols.SPY, 
                new Bar(290m, 292m, 289m, 291m), 12345,
                new Bar(290m, 292m, 289m, 291m), 12345);
            equity.SetMarketPrice(quoteBar);

            fill = fillModel.LimitFill(equity, order);

            // Limit orders don't fill with QuoteBar:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void LimitOrderDoesNotFillUsingTickTypeQuote(decimal orderQuantity, decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var configTick = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(configTick);

            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with this price
            var tradeTick = new Tick { TickType = TickType.Trade, Time = time, Value = 291m };
            equity.SetMarketPrice(tradeTick);

            var fill = fillModel.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(configTick),
                Time.OneHour,
                null)).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var price = limitPrice - 0.1m * Math.Sign(orderQuantity);
            var quoteTick = new Tick { TickType = TickType.Quote, Time = time, BidPrice = price, AskPrice = price };
            equity.SetMarketPrice(quoteTick);
            
            fill = fillModel.LimitFill(equity, order);

            // Limit orders don't fill with TickType.Quote:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void LimitOrderFillsAtLimitPriceWithFavorableGap(decimal orderQuantity, decimal limitPrice)
        {
            // See https://github.com/QuantConnect/Lean/issues/963

            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);

            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with these prices
            equity.SetMarketPrice(new TradeBar(time.AddMinutes(-10), Symbols.SPY, 291m, 291m, 291m, 291m, 12345));

            var fill = fillModel.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var tradeBar = Math.Sign(orderQuantity) switch
            { 
                1 => new TradeBar(time, Symbols.SPY, limitPrice + 1, limitPrice + 1, limitPrice - 2, limitPrice + 1, 12345),
                -1 => new TradeBar(time, Symbols.SPY, limitPrice - 1, limitPrice + 2, limitPrice - 1, limitPrice - 1, 12345),
            };

            equity.SetMarketPrice(tradeBar);

            fill = fillModel.LimitFill(equity, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void LimitOrderFillsAtOpenWithFavorableGap(decimal orderQuantity, decimal limitPrice)
        {
            // See https://github.com/QuantConnect/Lean/issues/7052

            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);

            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with these prices
            equity.SetMarketPrice(new TradeBar(time.AddMinutes(-10), Symbols.SPY, 291m, 291m, 291m, 291m, 12345));

            var fill = fillModel.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // The Gap TradeBar has all prices below/above the limit price
            var open = limitPrice - Math.Sign(orderQuantity);
            var gapTradeBar = Math.Sign(orderQuantity) switch
            {
                1 => new TradeBar(time, Symbols.SPY, open, limitPrice - 1, limitPrice - 2, limitPrice - 1, 12345),
                -1 => new TradeBar(time, Symbols.SPY, open, limitPrice + 2, limitPrice + 1, limitPrice + 1, 12345),
            };

            equity.SetMarketPrice(gapTradeBar);

            fill = fillModel.LimitFill(equity, order);

            // This fills at the open since all prices are below/above the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreNotEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(open, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void LimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new LimitOrder(Symbols.SPY, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);
            var equity = parameters.Security;

            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time.AddMinutes(-10), Symbols.SPY, 290m, 292m, 289m, 291m, 12345);
            equity.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            equity.SetMarketPrice(fillForwardBar);

            var fill = fillModel.Fill(parameters).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, Symbols.SPY, 291m, 292m, 289m, 291m, 12345);
            equity.SetMarketPrice(tradeBar);

            fill = fillModel.LimitFill(equity, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }
    }
}
