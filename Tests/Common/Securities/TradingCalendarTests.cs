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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using System;
using System.Linq;

namespace QuantConnect.Tests.Common.Securities
{

    [TestFixture]
    public class TradingCalendarTests
    {
        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);

        [Test]
        public void TestBasicFeaturesWithOptionsFutures()
        {
            var securities = new SecurityManager(TimeKeeper);
            var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Equity, Symbols.SPY),
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );
            securities[Symbols.SPY].SetMarketPrice(new TradeBar { Time = securities.UtcTime, Symbol = Symbols.SPY, Close = 195 });

            var option1 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 192m, new DateTime(2016, 02, 16));
            securities.Add(
                option1,
                new Option(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Option, option1),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var option2 = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 193m, new DateTime(2016, 03, 19));
            securities.Add(
                option2,
                new Option(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Option, option2),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var future1= Symbol.CreateFuture("ES", Market.USA, new DateTime(2016, 02, 16));
            securities.Add(
                future1,
                new Future(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Future, future1),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var future2 = Symbol.CreateFuture("ES", Market.USA, new DateTime(2016, 02, 19));
            securities.Add(
                future2,
                new Future(
                    SecurityExchangeHours,
                    CreateTradeBarDataConfig(SecurityType.Future, future2),
                    new Cash(Currencies.USD, 0, 1m),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null
                )
            );

            var cal = new TradingCalendar(securities, marketHoursDatabase);

            var optionDays = cal.GetDaysByType(TradingDayType.OptionExpiration, new DateTime(2016, 02, 16), new DateTime(2016, 03, 19)).Count();
            Assert.AreEqual(2, optionDays);

            var futureDays = cal.GetDaysByType(TradingDayType.OptionExpiration, new DateTime(2016, 02, 16), new DateTime(2016, 03, 19)).Count();
            Assert.AreEqual(2, futureDays);

            var days = cal.GetTradingDays(new DateTime(2016, 02, 16), new DateTime(2016, 03, 19));

            var optionAndfutureDays = days.Where(x => x.FutureExpirations.Any() || x.OptionExpirations.Any()).Count();
            Assert.AreEqual(3, optionAndfutureDays);

            // why? because option1 and future1 expire in one day 2016-02-16. Lets have a look.
            var day = cal.GetTradingDay(new DateTime(2016, 02, 16));
            Assert.AreEqual(1, day.OptionExpirations.Count());
            Assert.AreEqual(1, day.FutureExpirations.Count());

            var businessDays = days.Where(x => x.BusinessDay).Count();
            Assert.AreEqual(24, businessDays);

            var weekends = days.Where(x => x.Weekend).Count();
            Assert.AreEqual(9, weekends);

            Assert.AreEqual(24 + 9, (new DateTime(2016, 03, 19) - new DateTime(2016, 02, 16)).TotalDays + 1 /*inclusive*/);
        }

        private SubscriptionDataConfig CreateTradeBarDataConfig(SecurityType type, Symbol symbol)
        {
            if (type == SecurityType.Equity)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Forex)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Option)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            if (type == SecurityType.Future)
                return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true);
            throw new NotImplementedException(type.ToString());
        }

        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZones.NewYork }); }
        }

    }
}
