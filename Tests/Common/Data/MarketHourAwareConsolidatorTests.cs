/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2024 QuantConnect Corporation.
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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Data.Common;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class MarketHourAwareConsolidatorTests
    {
        [Test]
        public void MarketAlwaysOpen()
        {
            var symbol = Symbols.BTCUSD;
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 5, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 100 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddHours(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 2 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 15, 15, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 3 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 14, 0, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(time, latestBar.EndTime);
            Assert.AreEqual(time.AddDays(-1), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(100, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Daily(bool strictEndTime)
        {
            var symbol = strictEndTime ? Symbols.SPX : Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidator(strictEndTime, Resolution.Daily, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 5, 0, 0);
            // this bar will be ignored because it's during market closed hours
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 100 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddHours(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 2 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 15, 15, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 3 });

            Assert.IsNull(latestBar);

            time = strictEndTime ? time : new DateTime(2015, 04, 14, 0, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 15, 15, 0) : time, latestBar.EndTime);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 8, 30, 0) : time.AddDays(-1), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(3, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DailyExtendedMarketHours(bool strictEndTime)
        {
            var symbol = strictEndTime ? Symbols.SPX : Symbols.SPY;
            using var consolidator = new MarketHourAwareConsolidatorTest(Resolution.Daily, typeof(TradeBar), TickType.Trade, true);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 8, 31, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 10 });

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 15 });

            Assert.IsNull(latestBar);

            if (!strictEndTime)
            {
                time = new DateTime(2015, 04, 13, 18, 15, 0);
                consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = symbol, High = 20 });

                Assert.IsNull(latestBar);
            }

            time = new DateTime(2015, 04, 13, 20, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 15, 15, 0) : time, latestBar.EndTime);
            Assert.AreEqual(strictEndTime ? new DateTime(2015, 04, 13, 8, 30, 0) : new DateTime(2015, 04, 13, 4, 0, 0), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(strictEndTime ? 15 : 20, latestBar.High);
            Assert.AreEqual(10, latestBar.Low);
        }

        [Test]
        public void MarketHoursRespected()
        {
            using var consolidator = new MarketHourAwareConsolidator(true, Resolution.Hour, typeof(TradeBar), TickType.Trade, false);
            var consolidatedBarsCount = 0;
            TradeBar latestBar = null;

            consolidator.DataConsolidated += (sender, bar) =>
            {
                latestBar = (TradeBar)bar;
                consolidatedBarsCount++;
            };

            var time = new DateTime(2015, 04, 13, 9, 0, 0);
            // this bar will be ignored because it's during market closed hours
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 100 });

            time = new DateTime(2015, 04, 13, 9, 31, 0);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 1 });

            Assert.IsNull(latestBar);

            time = time.AddMinutes(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 2 });

            Assert.IsNull(latestBar);

            time = time.AddMinutes(2);
            consolidator.Update(new TradeBar() { Time = time.Subtract(Time.OneMinute), Period = Time.OneMinute, Symbol = Symbols.SPY, High = 3 });

            Assert.IsNull(latestBar);

            time = new DateTime(2015, 04, 13, 10, 0, 0);
            consolidator.Scan(time);

            // Assert that the bar emitted
            Assert.IsNotNull(latestBar);
            Assert.AreEqual(time, latestBar.EndTime);
            Assert.AreEqual(new DateTime(2015, 04, 13, 9, 0, 0), latestBar.Time);
            Assert.AreEqual(1, consolidatedBarsCount);
            Assert.AreEqual(3, latestBar.High);
            Assert.AreEqual(1, latestBar.Low);
        }

        private class MarketHourAwareConsolidatorTest : MarketHourAwareConsolidator
        {
            public MarketHourAwareConsolidatorTest(Resolution resolution, Type dataType, TickType tickType, bool extendedMarketHours)
                : base(true, resolution, dataType, tickType, extendedMarketHours)
            {
            }

            protected override bool UseStrictEndTime(Symbol symbol)
            {
                return true;
            }
        }
    }
}
