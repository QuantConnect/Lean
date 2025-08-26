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
using Common.Data.Consolidators;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SessionConsolidatorTests
    {
        [Test]
        public void CalculatesOHLCVRespectingMarketHours()
        {
            using var consolidator = new SessionConsolidator(true, typeof(TradeBar), TickType.Trade);

            var symbol = Symbols.SPY;
            var date = new DateTime(2025, 8, 25);

            var tradeBar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1));
            var tradeBar2 = new TradeBar(date.AddHours(13), symbol, 101, 102, 100, 101.5m, 1100, TimeSpan.FromHours(1));
            var tradeBar3 = new TradeBar(date.AddHours(14), symbol, 102, 103, 101, 102.5m, 1200, TimeSpan.FromHours(1));
            consolidator.Update(tradeBar1);
            consolidator.Update(tradeBar2);
            consolidator.Update(tradeBar3);

            var eventTime = new DateTime(2025, 8, 25, 16, 0, 0);
            // This should not fire the consolidator, it must be strictly after market close
            consolidator.ValidateAndScan(eventTime, true);
            Assert.IsNull(consolidator.Consolidated);

            var bar4 = new TradeBar(date.AddHours(15), symbol, 103, 104, 102, 103.5m, 1300, TimeSpan.FromHours(1));
            // Updates should fire the consolidator
            consolidator.Update(bar4);
            Assert.IsNotNull(consolidator.Consolidated);
            var consolidated = (TradeBar)consolidator.Consolidated;
            Assert.AreEqual(100, consolidated.Open);
            Assert.AreEqual(104, consolidated.High);
            Assert.AreEqual(99, consolidated.Low);
            Assert.AreEqual(103.5, consolidated.Close);
            Assert.AreEqual(4600, consolidated.Volume);
        }

        [Test]
        public void TracksOpenInterestFromOpenInterestTicks()
        {
            using var consolidator = new SessionConsolidator(true, typeof(Tick), TickType.Quote);

            var symbol = Symbols.SPY;
            var date = new DateTime(2025, 8, 25);

            var openInterest = new Tick(date.AddHours(12), symbol, 5);
            var tick1 = new Tick(date.AddHours(12), symbol, 100, 101);
            var tick2 = new Tick(date.AddHours(13), symbol, 101, 102);
            var tick3 = new Tick(date.AddHours(17), symbol, 102, 103);

            consolidator.Update(openInterest);
            consolidator.Update(tick1);
            consolidator.Update(tick2);
            consolidator.Update(tick3);

            Assert.IsNotNull(consolidator.Consolidated);
            var consolidated = (QuoteBar)consolidator.Consolidated;
            Assert.AreEqual(consolidator.OpenInterest, 5);
            Assert.AreEqual(0, consolidator.Volume);
            Assert.AreEqual(100.5, consolidated.Open);
            Assert.AreEqual(101.5, consolidated.High);
            Assert.AreEqual(100.5, consolidated.Low);
            Assert.AreEqual(101.5, consolidated.Close);
        }

        [Test]
        public void AccumulatesVolumeFromTradeBarsAndTradeTicksCorrectly()
        {
            using var consolidator = new SessionConsolidator(true, typeof(QuoteBar), TickType.Quote);

            var symbol = Symbols.SPY;
            var date = new DateTime(2025, 8, 25);

            // QuoteBars will be processed normally
            var quoteBar1 = new QuoteBar(date.AddHours(11), symbol, new Bar(100, 101, 100, 101), 0, new Bar(101, 102, 100, 101), 0);
            var quoteBar2 = new QuoteBar(date.AddHours(12), symbol, new Bar(100, 101, 100, 101), 0, new Bar(101, 102, 100, 101), 0);
            consolidator.Update(quoteBar1);
            consolidator.Update(quoteBar2);

            // We will handle the volume manually for trade bars and ticks(trade)

            // We will take the volume (1000) from the trade bar
            var tradeBar = new TradeBar(date.AddHours(13), symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1));
            consolidator.Update(tradeBar);
            // This should be ignored because it does not have the resolution of the first data
            var tick1 = new Tick(date.AddHours(14), symbol, "", "", 500, 5);
            consolidator.Update(tick1);
            // This should be ignored, because is not within market hours
            var tick2 = new Tick(date.AddHours(20), symbol, 500, 5);
            consolidator.Update(tick2);
            // This tick has a TickType of Quote, so it should be ignored
            var tick3 = new Tick(date.AddHours(14), symbol, 102, 103);
            consolidator.Update(tick3);

            Assert.IsNotNull(consolidator.Consolidated);
            var consolidated = (QuoteBar)consolidator.Consolidated;
            Assert.AreEqual(1000, consolidator.Volume);
            Assert.AreEqual(consolidated.Open, 100.5);
            Assert.AreEqual(consolidated.High, 101.5);
            Assert.AreEqual(consolidated.Low, 100);
            Assert.AreEqual(consolidated.Close, 101);
        }

        [TestCase(Resolution.Tick)]
        [TestCase(Resolution.Second)]
        [TestCase(Resolution.Minute)]
        [TestCase(Resolution.Hour)]
        public void AccumulatesVolumeOnlyFromSameResolution(Resolution resolution)
        {
            using var consolidator = new SessionConsolidator(true, typeof(QuoteBar), TickType.Quote);

            var symbol = Symbols.SPY;
            var date = new DateTime(2025, 8, 25, 10, 0, 0);

            // First data sets the resolution baseline
            BaseData first = resolution switch
            {
                Resolution.Tick => new Tick(date, symbol, "", "", 500, 5),
                Resolution.Second => new TradeBar(date, symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromSeconds(1)),
                Resolution.Minute => new TradeBar(date, symbol, 100, 101, 99, 100.5m, 2000, TimeSpan.FromMinutes(1)),
                Resolution.Hour => new TradeBar(date, symbol, 100, 101, 99, 100.5m, 3000, TimeSpan.FromHours(1)),
                _ => null
            };
            consolidator.Update(first);

            // Wrong resolution should be ignored
            var wrongResolution = resolution switch
            {
                Resolution.Tick => TimeSpan.FromSeconds(1),
                Resolution.Second => TimeSpan.FromHours(1),
                Resolution.Minute => TimeSpan.FromSeconds(1),
                Resolution.Hour => TimeSpan.FromTicks(1),
                _ => TimeSpan.FromDays(1)
            };
            consolidator.Update(new TradeBar(date.AddMinutes(1), symbol, 100, 101, 99, 100.5m, 999, wrongResolution));

            // Same resolution should be processed
            BaseData sameResolution = resolution switch
            {
                Resolution.Tick => new Tick(date.AddSeconds(1), symbol, "", "", 600, 6),
                Resolution.Second => new TradeBar(date.AddSeconds(1), symbol, 100, 101, 99, 100.5m, 2000, TimeSpan.FromSeconds(1)),
                Resolution.Minute => new TradeBar(date.AddMinutes(1), symbol, 100, 101, 99, 100.5m, 4000, TimeSpan.FromMinutes(1)),
                Resolution.Hour => new TradeBar(date.AddHours(1), symbol, 100, 101, 99, 100.5m, 5000, TimeSpan.FromHours(1)),
                _ => null
            };
            consolidator.Update(sameResolution);

            var expected = resolution switch
            {
                Resolution.Tick => 1100,
                Resolution.Second => 3000,
                Resolution.Minute => 6000,
                Resolution.Hour => 8000,
                _ => 0
            };

            Assert.AreEqual(expected, consolidator.Volume);
        }
    }
}
