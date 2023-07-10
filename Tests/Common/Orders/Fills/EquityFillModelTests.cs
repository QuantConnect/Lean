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
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Securities.Equity;
using System.Linq;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public partial class EquityFillModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);
        private TimeKeeper TimeKeeper;

        [SetUp]
        public void Setup()
        {
            TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
        }

        [TestCase(11, 11,  11, "")]
        [TestCase(12, 11, 11,"")]
        [TestCase(12, 10, 11, "Warning: No quote information")]
        [TestCase(12, 10, 10, "Warning: fill at stale price")]
        public void PerformsMarketFillBuy(int orderHour, int quoteBarHour, int tradeBarHour, string message)
        {
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var equity = CreateEquity(configTradeBar);

            var orderTime = new DateTime(2014, 6, 24, orderHour, 0, 0).ConvertToUtc(equity.Exchange.TimeZone);
            var quoteBarTime = new DateTime(2014, 6, 24, quoteBarHour, 0, 0).AddMinutes(-1);
            var tradeBarTime = new DateTime(2014, 6, 24, tradeBarHour, 0, 0).AddMinutes(-1);

            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOrder(Symbols.SPY, 100, orderTime);

            var parameters = new FillModelParameters(equity, order, configProvider, Time.OneHour, null);

            // Sets price at time zero
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            equity.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            // IndicatorDataPoint is not market data
            Assert.Throws<InvalidOperationException>(() => model.Fill(parameters),
                $"Cannot get ask price to perform fill for {equity.Symbol} because no market data subscription were found.");

            const decimal close = 101.234m;
            var bidBar = new Bar(101.123m, 101.123m, 101.123m, 101.123m);
            var askBar = new Bar(101.234m, 101.234m, 101.234m, close);
            var tradeBar = new TradeBar(tradeBarTime, Symbols.SPY, 101.123m, 101.123m, 101.123m, close, 100);
            equity.SetMarketPrice(new QuoteBar(quoteBarTime, Symbols.SPY, bidBar, 0, askBar, 0));
            equity.SetMarketPrice(tradeBar);

            var fill = model.Fill(parameters).Single();
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(close, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.IsTrue(fill.Message.StartsWith(message, StringComparison.InvariantCultureIgnoreCase));
        }

        [TestCase(11, 11, 11, "")]
        [TestCase(12, 11, 11, "")]
        [TestCase(12, 10, 11, "Warning: No quote information")]
        [TestCase(12, 10, 10, "Warning: fill at stale price")]
        public void PerformsMarketFillSell(int orderHour, int quoteBarHour, int tradeBarHour, string message)
        {
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var equity = CreateEquity(configTradeBar);

            var orderTime = new DateTime(2014, 6, 24, orderHour, 0, 0).ConvertToUtc(equity.Exchange.TimeZone);
            var quoteBarTime = new DateTime(2014, 6, 24, quoteBarHour, 0, 0).AddMinutes(-1);
            var tradeBarTime = new DateTime(2014, 6, 24, tradeBarHour, 0, 0).AddMinutes(-1);

            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOrder(Symbols.SPY, -100, orderTime);

            var parameters = new FillModelParameters(equity, order, configProvider, Time.OneHour, null);

            // Sets price at time zero
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            equity.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            // IndicatorDataPoint is not market data
            Assert.Throws<InvalidOperationException>(() => model.Fill(parameters),
                $"Cannot get bid price to perform fill for {equity.Symbol} because no market data subscription were found.");

            const decimal close = 101.123m;
            var bidBar = new Bar(101.123m, 101.123m, 101.123m, close);
            var askBar = new Bar(101.234m, 101.234m, 101.234m, 101.234m);
            var tradeBar = new TradeBar(tradeBarTime, Symbols.SPY, 101.234m, 101.234m, 101.234m, close, 100);
            equity.SetMarketPrice(new QuoteBar(quoteBarTime, Symbols.SPY, bidBar, 0, askBar, 0));
            equity.SetMarketPrice(tradeBar);

            var fill = model.Fill(parameters).Single();
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(close, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.IsTrue(fill.Message.StartsWith(message, StringComparison.InvariantCultureIgnoreCase));
        }

        [Test]
        public void PerformsStopLimitFillBuy()
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, 100, 101.5m, 101.75m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
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

        [Test]
        public void PerformsStopLimitFillSell()
        {
            var model = new EquityFillModel();
            var order = new StopLimitOrder(Symbols.SPY, -100, 101.75m, 101.50m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
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

        [Test]
        public void PerformsLimitIfTouchedSell()
        {
            var model = new EquityFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, -100, 101.5m, 105m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                configTradeBar,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

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

            // Time jump => limit reached, security bought
            // |---> First, ensure that price data are not used to fill
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 99m, 99m, 100));
            fill = model.LimitIfTouchedFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // |---> Lastly, ensure that quote data used to fill
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(105m, 105m, 105m, 105m), 100, // Bid bar
                    new Bar(105m, 105m, 105m, 105m), 100) // Ask bar
            );
            fill = model.LimitIfTouchedFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [Test]
        public void PerformsLimitIfTouchedBuy()
        {
            var model = new EquityFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, 100, 101.5m, 100m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                configTradeBar,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

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

            // Time jump => limit reached, security bought
            // |---> First, ensure that price data are not used to fill
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 99m, 99m, 100));
            fill = model.LimitIfTouchedFill(security, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // |---> Lastly, ensure that quote data used to fill
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY,
                    new Bar(100m, 100m, 100m, 100m), 100, // Bid bar
                    new Bar(100m, 100m, 100m, 100m), 100) // Ask bar
            );
            fill = model.LimitIfTouchedFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }


        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnOpenUsingOpenPriceWithMinuteSubscription(int quantity)
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY);
            var equity = CreateEquity(configTradeBar);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnOpenOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100));

            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min, so this is just before market open
            time = reference.AddMinutes(29);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100));

            fill = model.Fill(new FillModelParameters(
                equity,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            const decimal expected = 1.45m;
            // market opens after 30min
            time = reference.AddMinutes(30);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // Does not fill with quote data
            equity.SetMarketPrice(new QuoteBar(time, Symbols.SPY,
                new Bar(1.45m, 1.99m, 1.09m, 1.39m), 100,
                new Bar(1.46m, 2.01m, 1.11m, 1.41m), 100));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // Fill with trade bar
            equity.SetMarketPrice(new TradeBar(time, Symbols.SPY, expected, 2.0m, 1.1m, 1.40m, 100));
            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }

        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnOpenUsingOpenPriceWithDailySubscription(int quantity)
        {
            Func<DateTime, decimal, TradeBar> getTradeBar = (t, o) => new TradeBar(t.RoundDown(Time.OneDay),
                Symbols.SPY, o, 2m, 0.5m, 1.33m, 100, Time.OneDay);

            var reference = new DateTime(2015, 06, 05, 12, 0, 0); // market is open
            var config = CreateTradeBarConfig(Symbols.SPY, Resolution.Daily);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnOpenOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(getTradeBar(time, 2m));

            // Will not fill because the order was placed before the bar is closed
            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            // It will not fill in the next morning because needs to wait for day to close
            const decimal expected = 1m;
            time = equity.Exchange.Hours.GetNextMarketOpen(time, false);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // Fill once the equity is updated with the day bar
            equity.SetMarketPrice(getTradeBar(time, expected));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }
        // Official open
        [TestCase(-100, TradeConditionFlags.OfficialOpen)]
        [TestCase(100, TradeConditionFlags.OfficialOpen)]
        // Opening prints
        [TestCase(-100, TradeConditionFlags.OpeningPrints)]
        [TestCase(100, TradeConditionFlags.OpeningPrints)]
        // Official open and regular
        [TestCase(-100, TradeConditionFlags.OfficialOpen | TradeConditionFlags.Regular)]
        [TestCase(100, TradeConditionFlags.OfficialOpen | TradeConditionFlags.Regular)]
        // Opening prints and regular
        [TestCase(-100, TradeConditionFlags.OpeningPrints | TradeConditionFlags.Regular)]
        [TestCase(100, TradeConditionFlags.OpeningPrints | TradeConditionFlags.Regular)]
        // Any other random combination of flags that include OfficialOpen
        [TestCase(-100,
            TradeConditionFlags.OfficialOpen | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100,
            TradeConditionFlags.OfficialOpen | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        // Any other random combination of flags that include OpeningPrints
        [TestCase(-100,
            TradeConditionFlags.OpeningPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100,
            TradeConditionFlags.OpeningPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        public void PerformsMarketOnOpenUsingOpenPriceWithTickSubscription(int quantity, long numericSaleCondition)
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var config = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnOpenOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var saleCondition = Convert.ToString(numericSaleCondition, 16);

            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, 1m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min, so this is just before market open
            time = reference.AddMinutes(29);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, 1m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            const decimal expected = 1m;
            // market opens after 30min
            time = reference.AddMinutes(30);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // The quote is received after the market is open, but the trade is not
            equity.Update(new List<Tick>
            {
                new Tick(time.AddMinutes(-1), Symbols.SPY, saleCondition, "P", 100, expected),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // The trade is received after the market is open, but it is not have a official open flag
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, "", "P", 100, expected),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual("No trade with the OfficialOpen or OpeningPrints flag within the 1-minute timeout.", fill.Message);

            // One quote and some trades with different conditions are received after the market is open,
            // but there is trade prior to that with different price
            equity.Update(new List<Tick>
            {
                new Tick(time.AddMinutes(-1), Symbols.SPY,  "80000001", "P", 100, 0.9m),   // Not Open
                new Tick(time, Symbols.SPY, saleCondition, "Q", 100, 0.95m),            // Open but not primary exchange
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, expected),         // Fill with this tick
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.95m),   // Open but not primary exchange
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }

        [TestCase(-100, 0.9)]
        [TestCase(100, 1.1)]
        public void PerformsMarketOnOpenUsingOpenPriceWithTickSubscriptionButNoSalesCondition(int quantity, decimal expected)
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var config = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnOpenOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // No Sales Condition
            var saleCondition = "";

            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, "80000001", "P", 100, 1m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            const decimal price = 1m;
            // market opens after 30min. 1 minute after open to accept the last trade
            time = reference.AddMinutes(32);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.9m),   // Not Close
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, 0.95m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.95m),   // Open but not primary exchange
                new Tick(time, Symbols.SPY, saleCondition, "Q", 100, price),         // Fill with this tick
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(price, fill.FillPrice);

            // Test whether it fills on the bid/ask if there is no trade
            equity.Cache.Reset();
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
            }, typeof(Tick));

            fill = model.MarketOnOpenFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
            Assert.IsTrue(fill.Message.Contains("Fill with last Quote data.", StringComparison.InvariantCulture));
        }

        [TestCase(Resolution.Minute, 3, 17, 0, 9, 29)]
        [TestCase(Resolution.Minute, 4, 0, 0, 9, 29)]
        [TestCase(Resolution.Minute, 4, 8, 0, 9, 29)]
        [TestCase(Resolution.Minute, 4, 9, 30, 9, 29, true)]
        [TestCase(Resolution.Minute, 4, 9, 30, 9, 31, true)]
        [TestCase(Resolution.Hour, 3, 17, 0, 8, 29)]
        [TestCase(Resolution.Hour, 4, 0, 0, 8, 0)]
        [TestCase(Resolution.Hour, 4, 8, 0, 8, 0)]
        [TestCase(Resolution.Hour, 4, 9, 30, 8, 0, true)]
        [TestCase(Resolution.Hour, 4, 9, 30, 11, 0, true)]
        [TestCase(Resolution.Daily, 3, 17, 0, 8, 0)]
        [TestCase(Resolution.Daily, 4, 0, 0, 8, 0)]
        [TestCase(Resolution.Daily, 4, 8, 0, 8, 0)]
        [TestCase(Resolution.Daily, 4, 9, 30, 8, 0)]
        public void PerformsMarketOnOpenUsingOpenPriceWithDifferentOrderSubmissionDateTime(Resolution resolution, int day, int hour, int minute, int ref_hour, int ref_minute, bool nextDay = false)
        {
            var period = resolution.ToTimeSpan();
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY, resolution);
            var equity = CreateEquity(configTradeBar);
            var model = (EquityFillModel)equity.FillModel;

            var orderTime = new DateTime(2015, 6, day, hour, minute, 0).ConvertToUtc(TimeZones.NewYork);
            var order = new MarketOnOpenOrder(Symbols.SPY, 100, orderTime);

            var reference = new DateTime(2015, 6, 4, ref_hour, ref_minute, 0).RoundDown(period);

            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100, period));

            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                configProvider,
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);


            const decimal expected = 1.45m;

            var tradeBar = new TradeBar(reference.Add(period), Symbols.SPY, expected, 2.75m, 1.15m, 1.45m, 100, period);
            equity.SetMarketPrice(tradeBar);
            TimeKeeper.SetUtcDateTime(tradeBar.EndTime.ConvertToUtc(TimeZones.NewYork));

            fill = model.MarketOnOpenFill(equity, order);

            // Special case when the order exactly when the market opens.
            // Should only fill on the next day
            if (nextDay)
            {
                Assert.AreEqual(0, fill.FillQuantity);
                return;
            }

            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }

        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnCloseUsingClosingPriceWithDailySubscription(int quantity)
        {
            Func<DateTime, decimal, TradeBar> getTradeBar = (t, c) => new TradeBar(t.RoundDown(Time.OneDay * 2),
                Symbols.SPY, 1.33m, 2m, 0.5m, c, 100, Time.OneDay);

            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var config = CreateTradeBarConfig(Symbols.SPY, Resolution.Daily);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnCloseOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(getTradeBar(time, 2m));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(getTradeBar(time, 1.45m));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes
            const decimal expected = 1.40m;
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(getTradeBar(time, expected));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }

        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnCloseUsingClosingPriceWithMinuteTradeBarSubscription(int quantity)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var config = CreateTradeBarConfig(Symbols.SPY);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnCloseOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100, config.Increment));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100, config.Increment));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100, config.Increment));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(equity.Close, fill.FillPrice);
        }

        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnCloseUsingClosingPriceWithMinuteQuoteBarSubscription(int quantity)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var config = CreateQuoteBarConfig(Symbols.SPY);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnCloseOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new QuoteBar(time, Symbols.SPY,
                new Bar(1.45m, 1.99m, 1.09m, 1.39m), 100,
                new Bar(1.46m, 2.01m, 1.11m, 1.41m), 100, config.Increment));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new QuoteBar(time - config.Increment, Symbols.SPY,
                new Bar(1.45m, 1.99m, 1.09m, 1.39m), 100,
                new Bar(1.46m, 2.01m, 1.11m, 1.41m), 100, config.Increment));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            const decimal expected = 1.4m;

            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100, config.Increment));
            equity.SetMarketPrice(new QuoteBar(time, Symbols.SPY,
                new Bar(1.45m, 1.99m, 1.09m, expected), 100,
                new Bar(1.46m, 2.01m, 1.11m, expected), 100, config.Increment));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
            Assert.IsTrue(fill.Message.StartsWith("Warning: No trade information available", StringComparison.InvariantCulture));
        }

        // Official close
        [TestCase(-100, true, TradeConditionFlags.OfficialClose)]
        [TestCase(100, true, TradeConditionFlags.OfficialClose)]
        [TestCase(-100, false, TradeConditionFlags.OfficialClose)]
        [TestCase(100, false, TradeConditionFlags.OfficialClose)]
        // Closing prints
        [TestCase(-100, true, TradeConditionFlags.ClosingPrints)]
        [TestCase(100, true, TradeConditionFlags.ClosingPrints)]
        [TestCase(-100, false, TradeConditionFlags.ClosingPrints)]
        [TestCase(100, false, TradeConditionFlags.ClosingPrints)]
        // Official close and regular
        [TestCase(-100, true, TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular)]
        [TestCase(100, true, TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular)]
        [TestCase(-100, false, TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular)]
        [TestCase(100, false, TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular)]
        // Closing prints and regular
        [TestCase(-100, true, TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular)]
        [TestCase(100, true, TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular)]
        [TestCase(-100, false, TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular)]
        [TestCase(100, false, TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular)]
        // Any other random combination of flags that include OfficialClose
        [TestCase(-100, true,
            TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100, true,
            TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(-100, false,
            TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100, false,
            TradeConditionFlags.OfficialClose | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        // Any other random combination of flags that include ClosingPrints
        [TestCase(-100, true,
            TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100, true,
            TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(-100, false,
            TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        [TestCase(100, false,
            TradeConditionFlags.ClosingPrints | TradeConditionFlags.Regular | TradeConditionFlags.Cash | TradeConditionFlags.Cross |
            TradeConditionFlags.DerivativelyPriced)]
        public void PerformsMarketOnCloseUsingClosingPriceWithTickSubscription(int quantity, bool extendedHours, long numericSaleCondition)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var config = CreateTickConfig(Symbols.SPY, extendedHours: extendedHours);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnCloseOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var saleCondition = Convert.ToString(numericSaleCondition, 16);

            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, "80000001", "P", 100, 1m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                // It should fill with this tick based on sales condition but the market is still open
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, 1)
            }, typeof(Tick));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);

            const decimal expected = 1m;
            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.9m),
                new Tick(time, Symbols.SPY, "80000001", "Q", 100, 0.95m),
                new Tick(time, Symbols.SPY, "80000001", "P", 100, 0.98m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, expected),
            }, typeof(Tick));

            // If the subscriptions doesn't include extended hours, fills with the last tick
            if (!extendedHours)
            {
                fill = model.MarketOnCloseFill(equity, order);
                Assert.AreEqual(order.Quantity, fill.FillQuantity);
                Assert.AreEqual(expected, fill.FillPrice);
                Assert.AreEqual("No trade with the OfficialClose or ClosingPrints flag for data that does not include extended market hours. Fill with last Trade data.", fill.Message);
                return;
            }

            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.9m),   // Not Close
                new Tick(time, Symbols.SPY, saleCondition, "Q", 100, 0.95m),            // Close but not primary exchange
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, expected),         // Fill with this tick
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.95m),
            }, typeof(Tick));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
        }

        [TestCase(-100)]
        [TestCase(100)]
        public void PerformsMarketOnCloseUsingClosingPriceWithTickSubscriptionButNoSalesCondition(int quantity)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var config = CreateTickConfig(Symbols.SPY);
            var equity = CreateEquity(config);
            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOnCloseOrder(Symbols.SPY, quantity, reference);
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            // No Sales Condition
            var saleCondition = "";

            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY, "80000001", "P", 100, 1m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m)
            }, typeof(Tick));

            var fill = model.Fill(new FillModelParameters(
                equity,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.AreEqual(0, fill.FillQuantity);

            const decimal expected = 1m;
            // market closes
            time = reference.AddMinutes(60).AddMilliseconds(100);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.9m),   // Not Close
                new Tick(time, Symbols.SPY, saleCondition, "Q", 100, 0.95m),            // Close but not primary exchange
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, expected),         // Fill with this tick
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.95m),   // Open but not primary exchange
            }, typeof(Tick));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual("No trade with the OfficialClose or ClosingPrints flag within the 1-minute timeout.", fill.Message);

            // 2 minutes after the close
            time = reference.AddMinutes(62);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            equity.Update(new List<Tick>
            {
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.9m),   // Not Close
                new Tick(time, Symbols.SPY, saleCondition, "P", 100, 0.95m),
                new Tick(time, Symbols.SPY, 1m, 0.9m, 1.1m),
                new Tick(time, Symbols.SPY,  "80000001", "P", 100, 0.95m),   // Open but not primary exchange
                new Tick(time, Symbols.SPY, saleCondition, "Q", 100, expected),         // Fill with this tick
            }, typeof(Tick));

            fill = model.MarketOnCloseFill(equity, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(expected, fill.FillPrice);
            Assert.IsTrue(fill.Message.Contains("Fill with last Trade data.", StringComparison.InvariantCulture));
        }

        [TestCase(OrderDirection.Buy)]
        [TestCase(OrderDirection.Sell)]
        public void MarketOrderFillsAtBidAsk(OrderDirection direction)
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, "fxcm");
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var quoteCash = new Cash(Currencies.USD, 1000, 1);
            var symbolProperties = SymbolProperties.GetDefault(Currencies.USD);
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
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

        [Test]
        public void EquityFillModelUsesPriceForTicksWhenBidAskSpreadsAreNotAvailable()
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var config = new SubscriptionDataConfig(typeof(Tick), Symbols.SPY, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
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
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));

            // Add both a tradebar and a tick to the security cache
            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new EquityFillModel();
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

        [Test]
        public void EquityFillModelDoesNotUseTicksWhenThereIsNoTickSubscription()
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            // Minute subscription
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
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
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));


            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new EquityFillModel();
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

        [Test]
        public void MarketOrderFillWithStalePriceHasWarningMessage()
        {
            var model = new EquityFillModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon.ConvertToUtc(TimeZones.NewYork).AddMinutes(61));
            var config = CreateTickConfig(Symbols.SPY);
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new Tick(Noon, Symbols.SPY, 101.123m, 101.456m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour,
                null)).Single();

            Assert.IsTrue(fill.Message.Contains("Warning: fill at stale price"));
        }

        [TestCase(OrderDirection.Sell, 11)]
        [TestCase(OrderDirection.Buy, 21)]
        // uses the trade bar last close
        [TestCase(OrderDirection.Hold, 291)]
        public void PriceReturnsQuoteBarsIfPresent(OrderDirection orderDirection, decimal expected)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var configTradeBar = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var security = new Security(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                configQuoteBar,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
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
            var equity = CreateEquity(config);

            var baseTime = resolution == Resolution.Daily ? new DateTime(2014, 6, 25) : new DateTime(2014, 6, 24, 12, 0, 0);
            var orderTime = baseTime.ConvertToUtc(equity.Exchange.TimeZone);
            var resolutionTimeSpan = resolution.ToTimeSpan();
            var tradeBarTime = baseTime.Subtract(resolutionTimeSpan);

            var model = (EquityFillModel)equity.FillModel;
            var order = new MarketOrder(Symbols.SPY, 100, orderTime);

            var parameters = new FillModelParameters(equity, order, configProvider, Time.OneHour, null);

            var timeKeeper = TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            // midnight, shouldn't be able to fill for resolutions < daily
            timeKeeper.UpdateTime(new DateTime(2014, 6, 25).ConvertToUtc(TimeZones.NewYork));
            equity.SetLocalTimeKeeper(timeKeeper);

            const decimal close = 101.234m;
            equity.SetMarketPrice(new TradeBar(tradeBarTime, Symbols.SPY, 101.123m, 101.123m, 101.123m, close, 100, resolutionTimeSpan));

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

        private Equity CreateEquity(SubscriptionDataConfig config)
        {
            var equity = new Equity(
                SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                Exchange.ARCA
            );
            equity.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            return equity;
        }

        private SubscriptionDataConfig CreateTickConfig(Symbol symbol, bool extendedHours = true)
        {
            return new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, extendedHours, false);
        }

        private SubscriptionDataConfig CreateQuoteBarConfig(Symbol symbol, Resolution resolution = Resolution.Minute, bool extendedHours = true)
        {
            return new SubscriptionDataConfig(typeof(QuoteBar), symbol, resolution, TimeZones.NewYork, TimeZones.NewYork, true, extendedHours, false);
        }

        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol, Resolution resolution = Resolution.Minute, bool extendedHours = true)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, TimeZones.NewYork, TimeZones.NewYork, true, extendedHours, false);
        }

        private FillModelParameters GetFillModelParameters(Order order)
        {
            var configTradeBar = CreateTradeBarConfig(order.Symbol);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);
            var security = CreateEquity(configTradeBar);

            return new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour,
                null);
        }

        private class TestFillModel : EquityFillModel
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
