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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
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
        public void PerformsStopLimitFillBuy()
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, 100, 101.5m, 101.75m, Noon);
            
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var security = CreateEquity(configTradeBar);

            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 101m, 100m, 100m, 100));

            // Prices above the trigger price: no trigger, no fill
            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 102m, 100m, 101m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(101m, 101m, 100.5m, 100m), 100, // Bid bar
                    new Bar(101m, 102m, 100.5m, 102m), 100) // Ask bar
            );

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // Time jump => limit reached, security bought
            // |---> First, ensure that price data only triggers the order
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));
            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Second, ensure that quote data is not used to fill
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(100m, 100m, 100m, 100m), 100, // Bid bar
                    new Bar(100m, 100m, 100m, 100m), 100) // Ask bar
            );
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Third, ensure that fill forward data is not used to fill
            // Fill forward data is not cached (SetMarketPrice)
            var tradeBar = new TradeBar(Noon, Symbols.SPY, 101m, 101m, 99m, 99m, 100);
            var fillForwardedTradeBar = tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardedTradeBar);
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Last, ensure that trade data used to fill
            security.SetMarketPrice(tradeBar);
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsStopLimitFillSell()
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, -100, 101.75m, 101.50m, Noon);

            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var security = CreateEquity(configTradeBar);

            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));

            // Prices above the trigger price: no trigger, no fill
            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101.75m, 102m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(101m, 102m, 100m, 100m), 100, // Bid bar
                    new Bar(103m, 104m, 102m, 102m), 100) // Ask bar
            );

            fill = model.StopLimitFill(security, order);

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // Time jump => limit reached, security bought
            // |---> First, ensure that price data only triggers the order
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 99m, 99m, 100));
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Second, ensure that quote data is not used to fill
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(105m, 105m, 105m, 105m), 100, // Bid bar
                    new Bar(105m, 105m, 105m, 105m), 100) // Ask bar
            );
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Third, ensure that fill forward data is not used to fill
            // Fill forward data is not cached (SetMarketPrice)
            var tradeBar = new TradeBar(Noon, Symbols.SPY, 106m, 106m, 99m, 99m, 100);
            var fillForwardedTradeBar = tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardedTradeBar);
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // |---> Last, ensure that trade data used to fill
            security.SetMarketPrice(tradeBar);
            fill = model.StopLimitFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 290.55, 290.50)]
        [TestCase(-100, 291.45, 291.50)]
        public void PerformsStopLimitFillWithTickTradeData(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
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

            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

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

            // Create a series of price where the last value will not trigger
            // and the fill model need to use the minimum/maximum instead
            var trades = new[] { 0m, -0.05m, 0m, 0.05m, -0.1m }
                .Select(delta => new Tick
                {
                    TickType = TickType.Trade,
                    Time = time,
                    Value = stopPrice - delta * Math.Sign(orderQuantity)
                })
                .ToList();

            equity.Update(trades, typeof(Tick));

            fill = fillModel.StopLimitFill(equity, order);

            // Do not fill, but trigger
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // Do not fill with quote data
            equity.SetMarketPrice(new Tick
            {
                TickType = TickType.Quote,
                Time = time,
                BidPrice = limitPrice + 0.01m,
                AskPrice = limitPrice - 0.01m
            });

            fill = fillModel.StopLimitFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            // Fill with trade data
            equity.SetMarketPrice(new Tick
            {
                TickType = TickType.Trade,
                Time = time,
                Value = limitPrice - 0.1m * Math.Sign(orderQuantity)
            });

            fill = fillModel.StopLimitFill(equity, order);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
        }

        [TestCase(100, 290.45, 290.50)]
        [TestCase(-100, 291.55, 291.50)]
        public void StopLimitOrderDoesNotFillUsingQuoteBar(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
        {
            var fillModel = new EquityFillModel();

            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var equity = CreateEquity(configTradeBar);

            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not fill with these prices
            var tradeBar = new TradeBar(time, Symbols.SPY, 291m, 291m, 291m, 291m, 12345);
            equity.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                equity,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

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

            fill = fillModel.StopLimitFill(equity, order);

            // StopLimit orders don't trigger with QuoteBar:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);
        }

        [TestCase(100, 290.55, 290.50)]
        [TestCase(-100, 291.45, 291.50)]
        public void StopLimitOrderFillsUsingQuoteBarIfTriggersAndFillsInTheSameBar(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var equity = CreateEquity(configTradeBar);
            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not trigger with the limit price
            var price = stopPrice - 0.01m * Math.Sign(orderQuantity);
            var tradeBar = new TradeBar(time, Symbols.SPY, price, price, price, price, 12345);
            equity.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                equity,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            // Do not fill on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // The trade bar will trigger the limit order
            tradeBar = new TradeBar(time, Symbols.SPY, stopPrice, stopPrice, stopPrice, stopPrice, 12345);
            equity.SetMarketPrice(tradeBar);

            // The quote bar will fill the limit order
            var quoteBar = new QuoteBar(time, Symbols.SPY,
                new Bar(290m, 292m, 289m, limitPrice + 0.01m), 12345,
                new Bar(290m, 292m, 289m, limitPrice - 0.01m), 12345);
            equity.SetMarketPrice(quoteBar);

            fill = fillModel.StopLimitFill(equity, order);

            // Stop limit orders don't trigger with QuoteBar:
            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.True(order.StopTriggered);
        }

        [TestCase(100, 290.50, 290.50)]
        [TestCase(-100, 291.50, 291.50)]
        public void StopLimitOrderDoesNotFillUsingTickTypeQuote(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
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

            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

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
            var quoteTick = new Tick { TickType = TickType.Quote, Time = time, BidPrice = price, AskPrice = price, Value = price };
            equity.SetMarketPrice(quoteTick);

            fill = fillModel.StopLimitFill(equity, order);

            // StopLimit orders don't trigger with TickType.Quote:
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);
        }

        [TestCase(100, 290.50, 290.50)]
        [TestCase(-100, 291.50, 291.50)]
        public void StopLimitOrderFillsUsingTickTypeQuoteIfTriggersAndFillsInTheSameBar(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
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

            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

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
            var quoteTick = new Tick { TickType = TickType.Quote, Time = time, BidPrice = price, AskPrice = price, Value = price };
            tradeTick = new Tick { TickType = TickType.Trade, Time = time, Value = stopPrice };
            equity.Update(new[] { tradeTick, quoteTick }, typeof(Tick));

            fill = fillModel.StopLimitFill(equity, order);

            // Stop limit orders don't trigger with TickType.Quote:
            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.True(order.StopTriggered);
        }

        [TestCase(100, 290.55, 290.50)]
        [TestCase(-100, 291.45, 291.50)]
        public void StopLimitOrderFillsAtLimitPriceWithFavorableGap(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
        {
            // See https://github.com/QuantConnect/Lean/issues/963
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

            var fillModel = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var equity = CreateEquity(configTradeBar);

            equity.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The order will not trigger with these prices
            var price = stopPrice - 0.01m * Math.Sign(orderQuantity);
            var tradeBar = new TradeBar(time, Symbols.SPY, price, price, price, price, 12345);
            equity.SetMarketPrice(tradeBar);

            var fill = fillModel.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(configTradeBar),
                Time.OneHour,
                null)).Single();

            // Do not trigger on stale data
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.False(order.StopTriggered);

            // The order will not trigger with these prices
            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time, Symbols.SPY, stopPrice, stopPrice, stopPrice, stopPrice, 12345));

            fill = fillModel.StopLimitFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.True(order.StopTriggered);

            time += TimeSpan.FromMinutes(2);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // The Gap TradeBar has all prices below/above the limit price 
            var gapTradeBar = Math.Sign(orderQuantity) switch
            {
                1 => new TradeBar(time, Symbols.SPY, limitPrice - 1, limitPrice - 1, limitPrice - 2, limitPrice - 1, 12345),
                -1 => new TradeBar(time, Symbols.SPY, limitPrice + 1, limitPrice + 2, limitPrice + 1, limitPrice + 1, 12345),
            };

            equity.SetMarketPrice(gapTradeBar);

            fill = fillModel.StopLimitFill(equity, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(100, 291.50, 291.75)]
        [TestCase(-100, 290.50, 290.25)]
        public void StopLimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new EquityFillModel();
            var order = new StopLimitOrder(symbol, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.StopLimitFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }
    }
}
