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
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class QuoteBarBuilderEnumeratorTests
    {
        [Test]
        public void AggregatesTicksIntoSecondBars()
        {
            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            var enumerator = new QuoteBarBuilderEnumerator(Time.OneSecond, TimeZones.NewYork, timeProvider, false);

            // noon new york time
            var currentTime = new DateTime(2015, 10, 08, 12, 0, 0);
            timeProvider.SetCurrentTime(currentTime);

            // add some ticks
            var ticks = new List<Tick>
            {
                new Tick(currentTime, Symbols.SPY, 199, 200) {Quantity = 10},
                new Tick(currentTime, Symbols.SPY, 199.21m, 200.02m) {Quantity = 5},
                new Tick(currentTime, Symbols.SPY, 198.77m, 199.75m) {Quantity = 20},
                new Tick(currentTime, Symbols.SPY, 198.77m, 199.75m) {Quantity = 0},
                new Tick(currentTime, Symbols.SPY, 198.77m, 199.75m) {Quantity = 20},
                new Tick(currentTime, Symbols.SPY, 198.77m, 199.75m) {Quantity = 0},
            };

            foreach (var tick in ticks)
            {
                enumerator.ProcessData(tick);
            }

            // even though no data is here, it will still return true
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            // advance a second
            currentTime = currentTime.AddSeconds(1);
            timeProvider.SetCurrentTime(currentTime);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            var bar = (QuoteBar)enumerator.Current;
            Assert.AreEqual(currentTime.AddSeconds(-1), bar.Time);
            Assert.AreEqual(currentTime, bar.EndTime);
            Assert.AreEqual(Symbols.SPY, bar.Symbol);
            Assert.AreEqual(ticks.First().LastPrice, bar.Open);
            Assert.AreEqual(ticks.Max(x => x.LastPrice), bar.High);
            Assert.AreEqual(ticks.Min(x => x.LastPrice), bar.Low);
            Assert.AreEqual(ticks.Last().LastPrice, bar.Close);

            enumerator.Dispose();
        }

        [Test]
        public void LastCloseAndCurrentOpenPriceShouldBeSameConsolidated()
        {
            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            var enumerator = new QuoteBarBuilderEnumerator(Time.OneSecond, TimeZones.NewYork, timeProvider, false);

            // noon new york time
            var currentTime = new DateTime(2015, 10, 08, 12, 0, 0);
            timeProvider.SetCurrentTime(currentTime);

            var reference = DateTime.Today;
            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference,
                TickType = TickType.Quote,
                AskPrice = 0,
                BidPrice = 24,

            };
            enumerator.ProcessData(tick1);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(1),
                TickType = TickType.Quote,
                AskPrice = 25,
                BidPrice = 0,

            };
            enumerator.ProcessData(tick2);
            currentTime = currentTime.AddSeconds(1);
            timeProvider.SetCurrentTime(currentTime);

            Assert.IsTrue(enumerator.MoveNext());

            var quoteBar = enumerator.Current as QuoteBar;

            // bar 1 emitted
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Open);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Open);
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Close);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Close);

            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(1),
                TickType = TickType.Quote,
                AskPrice = 36,
                BidPrice = 35,
            };
            enumerator.ProcessData(tick3);

            currentTime = currentTime.AddSeconds(1);
            timeProvider.SetCurrentTime(currentTime);
            Assert.IsTrue(enumerator.MoveNext());
            quoteBar = enumerator.Current as QuoteBar;

            // bar 2 emitted
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Open, "Ask Open not equal to Previous Close");
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Open, "Bid Open not equal to Previous Close");
            Assert.AreEqual(tick3.AskPrice, quoteBar.Ask.Close, "Ask Close incorrect");
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.Close, "Bid Close incorrect");

            enumerator.Dispose();
        }
    }
}
