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
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Common.Securities.Equity
{
    [TestFixture]
    public class PatternDayTradingMarginModelTests
    {
        private static readonly DateTime Noon = new DateTime(2016, 02, 16, 12, 0, 0);
        private static readonly DateTime MidNight = new DateTime(2016, 02, 16, 0, 0, 0);
        private static readonly DateTime NoonWeekend = new DateTime(2016, 02, 14, 12, 0, 0);
        private static readonly DateTime NoonHoliday = new DateTime(2016, 02, 15, 12, 0, 0);

        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork),
            TimeZones.NewYork);

        private static Security CreateSecurity(DateTime newLocalTime)
        {
            var security = new Security(CreateUsEquitySecurityExchangeHours(), CreateTradeBarConfig());
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

            return new SecurityExchangeHours(TimeZones.NewYork, USHoliday.Dates.Select(x => x.Date), new[]
            {
                sunday, monday, tuesday, wednesday, thursday, friday, saturday
            }.ToDictionary(x => x.DayOfWeek));
        }

        private static SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof (TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork,
                TimeZones.NewYork, true, true, false);
        }

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
        public void VerifyClosedMarketLeverage()
        {
            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight

            var leverage = 2m;

            var model = new PatternDayTradingMarginModel();
            var security = CreateSecurity(MidNight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyClosedMarketLeverageAltVersion()
        {
            // Market is Closed on Tuesday, Feb, 16th 2016 at Midnight

            var leverage = 3m;

            var model = new PatternDayTradingMarginModel(leverage, 4m);
            var security = CreateSecurity(MidNight);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyHolidayMarketLeverage()
        {
            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)

            var leverage = 2m;

            var model = new PatternDayTradingMarginModel();
            var security = CreateSecurity(NoonHoliday);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyHolidayMarketLeverageAltVersion()
        {
            // Market is Closed on Monday, Feb, 15th 2016 at Noon (US President Day)

            var leverage = 3m;

            var model = new PatternDayTradingMarginModel(leverage, 4m);
            var security = CreateSecurity(NoonHoliday);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyOpenMarketLeverage()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon

            var leverage = 4m;

            var model = new PatternDayTradingMarginModel();
            var security = CreateSecurity(Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyOpenMarketLeverageAltVersion()
        {
            // Market is Open on Tuesday, Feb, 16th 2016 at Noon

            var leverage = 5m;

            var model = new PatternDayTradingMarginModel(2m, leverage);
            var security = CreateSecurity(Noon);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyWeekedMarketLeverageAltVersion()
        {
            // Market is Closed on Sunday, Feb, 14th 2016 at Noon

            var leverage = 3m;

            var model = new PatternDayTradingMarginModel(leverage, 4m);
            var security = CreateSecurity(NoonWeekend);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyWeekendMarketLeverage()
        {
            // Market is Closed on Sunday, Feb, 14th 2016 at Noon

            var leverage = 2m;

            var model = new PatternDayTradingMarginModel();
            var security = CreateSecurity(NoonWeekend);
            var order = new MarketOrder(security.Symbol, 100, security.LocalTime, type: security.Type);

            var expected = 100*100m/leverage + 1;
            var actual = model.GetInitialMarginRequiredForOrder(security, order);

            Assert.AreEqual(expected, actual);
        }
    }
}