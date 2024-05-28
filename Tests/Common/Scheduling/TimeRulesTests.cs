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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Scheduling
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class TimeRulesTests
    {
        private static DateTime _utcNow = new DateTime(2021, 07, 27, 1, 10, 10, 500);

        [Test]
        public void AtSpecificTimeFromUtc()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.At(TimeSpan.FromHours(12));
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 01) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(12), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void AtSpecificTimeFromNonUtc()
        {
            var rules = GetTimeRules(TimeZones.NewYork);
            var rule = rules.At(TimeSpan.FromHours(12));
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 01) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(12 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketOpenNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(9.5 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketOpenWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 30);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(9.5 + 5 + .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketOpenNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 0, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(4 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketOpenNoDeltaForContinuousSchedules()
        {
            var rules = GetFutureTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.ES_Future_Chain, 0);
            var times = rule.CreateUtcEventTimes(new[] {
                new DateTime(2022, 01, 03),
                new DateTime(2022, 01, 04),
                new DateTime(2022, 01, 05),
                new DateTime(2022, 01, 06),
                new DateTime(2022, 01, 07),
                new DateTime(2022, 01, 10)
            });

            var expectedMarketOpenDates = new[] {
                new DateTime(2022, 01, 03, 14, 30, 0),
                new DateTime(2022, 01, 04, 14, 30, 0),
                new DateTime(2022, 01, 05, 14, 30, 0),
                new DateTime(2022, 01, 06, 14, 30, 0),
                new DateTime(2022, 01, 07, 14, 30, 0),
                new DateTime(2022, 01, 10, 14, 30, 0)
            };
            int count = 0;
            foreach (var time in times)
            {
                Assert.AreEqual(expectedMarketOpenDates[count], time);
                count++;
            }
            Assert.AreEqual(6, count);
        }

        [Test]
        public void ExtendedMarketOpenNoDeltaForContinuousSchedules()
        {
            var rules = GetFutureTimeRules(TimeZones.Utc, true);
            var rule = rules.AfterMarketOpen(Symbols.ES_Future_Chain, 0, true);
            var times = rule.CreateUtcEventTimes(new[] {
                new DateTime(2022, 01, 01),
                new DateTime(2022, 01, 02),
                new DateTime(2022, 01, 03),
                new DateTime(2022, 01, 04),
                new DateTime(2022, 01, 05),
                new DateTime(2022, 01, 06),
                new DateTime(2022, 01, 07),
                new DateTime(2022, 01, 08),
                new DateTime(2022, 01, 09)
            });

            var expectedMarketOpenDates = new[] {
                new DateTime(2022, 01, 02, 23, 0, 0),
                new DateTime(2022, 01, 03, 23, 0, 0),
                new DateTime(2022, 01, 04, 23, 0, 0),
                new DateTime(2022, 01, 05, 23, 0, 0),
                new DateTime(2022, 01, 06, 23, 0, 0),
                new DateTime(2022, 01, 09, 23, 0, 0)
            };
            int count = 0;
            foreach (var time in times)
            {
                Assert.AreEqual(expectedMarketOpenDates[count], time);
                count++;
            }
            Assert.AreEqual(6, count);
        }

        [Test]
        public void ExtendedMarketCloseNoDeltaForContinuousSchedules()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 0, true);
            var times = rule.CreateUtcEventTimes(new[] {
                new DateTime(2022, 01, 01),
                new DateTime(2022, 01, 02),
                new DateTime(2022, 01, 03),
                new DateTime(2022, 01, 04),
                new DateTime(2022, 01, 05),
                new DateTime(2022, 01, 06),
                new DateTime(2022, 01, 07),
                new DateTime(2022, 01, 08),
                new DateTime(2022, 01, 09)
            });

            var expectedMarketOpenDates = new[] {
                new DateTime(2022, 01, 04, 01, 00, 00),
                new DateTime(2022, 01, 05, 01, 00, 00),
                new DateTime(2022, 01, 06, 01, 00, 00),
                new DateTime(2022, 01, 07, 01, 00, 00),
                new DateTime(2022, 01, 08, 01, 00, 00)
            };
            int count = 0;
            foreach (var time in times)
            {
                Assert.AreEqual(expectedMarketOpenDates[count], time);
                count++;
            }
            Assert.AreEqual(5, count);
        }

        [Test]
        public void ExtendedMarketOpenWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.AfterMarketOpen(Symbols.SPY, 30, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(4 + 5 + .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketCloseNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(16 + 5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void RegularMarketCloseWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 30);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours(16 + 5 - .5), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketCloseNoDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 0, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours((20 + 5) % 24), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ExtendedMarketCloseWithDelta()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var rule = rules.BeforeMarketClose(Symbols.SPY, 30, true);
            var times = rule.CreateUtcEventTimes(new[] { new DateTime(2000, 01, 03) });

            int count = 0;
            foreach (var time in times)
            {
                count++;
                Assert.AreEqual(TimeSpan.FromHours((20 + 5 - .5) % 24), time.TimeOfDay);
            }
            Assert.AreEqual(1, count);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void EveryValidatesTimeSpan(int timeSpanMinutes)
        {
            var rules = GetTimeRules(TimeZones.Utc);
            Assert.Throws<ArgumentException>(() => rules.Every(TimeSpan.FromMinutes(timeSpanMinutes)));
        }

        [Test]
        public void Every()
        {
            var rules = GetTimeRules(TimeZones.Utc);
            var every = rules.Every(TimeSpan.FromMilliseconds(500));
            var previous = new DateTime(2019, 6, 28);
            var dateTimes = every.CreateUtcEventTimes(new[] { previous });
            foreach (var dateTime in dateTimes)
            {
                if (previous != dateTime)
                {
                    Assert.Fail("Unexpected Every DateTime");
                }
                previous = previous.AddMilliseconds(500);
            }
        }

        [Test]
        public void SetTimeZone()
        {
            var rules = GetTimeRules(TimeZones.NewYork);
            var nowNewYork = rules.Now.CreateUtcEventTimes(new [] { _utcNow.Date }).Single();

            rules.SetDefaultTimeZone(TimeZones.Utc);

            var nowUtc = rules.Now.CreateUtcEventTimes(new [] { _utcNow.Date }).Single();

            Assert.AreEqual(_utcNow, nowUtc);
            Assert.AreEqual(nowUtc, nowNewYork);
        }

        [Test]
        public void SetFuncTimeRuleInPythonWorksAsExpected()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

def CustomTimeRule(dates):
    return [dates[0] + timedelta(days=1)]
");
                dynamic pythonCustomTimeRule = pythonModule.GetAttr("CustomTimeRule");
                var funcTimeRule = new FuncTimeRule("PythonFuncTimeRule", pythonCustomTimeRule);
                Assert.AreEqual("PythonFuncTimeRule", funcTimeRule.Name);
                Assert.AreEqual(new DateTime(2023, 1, 2, 0, 0, 0), funcTimeRule.CreateUtcEventTimes(new List<DateTime>() { new DateTime(2023, 1, 1) }).First());
            }
        }

        [Test]
        public void SetFuncTimeRuleInPythonWorksAsExpectedWithCSharpFunc()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

