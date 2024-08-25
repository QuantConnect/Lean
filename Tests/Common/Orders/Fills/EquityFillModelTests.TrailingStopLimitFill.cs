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

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data;
using QuantConnect.Tests.Common.Securities;
using System;
using System.Linq;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public partial class EquityFillModelTests
    {
        [Test]
        public void PerformsTrailingStopLimitImmediateFillBuy([Values] bool trailingAsPercentage)
        {
            var model = new EquityFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $105. limit offset of $5, limit price $110
                ? new TrailingStopLimitOrder(Symbols.SPY, 100, 105m, 110m, 0.05m, true, 5m, Noon)
                // a trailing amount of $5 set the stop price to $105. limit offset of $5, limit price $110
                : new TrailingStopLimitOrder(Symbols.SPY, 100, 105m, 110m, 5m, false, 5m, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // Security price rises above stop price immediately
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, trailingAsPercentage ?
                100m * (1 + 0.075m) : 100m + 7.5m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // High is below limit price so fills at the high
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.High, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsTrailingStopLimitFillBuy([Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $105. limit offset of $1, limit price $106
                ? new TrailingStopLimitOrder(Symbols.SPY, 100, 105m, 106m, 0.05m, true, 1m, Noon)
                // a trailing amount of $5 set the stop price to $105. limit offset of $1, limit price $106
                : new TrailingStopLimitOrder(Symbols.SPY, 100, 105m, 106m, 5m, false, 1m, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var initialTrailingStopPrice = order.StopPrice;
            var prevMarketPrice = 100m;
            // Market price hasn't moved
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, prevMarketPrice));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a rising security price, but not enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102.5m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a falling security price, but still above the lowest market price
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a falling security price, which triggers a stop price update
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 99m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have been updated to:
            //  --> (market price + trailing amount) if trailing amount is not a percentage
            //  --> (market price * (1 + trailing amount)) if trailing amount is a percentage
            // Limit price should have been updated to (new stop price + limit offset)
            Assert.AreNotEqual(initialTrailingStopPrice, order.StopPrice);
            var expectedUpdatedStopPrice = trailingAsPercentage ? security.Price * (1 + 0.05m) : security.Price + 5m;
            var expectedUpdatedLimitPrice = expectedUpdatedStopPrice + 1m;
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);
            Assert.AreEqual(expectedUpdatedLimitPrice, order.LimitPrice);

            // Simulate a rising security price, enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 110m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Stop price should have not been updated
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            // Market price is above limit price
            AssertUnfilled(fill);

            // Market price moves below limit price and order can fill
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 110m, 110m, 102m, 103m, 100));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Assumes worst case fill price -> limit price
            AssertFilled(fill, order.Quantity, Math.Max(security.Price, order.LimitPrice));
        }

        [Test]
        public void PerformsTrailingStopLimitImmediateFillSell([Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $95. limit offset of $5, limit price $90
                ? new TrailingStopLimitOrder(Symbols.SPY, -100, 95m, 90m, 0.05m, true, 5m, Noon)
                // a trailing amount of $5 set the stop price to $95. limit offset of $5, limit price $195
                : new TrailingStopLimitOrder(Symbols.SPY, -100, 95m, 90m, 5m, false, 5m, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // Security price falls below stop price immediately
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, trailingAsPercentage ?
                100m * (1 - 0.075m) : 100m - 7.5m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Low is above limit price so fills at the low
            AssertFilled(fill, order.Quantity, security.Low);
        }

        [Test]
        public void PerformsTrailingStopLimitFillSell([Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $95. limit offset of $1, limit price $94
                ? new TrailingStopLimitOrder(Symbols.SPY, -100, 95m, 94m, 0.05m, true, 1m, Noon)
                // a trailing amount of $5 set the stop price to $95. limit offset of $1, limit price $94
                : new TrailingStopLimitOrder(Symbols.SPY, -100, 95m, 94m, 5m, false, 1m, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var initialTrailingStopPrice = order.StopPrice;
            var prevMarketPrice = 100m;
            // Market price hasn't moved
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, prevMarketPrice));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a falling security price, but not enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 97.5m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a rising security price, but still below the highest market price
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 99m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            // Simulate a rising security price, which triggers a stop price update
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have been updated to:
            //  --> (market price - trailing amount) if trailing amount is not a percentage
            //  --> (market price * (1 - trailing amount)) if trailing amount is a percentage
            // Limit price should have been updated to (new stop price - limit offset)
            Assert.AreNotEqual(initialTrailingStopPrice, order.StopPrice);
            var expectedUpdatedStopPrice = trailingAsPercentage ? security.Price * (1 - 0.05m) : security.Price - 5m;
            var expectedUpdatedLimitPrice = expectedUpdatedStopPrice - 1m;
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);
            Assert.AreEqual(expectedUpdatedLimitPrice, order.LimitPrice);

            // Simulate a falling security price, enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 92m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Stop price should have not been updated
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            // Market price is below limit price
            AssertUnfilled(fill);

            // Market price moves above limit price and order can fill
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 93m, 97m, 93m, 95.5m, 100));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Assumes worst case fill price -> limit price
            AssertFilled(fill, order.Quantity, Math.Min(security.Price, order.LimitPrice));
        }

        [TestCase(100, 101, 102)]
        [TestCase(-100, 99, 98)]
        public void TrailingStopLimitDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice,
            decimal limitPrice)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);

            var symbol = Symbols.SPY;
            var config = CreateTradeBarConfig(symbol);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The new prices are enough to trigger the stop and fill for the orders
            var tradeBar = new TradeBar(time, symbol, 100m, 102m, 98.5m, 100m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new TrailingStopLimitOrder(symbol, orderQuantity, stopPrice, limitPrice, 0.1m, true, 1m,
                time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);
            Assert.False(order.StopTriggered);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 100m, 102m, 98.5m, 100m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertFilled(fill, orderQuantity, orderQuantity < 0 ? Math.Max(security.Low, limitPrice) : Math.Min(security.High, limitPrice));
        }
        private static void AssertUnfilled(OrderEvent fill)
        {
            Assert.AreEqual(OrderStatus.None, fill.Status);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
        }

        private static void AssertFilled(OrderEvent fill, decimal expectedFillQuantity, decimal expectedFillPrice)
        {
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(expectedFillQuantity, fill.FillQuantity);
            Assert.AreEqual(expectedFillPrice, fill.FillPrice);
        }
    }
}

