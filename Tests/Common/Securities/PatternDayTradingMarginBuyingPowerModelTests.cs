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
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class PatternDayTradingMarginBuyingPowerModelTests
    {
        private static readonly DateTime Noon = new DateTime(2016, 02, 16, 12, 0, 0);
        private static readonly DateTime Midnight = new DateTime(2016, 02, 16, 0, 0, 0);
        private static readonly DateTime NoonWeekend = new DateTime(2016, 02, 14, 12, 0, 0);
        private static readonly DateTime NoonHoliday = new DateTime(2016, 02, 15, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);

        private BuyingPowerModelComparator GetModel(decimal closed = 2m, decimal open = 4m)
        {
            return new BuyingPowerModelComparator(
                new PatternDayTradingMarginModel(closed, open),
                new SecurityPositionGroupBuyingPowerModel(),
                timeKeeper: TimeKeeper
            );
        }

        [Test]
        public void InitializationTests()
        {
            // No parameters initialization, used default PDT 4x leverage open market and 2x leverage otherwise
            var model = GetModel();
            var leverage = model.GetLeverage(CreateSecurity(model.SecurityModel, Noon));

            Assert.AreEqual(4.0m, leverage);

            model = GetModel(2m, 5m);
            leverage = model.GetLeverage(CreateSecurity(model.SecurityModel, Noon));

            Assert.AreEqual(5.0m, leverage);
        }

        [Test]
        public void SetLeverageTest()
        {
            var model = GetModel();

            // Open market
            var security = CreateSecurity(model.SecurityModel, Noon);

            security.BuyingPowerModel = GetModel();

            model.SetLeverage(security, 10m);
            Assert.AreNotEqual(10m, model.GetLeverage(security));

            // Closed market
            security = CreateSecurity(model.SecurityModel, Midnight);

            model.SetLeverage(security, 10m);
            Assert.AreNotEqual(10m, model.GetLeverage(security));

            security.Holdings.SetHoldings(100m, 100);
        }

        [Test]
        public void VerifyOpenMarketLeverage()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon
            // SPY @ $100 * 100 Shares / Leverage (4) = 2500
            var leverage = 4m;
            var expected = 100 * 100m / leverage;

            var model = GetModel();
            var security = CreateSecurity(model.SecurityModel, Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);
        }

        [Test]
        public void VerifyOpenMarketLeverageAltVersion()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon
            // SPY @ $100 * 100 Shares / Leverage (5) = 2000
            var leverage = 5m;
            var expected = 100 * 100m / leverage;

            var model = GetModel(2m, leverage);
            var security = CreateSecurity(model.SecurityModel, Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);
        }

        [Test]
        public void VerifyClosedMarketLeverage()
        {
            // SPY @ $100 * 100 Shares / Leverage (2) = 5000
            var leverage = 2m;
            var expected = 100 * 100m / leverage;

            var model = GetModel();

            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight
            var security = CreateSecurity(model.SecurityModel, Midnight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);

            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)
            security = CreateSecurity(model.SecurityModel, NoonHoliday);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);

            // Market is Closed on Sunday, Feb, 14th 2016 at Noon
            security = CreateSecurity(model.SecurityModel, NoonWeekend);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);
        }

        [Test]
        public void VerifyClosedMarketLeverageAltVersion()
        {
            // SPY @ $100 * 100 Shares / Leverage (3) = 3333.33
            var leverage = 3m;
            var expected = 100 * 100m / leverage;

            var model = GetModel(leverage, 4m);

            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight
            var security = CreateSecurity(model.SecurityModel, Midnight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);

            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)
            security = CreateSecurity(model.SecurityModel, NoonHoliday);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);

            // Market is Closed on Sunday, Feb, 14th 2016 at Noon
            security = CreateSecurity(model.SecurityModel, NoonWeekend);
            order = new MarketOrder(security.Symbol, 100, security.LocalTime);

            Assert.AreEqual((double)leverage, (double)model.GetLeverage(security), 1e-3);
            Assert.AreEqual((double)expected, (double)model.GetInitialMarginRequiredForOrder(
                new InitialMarginRequiredForOrderParameters(new IdentityCurrencyConverter(Currencies.USD), security, order)).Value, 1e-3);
        }

        [Test]
        public void VerifyClosingSoonMarketLeverage()
        {
            var closedLeverage = 2m;
            var openLeverage = 5m;

            var model = GetModel(closedLeverage, openLeverage);

            // Market is Closed on Tuesday, Feb, 16th 2016 at 16
            var security = CreateSecurity(model.SecurityModel, new DateTime(2016, 2, 16, 15, 49, 0));
            Assert.AreEqual(openLeverage, model.GetLeverage(security));
            Assert.IsFalse(security.Exchange.ClosingSoon);

            var localTimeKeeper = TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            localTimeKeeper.UpdateTime(new DateTime(2016, 2, 16, 15, 50, 0).ConvertToUtc(TimeZones.NewYork));
            Assert.AreEqual(closedLeverage, model.GetLeverage(security));
            Assert.IsTrue(security.Exchange.ClosingSoon);
            Assert.IsTrue(security.Exchange.ExchangeOpen);

            localTimeKeeper.UpdateTime(new DateTime(2016, 2, 16, 16, 0, 0).ConvertToUtc(TimeZones.NewYork));
            Assert.IsFalse(security.Exchange.ExchangeOpen);
        }

        [Test]
        public void VerifyMaintenaceMargin()
        {
            var model = GetModel();

            // Open Market
            var security = CreateSecurity(model.SecurityModel, Noon);
            security.Holdings.SetHoldings(100m, 100);

            Assert.AreEqual((double)100 * 100 / 4, (double)model.GetMaintenanceMargin(security), 1e-3);

            // Closed Market
            security = CreateSecurity(model.SecurityModel, Midnight);
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
            var model = GetModel();

            // Open Market
            var security = CreateSecurity(model.SecurityModel, Noon);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(-25000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            var expected = -(int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 4m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrders(
                new MarginCallOrdersParameters(portfolio.Positions.Groups.Single(), netLiquidationValue, totalMargin)).Single().Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderLongClosedMarket()
        {
            var securityPrice = 100m;
            var quantity = 300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Midnight);
            var model = GetModel();

            // Open Market
            var security = CreateSecurity(model.SecurityModel, Midnight);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(-25000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            var expected = -(int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 2m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrders(
                new MarginCallOrdersParameters(portfolio.Positions.Groups.Single(), netLiquidationValue, totalMargin)).Single().Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderShortOpenMarket()
        {
            var securityPrice = 100m;
            var quantity = -300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Noon);
            var model = GetModel();

            // Open Market
            var security = CreateSecurity(model.SecurityModel, Noon);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(35000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;

            var expected = (int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 4m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrders(
                new MarginCallOrdersParameters(portfolio.Positions.Groups.Single(), netLiquidationValue, totalMargin)).Single().Quantity;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyMarginCallOrderShortClosedMarket()
        {
            var securityPrice = 100m;
            var quantity = -300;

            var orderProcessor = new FakeOrderProcessor();
            var portfolio = GetPortfolio(orderProcessor, quantity, Midnight);
            var model = GetModel();

            // Open Market
            var security = CreateSecurity(model.SecurityModel, Midnight);
            security.BuyingPowerModel = model;
            security.Holdings.SetHoldings(securityPrice, quantity);
            portfolio.Securities.Add(security);
            portfolio.CashBook["USD"].AddAmount(35000);
            portfolio.InvalidateTotalPortfolioValue();
            var netLiquidationValue = portfolio.TotalPortfolioValue;
            var totalMargin = portfolio.TotalMarginUsed;

            var expected = (int)(Math.Round((totalMargin - netLiquidationValue) / securityPrice, MidpointRounding.AwayFromZero) * 2m);
            var actual = (portfolio.MarginCallModel as TestDefaultMarginCallModel).GenerateMarginCallOrders(
                new MarginCallOrdersParameters(portfolio.Positions.Groups.Single(), netLiquidationValue, totalMargin)).Single().Quantity;

            Assert.AreEqual(expected, actual);
        }

        private SecurityPortfolioManager GetPortfolio(IOrderProcessor orderProcessor, int quantity, DateTime time)
        {
            var securities = new SecurityManager(new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork));
            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(orderProcessor);

            var portfolio = new SecurityPortfolioManager(securities, transactions, new AlgorithmSettings());
            portfolio.SetCash(quantity);
            portfolio.MarginCallModel = new TestDefaultMarginCallModel(portfolio, new OrderProperties());

            return portfolio;
        }

        private static Security CreateSecurity(IBuyingPowerModel buyingPowerModel, DateTime newLocalTime)
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
            security.BuyingPowerModel = buyingPowerModel;
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, newLocalTime, 100m));
            security.FeeModel = new ConstantFeeModel(0);
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
            var holidays = MarketHoursDatabase.FromDataFolder()
                        .GetEntry(Market.USA, (string)null, SecurityType.Equity)
                        .ExchangeHours
                        .Holidays;
            return new SecurityExchangeHours(TimeZones.NewYork, holidays, new[]
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
