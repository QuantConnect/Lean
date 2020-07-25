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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.Threading;
using DateTime = System.DateTime;
using Tick = QuantConnect.Data.Market.Tick;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class AggregationManagerTests
    {
        [Test]
        public void PassesTicksStraightThrough()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Tick);

            var count = 0;
            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.SPY, 30, 30) {TickType = TickType.Trade});
            aggregator.Update(new Tick(reference.AddSeconds(2), Symbols.SPY, 20, 20) { TickType = TickType.Trade });

            Assert.AreEqual(count, 2);

            aggregator.Update(new Tick(reference.AddSeconds(3), Symbols.AAPL, 200, 200) { TickType = TickType.Trade });
            Assert.AreEqual(count, 2);

            aggregator.Remove(config);
            aggregator.Update(new Tick(reference.AddSeconds(4), Symbols.SPY, 20, 20) { TickType = TickType.Trade });
            Assert.AreEqual(count, 2);
        }

        [Test]
        public void BadTicksIgnored()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Tick);

            var count = 0;
            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.AAPL, 200, 200));
            Assert.AreEqual(count, 0);
        }

        [Test]
        public void TickTypeRespected()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Tick);

            var count = 0;
            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(3), Symbols.SPY, 200, 200) { TickType = TickType.Trade });
            Assert.AreEqual(count, 1);

            aggregator.Update(new Tick(reference.AddSeconds(4), Symbols.SPY, 20, 20) { TickType = TickType.Quote });
            Assert.AreEqual(count, 1);
        }

        [Test]
        public void UnknownSubscriptionIgnored()
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var config = GetSubscriptionDataConfig<Tick>(Symbols.SPY, Resolution.Tick);

            var count = 0;

            aggregator.Update(new Tick(reference.AddSeconds(1), Symbols.SPY, 30, 30) { TickType = TickType.Trade });
            aggregator.Update(new Tick(reference.AddSeconds(2), Symbols.SPY, 20, 20) { TickType = TickType.Trade });
            Assert.AreEqual(count, 0);

            aggregator.Add(config, (s, e) => { count++; });

            aggregator.Update(new Tick(reference.AddSeconds(3), Symbols.SPY, 200, 200) { TickType = TickType.Trade });
            Assert.AreEqual(count, 1);

            aggregator.Remove(config);
            aggregator.Update(new Tick(reference.AddSeconds(4), Symbols.SPY, 20, 20) { TickType = TickType.Trade });
            Assert.AreEqual(count, 1);
        }

        [TestCase(100, 1, typeof(TradeBar), Resolution.Minute)]
        [TestCase(120, 2, typeof(TradeBar), Resolution.Minute)]
        [TestCase(121, 2, typeof(TradeBar), Resolution.Minute)]
        [TestCase(100, 1, typeof(QuoteBar), Resolution.Minute)]
        [TestCase(120, 2, typeof(QuoteBar), Resolution.Minute)]
        [TestCase(121, 2, typeof(QuoteBar), Resolution.Minute)]
        [TestCase(100, 99, typeof(TradeBar), Resolution.Second)]
        [TestCase(121, 120, typeof(QuoteBar), Resolution.Second)]
        [TestCase(3599, 0, typeof(QuoteBar), Resolution.Hour)]
        [TestCase(3599, 0, typeof(TradeBar), Resolution.Hour)]
        [TestCase(3600, 1, typeof(QuoteBar), Resolution.Hour)]
        [TestCase(3600, 1, typeof(TradeBar), Resolution.Hour)]
        [TestCase(3601, 1, typeof(QuoteBar), Resolution.Hour)]
        [TestCase(3601, 1, typeof(TradeBar), Resolution.Hour)]
        public void CanHandleMultipleSubscriptions(int secondsToAdd, int expectedBars, Type dataType, Resolution resolution)
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var symbols = new[] { Symbols.SPY, Symbols.AAPL, Symbols.USDJPY, Symbols.EURUSD };
            var enumerators = new Queue<IEnumerator<BaseData>>();

            foreach (var symbol in symbols)
            {
                enumerators.Enqueue(aggregator.Add(GetSubscriptionDataConfig(dataType, symbol, resolution), (s, e) => { }));
            }

            for (var i = 1; i <= secondsToAdd; i++)
            {
                foreach (var symbol in symbols)
                {
                    aggregator.Update(new Tick(reference.AddSeconds(i), symbol, 20 + i, 20 + i) { TickType = dataType == typeof(TradeBar) ? TickType.Trade : TickType.Quote});
                }
            }

            foreach (var enumerator in enumerators)
            {
                for (int i = 0; i < expectedBars; i++)
                {
                    enumerator.MoveNext();
                    Assert.IsNotNull(enumerator.Current);
                }

                enumerator.MoveNext();
                Assert.IsNull(enumerator.Current);
            }
        }

        [TestCase(typeof(TradeBar), TickType.Trade, Resolution.Second)]
        [TestCase(typeof(QuoteBar), TickType.Quote, Resolution.Second)]
        [TestCase(typeof(Tick), TickType.Trade, Resolution.Tick)]
        [TestCase(typeof(Tick), TickType.Quote, Resolution.Tick)]
        public void CanHandleBars(Type type, TickType tickType, Resolution resolution)
        {
            var aggregator = GetDataAggregator();
            var reference = DateTime.Today;
            var total = 0;
            var enumerator = aggregator.Add(GetSubscriptionDataConfig(type, Symbols.EURUSD, resolution, tickType), (s, e) => { });

            for (int i = 0; i < 100; i++)
            {
                aggregator.Update(new Tick(reference.AddSeconds(i), Symbols.EURUSD, 20 + i, 20 + i) { TickType = tickType });
            }
            Thread.Sleep(250);

            enumerator.MoveNext();
            while (enumerator.Current != null)
            {
                Assert.IsTrue(enumerator.Current.GetType() == type);
                var tick = enumerator.Current as Tick;
                if (tick != null)
                {
                    Assert.IsTrue(tick.TickType == tickType);
                }
                total++;
                enumerator.MoveNext();
            }

            if (resolution == Resolution.Second)
            {
                Assert.AreEqual(99, total);
            }
            else
            {
                Assert.AreEqual(100, total);
            }
        }

        private IDataAggregator GetDataAggregator()
        {
            return new TestAggregationManager(new ManualTimeProvider(DateTime.Today));
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return GetSubscriptionDataConfig(typeof(T), symbol, resolution);
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig(Type T, Symbol symbol, Resolution resolution, TickType? tickType = null)
        {
            return new SubscriptionDataConfig(
                T,
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false,
                tickType: tickType);
        }

        private class TestAggregationManager : AggregationManager
        {
            public TestAggregationManager(ITimeProvider timeProvider)
            {
                TimeProvider = timeProvider;
            }
        }
    }
}
