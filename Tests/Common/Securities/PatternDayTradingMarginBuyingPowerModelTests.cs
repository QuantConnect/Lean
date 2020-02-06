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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class PatternDayTradingMarginBuyingPowerModelTests
    {
        // Test class to enable calling protected methods
        public class TestPatternDayTradingMarginModel : PatternDayTradingMarginModel
        {
            public TestPatternDayTradingMarginModel()
            {
            }

            public TestPatternDayTradingMarginModel(decimal closedMarketLeverage, decimal openMarketLeverage)
                : base(closedMarketLeverage, openMarketLeverage)
            {
            }

            public new decimal GetInitialMarginRequiredForOrder(
                InitialMarginRequiredForOrderParameters parameters)
            {
                return base.GetInitialMarginRequiredForOrder(parameters);
            }

            public new decimal GetMaintenanceMargin(Security security)
            {
                return base.GetMaintenanceMargin(security);
            }
        }

        private static readonly DateTime Noon = new DateTime(2016, 02, 16, 12, 0, 0);
        private static readonly DateTime Midnight = new DateTime(2016, 02, 16, 0, 0, 0);
        private static readonly DateTime NoonWeekend = new DateTime(2016, 02, 14, 12, 0, 0);
        private static readonly DateTime NoonHoliday = new DateTime(2016, 02, 15, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

        [Test]
        public void InitializationTests()
        {
            // No parameters initialization, used default PDT 4x leverage open market and 2x leverage otherwise
            var model = new PatternDayTradingMarginModel();
            var leverage = model.GetLeverage(CreateSecurity(Noon));

            Assert.AreEqual(4.0m, leverage);

            model = new PatternDayTradingMarginModel(2.0m, 5.0m);
            leverage = model.GetLeverage(CreateSecurity(Noon));

            Assert.AreEqual(5.0m, leverage);
        }

        [Test]
        public void SetLeverageTest()
        {
            var model = new PatternDayTradingMarginModel();

            // Open market
            var security = CreateSecurity(Noon);

            security.BuyingPowerModel = new PatternDayTradingMarginModel();

            model.SetLeverage(security, 10m);
            Assert.AreNotEqual(10m, model.GetLeverage(security));

            // Closed market
            security = CreateSecurity(Midnight);

            model.SetLeverage(security, 10m);
            Assert.AreNotEqual(10m, model.GetLeverage(security));

            security.Holdings.SetHoldings(100m, 100);
        }

        [Test]
        public void VerifyOpenMarketLeverage()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon

            var leverage = 4m;
            var expected = 100 * 100m / leverage + 1;

            var model = new TestPatternDayTradingMarginModel();
            var security = CreateSecurity(Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);
        }

        [Test]
        public void VerifyOpenMarketLeverageAltVersion()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon

            var leverage = 5m;
            var expected = 100 * 100m / leverage + 1;

            var model = new TestPatternDayTradingMarginModel(2m, leverage);
            var security = CreateSecurity(Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);
        }

        [Test]
        public void VerifyClosedMarketLeverage()
        {
            var leverage = 2m;
            var expected = 100 * 100m / leverage + 1;

            var model = new TestPatternDayTradingMarginModel();

            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight
            var security = CreateSecurity(Midnight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);

            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)
            security = CreateSecurity(NoonHoliday);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);

            // Market is Closed on Sunday, Feb, 14th 2016 at Noon
            security = CreateSecurity(NoonWeekend);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);
        }

        [Test]
        public void VerifyClosedMarketLeverageAltVersion()
        {
            var leverage = 3m;
            var expected = 100 * 100m / leverage + 1;

            var model = new TestPatternDayTradingMarginModel(leverage, 4m);

            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight
            var security = CreateSecurity(Midnight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);

            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)
            security = CreateSecurity(NoonHoliday);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);

            // Market is Closed on Sunday, Feb, 14th 2016 at Noon
            security = CreateSecurity(NoonWeekend);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)), 1e-3);
        }

        [Test]
        public void VerifyClosingSoonMarketLeverage()
        {
            var closedLeverage = 2m;
            var openLeverage = 5m;

            var model = new TestPatternDayTradingMarginModel(closedLeverage, openLeverage);

            // Market is Closed on Tuesday, Feb, 16th 2016 at 16
            var security = CreateSecurity(new DateTime(2016, 2, 16, 15, 49, 0));
            Assert.AreEqual(openLeverage, model.GetLeverage(security));
            Assert.IsFalse(security.Exchange.ClosingSoon);

            security.Exchange.SetLocalDateTimeFrontier(new DateTime(2016, 2, 16, 15, 50, 0));
            Assert.AreEqual(closedLeverage, model.GetLeverage(security));
            Assert.IsTrue(security.Exchange.ClosingSoon);
            Assert.IsTrue(security.Exchange.ExchangeOpen);

            security.Exchange.SetLocalDateTimeFrontier(new DateTime(2016, 2, 16, 16, 0, 0));
            Assert.IsFalse(security.Exchange.ExchangeOpen);
        }

        [Test]
        public void VerifyMaintenaceMargin()
        {
            var model = new TestPatternDayTradingMarginModel();

            // Open Market
            var security = CreateSecurity(Noon);
            security.Holdings.SetHoldings(100m, 100);

            Assert.AreEqual((double)100 * 100 / 4, (double)model.GetMaintenanceMargin(security), 1e-3);

            // Closed Market
            security = CreateSecurity(Midnight);
            security.Holdings.SetHoldings(100m, 100);

            Assert.AreEqual((double)100 * 100 / 2, (double)model.GetMaintenanceMargin(security), 1e-3);
        }

        [Test]
        public void VerifyMarginCallOrderLongOpenMarket()
        {
            var securityPrice = 100m;
            var quantity = 300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Noon);
            var model = new PatternDayTradingMarginModel();

            // Open Market
            var security = CreateSecurity(Noon);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(-25000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            var expected = -(int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 4m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrder(security, netLiquidationValue, totalMargin).Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderLongClosedMarket()
        {
            var securityPrice = 100m;
            var quantity = 300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Midnight);
            var model = new PatternDayTradingMarginModel();

            // Open Market
            var security = CreateSecurity(Midnight);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(-25000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            var expected = -(int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 2m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrder(security, netLiquidationValue, totalMargin).Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderShortOpenMarket()
        {
            var securityPrice = 100m;
            var quantity = -300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Noon);
            var model = new PatternDayTradingMarginModel();

            // Open Market
            var security = CreateSecurity(Noon);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(35000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;

            var expected = (int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 4m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrder(security, netLiquidationValue, totalMargin).Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderShortClosedMarket()
        {
            var securityPrice = 100m;
            var quantity = -300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Midnight);
            var model = new PatternDayTradingMarginModel();

            // Open Market
            var security = CreateSecurity(Midnight);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(35000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;

            var expected = (int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 2m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrder(security, netLiquidationValue, totalMargin).Quantity;

            Assert.AreEqual(expected, actual);
        }

        private SecurityPortfolioManager GetPortfolio(IOrderProcessor orderProcessor, int quantity, DateTime time)
        {
            var securities = new SecurityManager(new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork));
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(orderProcessor);

            var portfolio = new SecurityPortfolioManager(securities, transactions);
            portfolio.SetCash(quantity);
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            return portfolio;
        }

        private static Security CreateSecurity(DateTime newLocalTime)
        {
            var security = new Security(
                CreateUsEquitySecurityExchangeHours(),
                CreateTradeBarConfig(),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            TimeKeeper.SetUtcDateTime(newLocalTime.ConvertToUtc(security.Exchange.TimeZone));
            security.Exchange.SetLocalDateTimeFrontier(newLocalTime);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, newLocalTime, 100m));
            return security;
        }

        private static SecurityExchangeHours CreateUsEquitySecurityExchangeHours()
        {
            var sunday = LocalMarketHours.ClosedAllDay(DayOfWeek.Sunday);
            var monday = new LocalMarketHours(DayOfWeek.Monday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var tuesday = new LocalMarketHours(DayOfWeek.Tuesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var wednesday = new LocalMarketHours(DayOfWeek.Wednesday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var thursday = new LocalMarketHours(DayOfWeek.Thursday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var friday = new LocalMarketHours(DayOfWeek.Friday, new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0));
            var saturday = LocalMarketHours.ClosedAllDay(DayOfWeek.Saturday);

            var earlyCloses = new Dictionary<DateTime, TimeSpan>();
            var lateOpens = new Dictionary<DateTime, TimeSpan>();
            return new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork,
                TimeZones.NewYork, true, true, false);
        }
    }
}