def GetFuncTimeRule(csharpFunc):
    return FuncTimeRule(""CSharp"", csharpFunc)
");
                dynamic getFuncTimeRule = pythonModule.GetAttr("GetFuncTimeRule");
                Func<IEnumerable<DateTime>, IEnumerable<DateTime>> csharpFunc = (dates) => { return new List<DateTime>() { new DateTime(2001, 3, 18) }; };
                var funcTimeRule = getFuncTimeRule(csharpFunc);
                Assert.AreEqual("CSharp", (funcTimeRule.Name as PyObject).GetAndDispose<string>());
                Assert.AreEqual(new DateTime(2001, 3, 18),
                    (funcTimeRule.CreateUtcEventTimes(new List<DateTime>() { new DateTime(2023, 1, 1) }) as PyObject).GetAndDispose<List<DateTime>>().First());
            }
        }

        [Test]
        public void SetFuncTimeRuleInPythonFailsWhenInvalidTimeRule()
        {
            using (Py.GIL())
            {
                var pythonModule = PyModule.FromString("testModule", @"
from AlgorithmImports import *

wrongCustomTimeRule = ""hello""
");
                dynamic pythonCustomTimeRule = pythonModule.GetAttr("wrongCustomTimeRule");
                Assert.Throws<ArgumentException>(() => new FuncTimeRule("PythonFuncTimeRule", pythonCustomTimeRule));
            }
        }

        private static TimeRules GetTimeRules(DateTimeZone dateTimeZone)
        {
            var timeKeeper = new TimeKeeper(_utcNow, new List<DateTimeZone>());
            var manager = new SecurityManager(timeKeeper);
            var mhdb = MarketHoursDatabase.FromDataFolder();
            var marketHourDbEntry = mhdb.GetEntry(Market.USA, (string)null, SecurityType.Equity);
            var securityExchangeHours = marketHourDbEntry.ExchangeHours;
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Daily, marketHourDbEntry.DataTimeZone, securityExchangeHours.TimeZone, true, false, false);
            manager.Add(
                Symbols.SPY,
                new Security(
                    securityExchangeHours,
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            var rules = new TimeRules(manager, dateTimeZone, mhdb);
            return rules;
        }

        private static TimeRules GetFutureTimeRules(DateTimeZone dateTimeZone, bool extendedMarket = false)
        {
            var timeKeeper = new TimeKeeper(_utcNow, new List<DateTimeZone>());
            var manager = new SecurityManager(timeKeeper);
            var mhdb = MarketHoursDatabase.FromDataFolder();
            var marketHourDbEntry = mhdb.GetEntry(Market.CME, "ES", SecurityType.Future);
            var securityExchangeHours = marketHourDbEntry.ExchangeHours;
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.ES_Future_Chain, Resolution.Daily, marketHourDbEntry.DataTimeZone,
                securityExchangeHours.TimeZone, true, extendedMarket, false);
            manager.Add(
                Symbols.ES_Future_Chain,
                new Security(
                    securityExchangeHours,
                    config,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            var rules = new TimeRules(manager, dateTimeZone, mhdb);
            return rules;
        }
    }
}
