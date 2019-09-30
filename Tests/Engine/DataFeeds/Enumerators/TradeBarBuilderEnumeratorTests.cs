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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class TradeBarBuilderEnumeratorTests
    {
        [Test]
        public void AggregatesTicksIntoSecondBars()
        {
            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            var enumerator = new TradeBarBuilderEnumerator(Time.OneSecond, TimeZones.NewYork, timeProvider, false);

            // noon new york time
            var currentTime = new DateTime(2015, 10, 08, 12, 0, 0);
            timeProvider.SetCurrentTime(currentTime);

            // add some ticks
            var ticks = new List<Tick>
            {
                new Tick(currentTime, Symbols.SPY, 199.55m, 199, 200) {Quantity = 10},
                new Tick(currentTime, Symbols.SPY, 199.56m, 199.21m, 200.02m) {Quantity = 5},
                new Tick(currentTime, Symbols.SPY, 199.53m, 198.77m, 199.75m) {Quantity = 20},
                new Tick(currentTime, Symbols.SPY, 198.77m, 199.75m) {Quantity = 0},
                new Tick(currentTime, Symbols.SPY, 199.73m, 198.77m, 199.75m) {Quantity = 20},
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

            // in the spirit of not duplicating the above code 5 times (OHLCV, we'll assert these here as well)
            var bar = (TradeBar) enumerator.Current;
            Assert.AreEqual(currentTime.AddSeconds(-1), bar.Time);
            Assert.AreEqual(currentTime, bar.EndTime);
            Assert.AreEqual(Symbols.SPY, bar.Symbol);
            Assert.AreEqual(ticks.First().LastPrice, bar.Open);
            Assert.AreEqual(ticks.Max(x => x.LastPrice), bar.High);
            Assert.AreEqual(ticks.Min(x => x.LastPrice), bar.Low);
            Assert.AreEqual(ticks.Last().LastPrice, bar.Close);
            Assert.AreEqual(ticks.Sum(x => x.Quantity), bar.Volume);

            enumerator.Dispose();
        }

        [Test]
        public void AggregatesHourlyTicksIntoHourlyBars()
        {
            var timeProvider = new ManualTimeProvider(TimeZones.NewYork);
            var enumerator = new TradeBarBuilderEnumerator(Time.OneHour, TimeZones.NewYork, timeProvider, false);

            // noon new york time
            var currentTime = new DateTime(2015, 10, 08, 12, 0, 0);
            timeProvider.SetCurrentTime(currentTime);

            var price = 0m;

            for (var i = 0; i < 5; i++)
            {
                // advance one hour
                currentTime = currentTime.AddHours(1);
                timeProvider.SetCurrentTime(currentTime);

                // we advanced time, a new bar should be generated
                Assert.IsTrue(enumerator.MoveNext());

                // the first loop no trade bar was generated yet, end time not reached
                if (i > 0)
                {
                    Assert.IsNotNull(enumerator.Current);

                    var bar = (TradeBar) enumerator.Current;
                    Assert.AreEqual(currentTime.AddHours(-1), bar.Time);
                    Assert.AreEqual(currentTime, bar.EndTime);
                    Assert.AreEqual(price, bar.Open);
                    Assert.AreEqual(price, bar.High);
                    Assert.AreEqual(price, bar.Low);
                    Assert.AreEqual(price, bar.Close);
                }

                // add a tick, will generate a new bar
                price++;
                enumerator.ProcessData(new Tick(currentTime, Symbols.SPY, price, price, price));
            }

            enumerator.Dispose();
        }
    }
}
