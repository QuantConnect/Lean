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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public class ImmediateFillModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);
        private static TimeKeeper TimeKeeper;

        [SetUp]
        public void Setup()
        {
            TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, 100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void LimitFillExtendedMarketHours(bool isInternal, bool extendedMarketHours)
        {
            var model = new ImmediateFillModel();
            // 6 AM NewYork time, pre market
            var currentTimeNY = new DateTime(2022, 7, 19, 6, 0, 0);
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, currentTimeNY);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal, extendedMarketHours);
            var security = GetSecurity(config);
            TimeKeeper.SetUtcDateTime(currentTimeNY.ConvertToUtc(TimeZones.NewYork));
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, currentTimeNY, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(currentTimeNY, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            if (extendedMarketHours)
            {
                Assert.AreEqual(order.Quantity, fill.FillQuantity);
                Assert.AreEqual(Math.Min(order.LimitPrice, security.High), fill.FillPrice);
                Assert.AreEqual(OrderStatus.Filled, fill.Status);
            }
            else
            {
                Assert.AreEqual(0, fill.FillQuantity);
                Assert.AreEqual(0, fill.FillPrice);
                Assert.AreEqual(OrderStatus.None, fill.Status);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(order.LimitPrice, security.High), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(order.LimitPrice, security.Low), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopLimitFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopLimitOrder(Symbols.SPY, 100, 101.5m, 101.75m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 100m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.High, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopLimitFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopLimitOrder(Symbols.SPY, -100, 101.75m, 101.50m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Low, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopMarketFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopMarketOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102.5m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopMarketFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopMarketOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsTrailingStopImmediateFillBuy([Values] bool isInternal, [Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $105
                ? new TrailingStopOrder(Symbols.SPY, 100, 105m, 0.05m, true, Noon)
                // a trailing amount of $10 set the stop price to $110
                : new TrailingStopOrder(Symbols.SPY, 100, 110m, 10m, false, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // Security price rises above stop price immediately
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, trailingAsPercentage ? 100m * (1 + 0.05m) : 100m + 10m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertFilled(fill, order.Quantity, Math.Max(security.Price, order.StopPrice));
        }

        [Test]
        public void PerformsTrailingStopFillBuy([Values] bool isInternal, [Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100:
            var order = trailingAsPercentage
                // a trailing amount of 5%, stop price $105
                ? new TrailingStopOrder(Symbols.SPY, 100, 105m, 0.05m, true, Noon)
                // a trailing amount of $10 set the stop price to $110
                : new TrailingStopOrder(Symbols.SPY, 100, 110m, 10m, false, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
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
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon,
                trailingAsPercentage ? prevMarketPrice * (1 + 0.025m) : prevMarketPrice + 5m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            prevMarketPrice = security.Price;
            // Simulate a falling security price, but still above the lowest market price
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon,
                trailingAsPercentage ? prevMarketPrice * (1 + 0.0125m) : prevMarketPrice - 2.5m));

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
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon,
                trailingAsPercentage ? prevMarketPrice * (1 - 0.05m) : prevMarketPrice - 10m));

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
            Assert.AreNotEqual(initialTrailingStopPrice, order.StopPrice);
            var expectedUpdatedStopPrice = trailingAsPercentage ? security.Price * (1 + 0.05m) : security.Price + 10m;
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            // Simulate a rising security price, enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon,
                trailingAsPercentage ? order.StopPrice * (1 + 0.05m) : order.StopPrice + 10m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Stop price should have not been updated
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            AssertFilled(fill, order.Quantity, Math.Max(security.Price, order.StopPrice));
        }

        [Test]
        public void PerformsTrailingStopImmediateFillSell([Values] bool isInternal, [Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            // Assume market price is $100, with a trailing amount of $10 set the stop price to $90
            var order = trailingAsPercentage
                ? new TrailingStopOrder(Symbols.SPY, 100, 95m, 0.05m, true, Noon)
                : new TrailingStopOrder(Symbols.SPY, 100, 90m, 10m, false, Noon);

            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // Security price falls below stop price immediately
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, trailingAsPercentage ? 100m * (1 - 0.05m) : 100m - 10m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertFilled(fill, order.Quantity, Math.Min(security.Price, order.StopPrice));
        }

        [Test]
        public void PerformsTrailingStopFillSell([Values] bool isInternal, [Values] bool trailingAsPercentage)
        {
            var model = new ImmediateFillModel();
            var prevMarketPrice = 100m;
            // Initial market price $100, trailing amount of $10 set the stop price to $90
            var order = trailingAsPercentage
                ? new TrailingStopOrder(Symbols.SPY, -100, 90m, 0.1m, true, Noon)
                : new TrailingStopOrder(Symbols.SPY, -100, 90m, 10m, false, Noon);

            var initialTrailingStopPrice = order.StopPrice;

            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

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
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 95m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            // Stop price should have not been updated
            Assert.AreEqual(initialTrailingStopPrice, order.StopPrice);

            prevMarketPrice = security.Price;
            // Simulate a rising security price, but still above the lowest market price
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

            prevMarketPrice = security.Price;
            // Simulate a rising security price, which triggers a stop price update
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 105m));

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
            Assert.AreNotEqual(initialTrailingStopPrice, order.StopPrice);
            var expectedUpdatedStopPrice = trailingAsPercentage ? 105m * (1 - 0.1m) : 105m - 10m;
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            prevMarketPrice = security.Price;
            var sopTriggerMarketPrice = trailingAsPercentage ? prevMarketPrice * (1 - 0.1m) : prevMarketPrice - 10m;
            // Simulate a falling security price, enough to trigger the stop
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, sopTriggerMarketPrice));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // Stop price should have not been updated
            Assert.AreEqual(expectedUpdatedStopPrice, order.StopPrice);

            AssertFilled(fill, order.Quantity, Math.Min(security.Price, order.StopPrice));
        }

        [TestCase(100, 291.50, false)]
        [TestCase(-100, 290.50, false)]
        [TestCase(100, 291.50, true)]
        [TestCase(-100, 290.50, true)]
        public void TrailingStopOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);

            var symbol = Symbols.SPY;
            var config = CreateTradeBarConfig(symbol, isInternal);
            var security = GetSecurity(config);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            // The new prices are enough to trigger the stop for the orders
            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new TrailingStopOrder(symbol, orderQuantity, stopPrice, 0.1m, true, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertUnfilled(fill);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            AssertFilled(fill, orderQuantity, orderQuantity < 0 ? Math.Min(security.Price, stopPrice) : Math.Max(security.Price, stopPrice));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitIfTouchedFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, 100, 101.5m, 100m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = GetSecurity(configTradeBar);
            // Sets price at time zero
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 101m, 100.5m, 101m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                new Bar(101m, 101m, 100.5m, 101m), 100, // Bid bar
                new Bar(101m, 101m, 100.5m, 101m), 100) // Ask bar
            );

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => limit reached, holdings sold
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 99m, 99m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(100m, 100m, 99m, 99m), 100, // Bid bar
                    new Bar(100m, 100m, 99m, 99m), 100) // Ask bar
            );


            fill = model.LimitIfTouchedFill(security, order);

            // this fills worst case scenario
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitIfTouchedFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, -100, 101.5m, 105m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = GetSecurity(configTradeBar);

            // Sets price at time zero
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 90m, 90m, 100));
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 102m, 102m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                new Bar(101m, 102m, 100m, 100m), 100, // Bid bar
                new Bar(103m, 104m, 102m, 102m), 100) // Ask bar
            );

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => limit reached, holdings sold
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 103m, 108m, 103m, 105m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(103m, 106m, 103m, 105m), 100, // Bid bar
                    new Bar(103m, 108m, 103m, 105m), 100) // Ask bar
            );


            fill = model.LimitIfTouchedFill(security, order);

            // this fills worst case scenario
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketOnOpenUsingOpenPrice(bool isInternal)
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var model = new ImmediateFillModel();
            var order = new MarketOnOpenOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min, so this is just before market open
            time = reference.AddMinutes(29);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min
            time = reference.AddMinutes(30);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100));

            fill = model.MarketOnOpenFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Open, fill.FillPrice);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketOnCloseUsingClosingPrice(bool isInternal)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var model = new ImmediateFillModel();
            var order = new MarketOnCloseOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100, config.Increment));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100, config.Increment));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100, config.Increment));

            fill = model.MarketOnCloseFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Close, fill.FillPrice);
        }

        [TestCase(OrderDirection.Buy, true)]
        [TestCase(OrderDirection.Sell, true)]
        [TestCase(OrderDirection.Buy, false)]
        [TestCase(OrderDirection.Sell, false)]
        public void MarketOrderFillsAtBidAsk(OrderDirection direction, bool isInternal)
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, "fxcm");
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var quoteCash = new Cash(Currencies.USD, 1000, 1);
            var symbolProperties = SymbolProperties.GetDefault(Currencies.USD);
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = new Forex(exchangeHours, quoteCash, new Cash("EUR", 0, 0), config, symbolProperties, ErrorCurrencyConverter.Instance, RegisteredSecurityDataTypesProvider.Null);

            var reference = DateTime.Now;
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var brokerageModel = new FxcmBrokerageModel();
            var fillModel = brokerageModel.GetFillModel(security);

            const decimal bidPrice = 1.13739m;
            const decimal askPrice = 1.13746m;

            security.SetMarketPrice(new Tick(DateTime.Now, symbol, bidPrice, askPrice));

            var quantity = direction == OrderDirection.Buy ? 1 : -1;
            var order = new MarketOrder(symbol, quantity, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            var expected = direction == OrderDirection.Buy ? askPrice : bidPrice;
            Assert.AreEqual(expected, fill.FillPrice);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ImmediateFillModelUsesPriceForTicksWhenBidAskSpreadsAreNotAvailable(bool isInternal)
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var config = new SubscriptionDataConfig(typeof(Tick), Symbols.SPY, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));

            // Add both a tradebar and a tick to the security cache
            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new ImmediateFillModel();
            var order = new MarketOrder(symbol, 1000, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // The fill model should use the tick.Price
            Assert.AreEqual(fill.FillPrice, 100m);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ImmediateFillModelDoesNotUseTicksWhenThereIsNoTickSubscription(bool isInternal)
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            // Minute subscription
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));


            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new ImmediateFillModel();
            var order = new MarketOrder(symbol, 1000, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            // The fill model should use the tick.Price
            Assert.AreEqual(fill.FillPrice, 1.0m);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 290.50, true)]
        [TestCase(-100, 291.50, true)]
        [TestCase(100, 290.50, false)]
        [TestCase(-100, 291.50, false)]
        public void LimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal limitPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new LimitOrder(symbol, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

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

            fill = fillModel.LimitFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 291.50, false)]
        [TestCase(-100, 290.50, false)]
        [TestCase(100, 291.50, true)]
        [TestCase(-100, 290.50, true)]
        public void StopMarketOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new StopMarketOrder(symbol, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

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

            fill = fillModel.StopMarketFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(stopPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 291.50, 291.75, true)]
        [TestCase(-100, 290.50, 290.25, true)]
        [TestCase(100, 291.50, 291.75, false)]
        [TestCase(-100, 290.50, 290.25, false)]
        public void StopLimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, decimal limitPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
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

        [TestCase(true)]
        [TestCase(false)]
        public void MarketOrderFillWithStalePriceHasWarningMessage(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon.ConvertToUtc(TimeZones.NewYork).AddMinutes(61));
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.IsTrue(fill.Message.Contains("Warning: fill at stale price"));
        }

        [TestCase(OrderDirection.Sell, 11, true)]
        [TestCase(OrderDirection.Buy, 21, true)]
        // uses the trade bar last close
        [TestCase(OrderDirection.Hold, 291, true)]
        [TestCase(OrderDirection.Sell, 11, false)]
        [TestCase(OrderDirection.Buy, 21, false)]
        // uses the trade bar last close
        [TestCase(OrderDirection.Hold, 291, false)]
        public void PriceReturnsQuoteBarsIfPresent(OrderDirection orderDirection, decimal expected, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var configTradeBar = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var security = GetSecurity(configQuoteBar);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            var quoteBar = new QuoteBar(time, symbol,
                new Bar(10, 15, 5, 11),
                100,
                new Bar(20, 25, 15, 21),
                100);
            security.SetMarketPrice(quoteBar);

            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var testFillModel = new TestFillModel();
            testFillModel.SetParameters(new FillModelParameters(security,
                null,
                configProvider,
                TimeSpan.FromDays(1),
                null));

            var result = testFillModel.GetPricesPublic(security, orderDirection);

            Assert.AreEqual(expected, result.Close);
        }

        [Test]
        public void PerformsComboMarketFill(
            [Values] bool isInternal,
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection)
        {
            var model = new ImmediateFillModel();
            var groupOrderManager = new GroupOrderManager(0, 2, orderDirection == OrderDirection.Buy ? 10 : -10);
            var spyOrder = new ComboMarketOrder(
                Symbols.SPY,
                10m.GetOrderLegGroupQuantity(groupOrderManager),
                Noon,
                groupOrderManager)
            { Id = 1 };
            var aaplOrder = new ComboMarketOrder(
                Symbols.AAPL,
                5m.GetOrderLegGroupQuantity(groupOrderManager),
                Noon,
                groupOrderManager)
            { Id = 2 };

            groupOrderManager.OrderIds.Add(spyOrder.Id);
            groupOrderManager.OrderIds.Add(aaplOrder.Id);

            var spyConfig = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var aaplConfig = CreateTradeBarConfig(Symbols.AAPL, isInternal);
            var spy = GetSecurity(spyConfig);
            var aapl = GetSecurity(aaplConfig);
            spy.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            spy.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));
            aapl.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            aapl.SetMarketPrice(new IndicatorDataPoint(Symbols.AAPL, Noon, 55.456m));

            Assert.AreEqual(orderDirection, groupOrderManager.Direction);

            var securitiesForOrders = new Dictionary<Order, Security>
            {
                { spyOrder, spy },
                { aaplOrder, aapl }
            };

            var fill = model.Fill(new FillModelParameters(
                spy,
                spyOrder,
                new MockSubscriptionDataConfigProvider(spyConfig),
                Time.OneHour,
                securitiesForOrders));

            Assert.AreEqual(2, fill.Count());

            var spyFillEvent = fill.First();

            Assert.AreEqual(spyOrder.Quantity, spyFillEvent.FillQuantity);
            Assert.AreEqual(spy.Price, spyFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, spyFillEvent.Status);

            var aaplFillEvent = fill.Last();

            Assert.AreEqual(aaplOrder.Quantity, aaplFillEvent.FillQuantity);
            Assert.AreEqual(aapl.Price, aaplFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, aaplFillEvent.Status);
        }

        [Test]
        public void PerformsComboLimitFill(
            [Values] bool isInternal,
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection,
            [Values] bool debit)
        {
            var spyConfig = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var aaplConfig = CreateTradeBarConfig(Symbols.AAPL, isInternal);
            var spy = GetSecurity(spyConfig);
            var aapl = GetSecurity(aaplConfig);
            spy.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            spy.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 301m, 302m, 299m, 300m, 10));
            aapl.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            aapl.SetMarketPrice(new TradeBar(Noon, Symbols.AAPL, 101m, 102m, 99m, 100m, 25));

            var groupOrderManager = new GroupOrderManager(0, 2, orderDirection == OrderDirection.Buy ? 10 : -10, 0m);
            Assert.AreEqual(orderDirection, groupOrderManager.Direction);

            var spyLegOrder = new ComboLimitOrder(
                Symbols.SPY,
                -100m.GetOrderLegGroupQuantity(groupOrderManager),
                0m,
                Noon,
                groupOrderManager);
            var aaplLegOrder = new ComboLimitOrder(
                Symbols.AAPL,
                100m.GetOrderLegGroupQuantity(groupOrderManager),
                0m,
                Noon,
                groupOrderManager);
            var legsOrders = new List<ComboLimitOrder>() { spyLegOrder, aaplLegOrder };
            for (var i = 0; i < legsOrders.Count; i++)
            {
                legsOrders[i].Id = i + 1;
                if (debit)
                {
                    legsOrders[i].Quantity *= -1;
                }

                groupOrderManager.OrderIds.Add(legsOrders[i].Id);
            }

            var securitiesForOrders = new Dictionary<Order, Security>
            {
                { spyLegOrder, spy },
                { aaplLegOrder, aapl }
            };

            var getLegsPrice = (Func<Security, decimal> priceSelector) =>
                priceSelector(spy) * spyLegOrder.Quantity.GetOrderLegRatio(groupOrderManager) / 100 +
                priceSelector(aapl) * aaplLegOrder.Quantity.GetOrderLegRatio(groupOrderManager) / 100;

            // set limit prices that won't fill.
            // combo limit orders fill based on the total price that will be paid/received for the legs
            if (orderDirection == OrderDirection.Buy)
            {
                // limit price lower than legs price
                var price = getLegsPrice((security) => security.Low);
                var multiplier = price > 0 ? 0.999m : 1.001m;
                groupOrderManager.LimitPrice = price * multiplier;
            }
            else
            {
                // limit price higher than legs price
                var price = getLegsPrice((security) => security.High);
                var multiplier = price > 0 ? 1.001m : 0.999m;
                groupOrderManager.LimitPrice = price * multiplier;
            }

            var model = new ImmediateFillModel();

            var fill = model.Fill(new FillModelParameters(spy,
                spyLegOrder,
                new MockSubscriptionDataConfigProvider(spyConfig),
                Time.OneHour,
                securitiesForOrders));
            // won't fill with the given limit price
            Assert.IsEmpty(fill);

            // set limit prices that will fill
            if (orderDirection == OrderDirection.Buy)
            {
                var price = getLegsPrice((security) => security.Low);
                var multiplier = price > 0 ? 1.001m : 0.999m;
                groupOrderManager.LimitPrice = price * multiplier;
            }
            else
            {
                var price = getLegsPrice((security) => security.High);
                var multiplier = price > 0 ? 0.999m : 1.001m;
                groupOrderManager.LimitPrice = price * multiplier;
            }

            fill = model.Fill(new FillModelParameters(spy,
                spyLegOrder,
                new MockSubscriptionDataConfigProvider(spyConfig),
                Time.OneHour,
                securitiesForOrders));
            Assert.AreEqual(legsOrders.Count, fill.Count());

            var spyFillEvent = fill.First();

            Assert.AreEqual(spyLegOrder.Quantity, spyFillEvent.FillQuantity);
            var expectedSpyFillPrice = orderDirection == OrderDirection.Buy ? spy.Low : spy.High;
            Assert.AreEqual(expectedSpyFillPrice, spyFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, spyFillEvent.Status);

            var aaplFillEvent = fill.Last();

            Assert.AreEqual(aaplLegOrder.Quantity, aaplFillEvent.FillQuantity);
            var expectedAaplFillPrice = orderDirection == OrderDirection.Buy ? aapl.Low : aapl.High;
            Assert.AreEqual(expectedAaplFillPrice, aaplFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, aaplFillEvent.Status);
        }

        [Test]
        public void PerformsComboLegLimitFill(
            [Values] bool isInternal,
            [Values(OrderDirection.Buy, OrderDirection.Sell)] OrderDirection orderDirection)
        {
            var model = new ImmediateFillModel();
            var multiplier = orderDirection == OrderDirection.Buy ? 1 : -1;
            var groupOrderManager = new GroupOrderManager(0, 2, multiplier * 10, 1m);

            var spyLimitPrice = orderDirection == OrderDirection.Buy ? 101.1m : 102m;
            var spyOrder = new ComboLegLimitOrder(
                Symbols.SPY,
                10m.GetOrderLegGroupQuantity(groupOrderManager),
                spyLimitPrice,
                Noon,
                groupOrderManager)
            { Id = 1 };
            var aaplLimitPrice = orderDirection == OrderDirection.Buy ? 252.5m : 251.1m;
            var aaplOrder = new ComboLegLimitOrder(
                Symbols.AAPL,
                multiplier * 5m.GetOrderLegGroupQuantity(groupOrderManager),
                aaplLimitPrice,
                Noon,
                groupOrderManager)
            { Id = 2 };

            groupOrderManager.OrderIds.Add(spyOrder.Id);
            groupOrderManager.OrderIds.Add(aaplOrder.Id);

            Assert.AreEqual(orderDirection, groupOrderManager.Direction);

            var spyConfig = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var aaplConfig = CreateTradeBarConfig(Symbols.AAPL, isInternal);
            var spy = GetSecurity(spyConfig);
            var aapl = GetSecurity(aaplConfig);
            spy.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            spy.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));
            aapl.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            aapl.SetMarketPrice(new IndicatorDataPoint(Symbols.AAPL, Noon, 252.456m));

            var securitiesForOrders = new Dictionary<Order, Security>
            {
                { spyOrder, spy },
                { aaplOrder, aapl }
            };

            var fill = model.Fill(new FillModelParameters(
                spy,
                spyOrder,
                new MockSubscriptionDataConfigProvider(spyConfig),
                Time.OneHour,
                securitiesForOrders));

            // Won't fill, the limit price condition is not met
            Assert.IsEmpty(fill);

            spy.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));
            aapl.SetMarketPrice(new TradeBar(Noon, Symbols.AAPL, 252m, 253m, 251m, 252.3m, 250));

            fill = model.Fill(new FillModelParameters(
                spy,
                spyOrder,
                new MockSubscriptionDataConfigProvider(spyConfig),
                Time.OneHour,
                securitiesForOrders));

            Assert.AreEqual(2, fill.Count());

            var spyFillEvent = fill.First();

            Assert.AreEqual(spyOrder.Quantity, spyFillEvent.FillQuantity);
            var expectedSpyFillPrice = orderDirection == OrderDirection.Buy
                ? Math.Min(spyOrder.LimitPrice, spy.High)
                : Math.Max(spyOrder.LimitPrice, spy.Low);
            Assert.AreEqual(expectedSpyFillPrice, spyFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, spyFillEvent.Status);

            var aaplFillEvent = fill.Last();

            Assert.AreEqual(aaplOrder.Quantity, aaplFillEvent.FillQuantity);
            var expectedAaplFillPrice = orderDirection == OrderDirection.Buy
                ? Math.Min(aaplOrder.LimitPrice, aapl.High)
                : Math.Max(aaplOrder.LimitPrice, aapl.Low);
            Assert.AreEqual(expectedAaplFillPrice, aaplFillEvent.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, aaplFillEvent.Status);
        }

        [TestCase(Resolution.Tick, false)]
        [TestCase(Resolution.Second, false)]
        [TestCase(Resolution.Minute, false)]
        [TestCase(Resolution.Hour, false)]
        [TestCase(Resolution.Daily, true)]
        public void PerformFillOutsideRegularAndExtendedHours(Resolution resolution, bool shouldFill)
        {
            var config = CreateTradeBarConfig(Symbols.SPY, resolution: resolution);
            var configProvider = new MockSubscriptionDataConfigProvider(config);
            configProvider.SubscriptionDataConfigs.Add(config);
            var security = GetSecurity(config);
            security.SetFillModel(new ImmediateFillModel());

            var baseTime = resolution == Resolution.Daily ? new DateTime(2014, 6, 25) : new DateTime(2014, 6, 24, 12, 0, 0);
            var orderTime = baseTime.ConvertToUtc(security.Exchange.TimeZone);
            var resolutionTimeSpan = resolution.ToTimeSpan();
            var tradeBarTime = baseTime.Subtract(resolutionTimeSpan);

            var model = (ImmediateFillModel)security.FillModel;
            var order = new MarketOrder(Symbols.SPY, 100, orderTime);

            var parameters = new FillModelParameters(security, order, configProvider, Time.OneHour, null);

            var timeKeeper = TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            // midnight, shouldn't be able to fill for resolutions < daily
            timeKeeper.UpdateTime(new DateTime(2014, 6, 25).ConvertToUtc(TimeZones.NewYork));
            security.SetLocalTimeKeeper(timeKeeper);

            const decimal close = 101.234m;
            security.SetMarketPrice(new TradeBar(tradeBarTime, Symbols.SPY, 101.123m, 101.123m, 101.123m, close, 100, resolutionTimeSpan));

            var fill = model.Fill(parameters).Single();

            if (shouldFill)
            {
                Assert.AreEqual(OrderStatus.Filled, fill.Status);
                Assert.AreEqual(order.Quantity, fill.FillQuantity);
                Assert.AreEqual(close, fill.FillPrice);
            }
            else
            {
                Assert.AreNotEqual(OrderStatus.Filled, fill.Status);
                Assert.AreNotEqual(OrderStatus.PartiallyFilled, fill.Status);
                Assert.AreEqual(0, fill.FillQuantity);
                Assert.AreEqual(0, fill.FillPrice);
            }
        }

        [Test]
        public void ImmediateFillModelFillsMOCAtOrAfterMarketCloseTime()
        {
            var model = new ImmediateFillModel();
            var config = CreateTradeBarConfig(Symbols.SPY);
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(config.Symbol.ID.Market, config.Symbol, config.SecurityType);
            var security = new Security(
                entry.ExchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var timeOffset = TimeSpan.FromSeconds(1);
            var time = new DateTime(2025, 7, 8, 16, 0, 0);
            // Set LocalTime to slightly after market close
            var localTime = time + timeOffset - TimeSpan.FromTicks(1);
            // Submit MOC order an hour before close
            var order = new MarketOnCloseOrder(Symbols.SPY, -100, time.AddMinutes(-60));
            var utcTime = localTime.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(utcTime, new[] { TimeZones.NewYork });
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            // Seed last regular bar
            security.SetMarketPrice(new TradeBar(time - timeOffset, Symbols.SPY, 101.123m, 101.123m, 101.123m, 100, 100, timeOffset));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(OrderStatus.Filled, fill.Status);
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

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol, bool isInternal = false, bool extendedMarketHours = true,
            Resolution resolution = Resolution.Minute)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, TimeZones.NewYork, TimeZones.NewYork, true, extendedMarketHours, isInternal);
        }

        private Security GetSecurity(SubscriptionDataConfig config)
        {
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(config.Symbol.ID.Market, config.Symbol, config.SecurityType);
            var security = new Security(
                entry.ExchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            return security;
        }

        private class TestFillModel : FillModel
        {
            public void SetParameters(FillModelParameters parameters)
            {
                Parameters = parameters;
            }

            public Prices GetPricesPublic(Security asset, OrderDirection direction)
            {
                return base.GetPrices(asset, direction);
            }
        }
    }
}
