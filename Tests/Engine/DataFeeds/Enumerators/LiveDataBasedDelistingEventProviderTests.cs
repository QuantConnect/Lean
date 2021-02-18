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
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class LiveDataBasedDelistingEventProviderTests
    {
        [Test]
        public void EmitsBasedOnData()
        {
            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.AAPL,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
            var time = new DateTime(2021, 2, 17);
            var timeProvider = new ManualTimeProvider(time);
            Delisting delisting = null;
            var autoResetEvent = new AutoResetEvent(false);
            var dataQueueHandler = new FuncDataQueueHandler(handler =>
            {
                autoResetEvent.Set();
                if (delisting != null)
                {
                    var result = new[] { delisting };
                    delisting = null;
                    return result;
                }
                return Enumerable.Empty<BaseData>();
            }, timeProvider);
            var provider = new LiveDataBasedDelistingEventProvider(config, dataQueueHandler);
            var mapFile = new LocalDiskMapFileProvider().Get(config.Symbol.ID.Market).ResolveMapFile(config.Symbol, config.Type);
            provider.Initialize(config, null, mapFile, time);
            Assert.IsTrue(autoResetEvent.WaitOne(TimeSpan.FromMilliseconds(100)));

            var events = provider.GetEvents(new NewTradableDateEventArgs(time, null, Symbols.AAPL, null)).ToList();
            Assert.AreEqual(0, events.Count);

            delisting = new Delisting(Symbols.AAPL, time, 1, DelistingType.Warning);
            Thread.Sleep(100);

            events = provider.GetEvents(new NewTradableDateEventArgs(time.AddDays(-1), null, Symbols.AAPL, null)).ToList();
            Assert.AreEqual(0, events.Count);

            events = provider.GetEvents(new NewTradableDateEventArgs(time, null, Symbols.AAPL, null)).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(time, (events[0] as Delisting).Time);
            Assert.AreEqual(DelistingType.Warning, (events[0] as Delisting).Type);

            events = provider.GetEvents(new NewTradableDateEventArgs(time, null, Symbols.AAPL, null)).ToList();
            Assert.AreEqual(0, events.Count);

            events = provider.GetEvents(new NewTradableDateEventArgs(time.AddDays(1), null, Symbols.AAPL, null)).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(time.AddDays(1), (events[0] as Delisting).Time);
            Assert.AreEqual(DelistingType.Delisted, (events[0] as Delisting).Type);
        }
    }
}
