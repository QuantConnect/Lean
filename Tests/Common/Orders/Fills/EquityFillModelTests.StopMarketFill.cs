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
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public partial class EquityFillModelTests
    {
        [Test]
        public void PerformsStopMarketFillBuy()
        {
            var model = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, 100, 101.5m, Noon);

            var parameters = GetFillModelParameters(order);

            var security = parameters.Security;
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 101m, 101m, 101m, 100));

            var fill = model.Fill(parameters).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 102.5m, 101m, 102m, 100));

            fill = model.StopMarketFill(security, order);

            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.StopPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopMarketFillSell()
        {
            var model = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, -100, 101.5m, Noon);

            var parameters = GetFillModelParameters(order);

            var security = parameters.Security;
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));

            var fill = model.Fill(parameters).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102.5m, 101m, 101.5m, 100));

            fill = model.StopMarketFill(security, order);

            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.StopPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void PerformsStopMarketFillWithTickTradeData(decimal orderQuantity, decimal stopPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

            var configTick = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(configTick);

            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with this price
            var tradeTick = new Tick { TickType = TickType.Trade, Time = time, Value = 291m };
            equity.SetMarketPrice(tradeTick);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

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
            var trades = new[] { 0.1m, -0.1m, 0m, 0.1m, 0m }
                .Select(delta => new Tick 
                    { 
                        TickType = TickType.Trade,
                        Time = time, 
                        Value = stopPrice - delta * Math.Sign(orderQuantity)
                    })
                .ToList();

            equity.Update(trades, typeof(Tick));

            fill = fillModel.StopMarketFill(equity, order);

            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            // Fill at the stop price because the equity price matches it
            Assert.AreEqual(stopPrice, fill.FillPrice);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void StopMarketOrderDoesNotFillUsingQuoteBar(decimal orderQuantity, decimal stopPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);

            var equity = parameters.Security;
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with these prices
            var tradeBar = new TradeBar(time.AddMinutes(-10), Symbols.SPY, 291m, 291m, 291m, 291m, 12345);
            equity.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

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

            fill = fillModel.StopMarketFill(equity, order);

            // Stop market orders don't fill with QuoteBar:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void StopMarketOrderDoesNotFillUsingTickTypeQuote(decimal orderQuantity, decimal stopPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

            var configTick = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(configTick);

            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with this price
            var tradeTick = new Tick { TickType = TickType.Trade, Time = time, Value = 291m };
            equity.SetMarketPrice(tradeTick);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

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

            var price = stopPrice - 0.1m * Math.Sign(orderQuantity);
            var quoteTick = new Tick { TickType = TickType.Quote, Time = time, BidPrice = price, AskPrice = price };
            equity.SetMarketPrice(quoteTick);
            
            fill = fillModel.StopMarketFill(equity, order);

            // Stop market orders don't fill with TickType.Quote:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
        }

        [TestCase(100, 290.50)]
        [TestCase(-100, 291.50)]
        public void StopMarketOrderFillsAtOpenWithUnfavourableGap(decimal orderQuantity, decimal stopPrice)
        {
            // See https://github.com/QuantConnect/Lean/issues/4545
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

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

            // The Gap TradeBar has all prices below/above the stop price 
            var gapTradeBar = Math.Sign(orderQuantity) switch
            { 
                1 => new TradeBar(time, Symbols.SPY, stopPrice + 1, stopPrice + 2, stopPrice + 1, stopPrice + 1, 12345),
                -1 => new TradeBar(time, Symbols.SPY, stopPrice - 1, stopPrice - 1, stopPrice - 2, stopPrice - 1, 12345),
            };

            equity.SetMarketPrice(gapTradeBar);

            fill = fillModel.StopMarketFill(equity, order);

            // Fills at the open
            Assert.AreEqual(gapTradeBar.Open, fill.FillPrice);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }
        
        [TestCase(100, 291.50)]
        [TestCase(-100, 290.50)]
        public void StopMarketOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopMarketOrder(Symbols.SPY, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

            var parameters = GetFillModelParameters(order);

            var security = parameters.Security;
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time.AddMinutes(-10), Symbols.SPY, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fill = fillModel.Fill(parameters).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, Symbols.SPY, stopPrice - 0.01m * Math.Sign(orderQuantity), 292m, 289m, stopPrice, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.StopMarketFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(stopPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }
    }
}
