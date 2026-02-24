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
using Common.Data.Consolidators;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class SessionConsolidatorTests
    {
        [Test]
        public void CalculatesOHLCVRespectingMarketHours()
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(TickType.Trade);

            var date = new DateTime(2025, 8, 25);

            var tradeBar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1));
            var tradeBar2 = new TradeBar(date.AddHours(13), symbol, 101, 102, 100, 101.5m, 1100, TimeSpan.FromHours(1));
            var tradeBar3 = new TradeBar(date.AddHours(14), symbol, 102, 103, 101, 102.5m, 1200, TimeSpan.FromHours(1));
            consolidator.Update(tradeBar1);
            consolidator.Update(tradeBar2);
            consolidator.Update(tradeBar3);

            var eventTime = new DateTime(2025, 8, 26, 0, 0, 0);
            // This should fire the scan, because is the end of the day
            consolidator.ValidateAndScan(eventTime);

            Assert.IsNotNull(consolidator.Consolidated);
            var consolidated = (SessionBar)consolidator.Consolidated;
            Assert.AreEqual(100, consolidated.Open);
            Assert.AreEqual(103, consolidated.High);
            Assert.AreEqual(99, consolidated.Low);
            Assert.AreEqual(102.5, consolidated.Close);
            Assert.AreEqual(3300, consolidated.Volume);
        }

        [Test]
        public void TracksOpenInterestFromOpenInterestTicks()
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(TickType.Quote);

            var date = new DateTime(2025, 8, 25);

            var openInterest = new Tick(date.AddHours(12), symbol, 5);
            var tick1 = new Tick(date.AddHours(12), symbol, 100, 101);
            var tick2 = new Tick(date.AddHours(13), symbol, 101, 102);
            var tick3 = new Tick(date.AddHours(14), symbol, 102, 103);

            consolidator.Update(openInterest);
            consolidator.Update(tick1);
            consolidator.Update(tick2);
            consolidator.Update(tick3);

            var workingData = (SessionBar)consolidator.WorkingData;
            Assert.AreEqual(5, workingData.OpenInterest);
            Assert.AreEqual(0, workingData.Volume);
            Assert.AreEqual(100.5, workingData.Open);
            Assert.AreEqual(102.5, workingData.High);
            Assert.AreEqual(100.5, workingData.Low);
            Assert.AreEqual(102.5, workingData.Close);
        }

        [Test]
        public void AccumulatesVolumeFromTradeBarsAndTradeTicksCorrectly()
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(TickType.Quote);

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
            // We will take the quantity (500) from the tick
            var tick1 = new Tick(date.AddHours(14), symbol, "", "", 500, 5);
            consolidator.Update(tick1);

            var workingData = (SessionBar)consolidator.WorkingData;
            Assert.AreEqual(1500, workingData.Volume);
            Assert.AreEqual(100.5, workingData.Open);
            Assert.AreEqual(101.5, workingData.High);
            Assert.AreEqual(100, workingData.Low);
            Assert.AreEqual(101, workingData.Close);
        }

        [Test]
        public void AccumulatesVolumeCorrectlyAfterReset()
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(TickType.Quote);

            var date = new DateTime(2025, 8, 25, 0, 0, 0);

            // Resolution = Hour, accumulates normally
            var tradeBar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1));
            var tradeBar2 = new TradeBar(date.AddHours(13), symbol, 101, 102, 100, 101.5m, 1100, TimeSpan.FromHours(1));
            consolidator.Update(tradeBar1);
            consolidator.Update(tradeBar2);

            Assert.AreEqual(2100, ((SessionBar)consolidator.WorkingData).Volume);

            consolidator.Reset();

            tradeBar1 = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100.5m, 2000, TimeSpan.FromMinutes(1));
            tradeBar2 = new TradeBar(date.AddHours(12).AddMinutes(1), symbol, 101, 102, 100, 101.5m, 3000, TimeSpan.FromMinutes(1));

            consolidator.Update(tradeBar1);
            consolidator.Update(tradeBar2);

            Assert.AreEqual(5000, ((SessionBar)consolidator.WorkingData).Volume);
        }

        [Test]
        public void PreservesSymbolAfterConsolidation()
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(TickType.Trade);

            var date = new DateTime(2025, 8, 25);
            var tradeBar = new TradeBar(date.AddHours(12), symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1));
            consolidator.Update(tradeBar);
            Assert.AreEqual(symbol, consolidator.WorkingData.Symbol);

            var eventTime = new DateTime(2025, 8, 26, 0, 0, 0);
            // This should fire the scan, because is the end of the day
            consolidator.ValidateAndScan(eventTime);
            Assert.AreEqual(symbol, consolidator.Consolidated.Symbol);
        }

        [TestCase(TickType.Trade, Resolution.Tick, Resolution.Second)]
        [TestCase(TickType.Trade, Resolution.Tick, Resolution.Minute)]
        [TestCase(TickType.Trade, Resolution.Tick, Resolution.Hour)]
        [TestCase(TickType.Trade, Resolution.Second, Resolution.Minute)]
        [TestCase(TickType.Trade, Resolution.Second, Resolution.Hour)]
        [TestCase(TickType.Trade, Resolution.Minute, Resolution.Hour)]
        [TestCase(TickType.Quote, Resolution.Tick, Resolution.Second)]
        [TestCase(TickType.Quote, Resolution.Tick, Resolution.Minute)]
        [TestCase(TickType.Quote, Resolution.Tick, Resolution.Hour)]
        [TestCase(TickType.Quote, Resolution.Second, Resolution.Minute)]
        [TestCase(TickType.Quote, Resolution.Second, Resolution.Hour)]
        [TestCase(TickType.Quote, Resolution.Minute, Resolution.Hour)]
        public void IgnoresOverlappingHigherResolutionData(TickType tickType, Resolution firstResolution, Resolution secondResolution)
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(tickType);


            var currentTime = new DateTime(2025, 8, 25, 11, 0, 0);

            var dataDictionary = new Dictionary<(TickType, Resolution), BaseData>
            {
                { (TickType.Trade, Resolution.Tick), new Tick(currentTime, symbol, "", "", 600, 15) },
                { (TickType.Quote, Resolution.Tick), new Tick(currentTime, symbol, 100, 101) },
                { (TickType.Trade, Resolution.Second), new TradeBar(currentTime, symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromSeconds(1)) },
                { (TickType.Quote, Resolution.Second), new QuoteBar(currentTime, symbol, new Bar(300, 301, 300, 301), 0, new Bar(300, 301, 300, 301), 0, TimeSpan.FromSeconds(1)) },
                { (TickType.Trade, Resolution.Minute), new TradeBar(currentTime, symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromMinutes(1)) },
                { (TickType.Quote, Resolution.Minute), new QuoteBar(currentTime, symbol, new Bar(300, 301, 300, 301), 0, new Bar(300, 301, 300, 301), 0, TimeSpan.FromMinutes(1)) },
                { (TickType.Trade, Resolution.Hour), new TradeBar(currentTime, symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1)) },
                { (TickType.Quote, Resolution.Hour), new QuoteBar(currentTime, symbol, new Bar(300, 301, 300, 301), 0, new Bar(300, 301, 300, 301), 0, TimeSpan.FromHours(1)) }
            };

            // First update with lower-resolution data (should be accepted)
            var firstData = dataDictionary[(tickType, firstResolution)];
            firstData.Time = currentTime.AddTicks(1);
            consolidator.Update(firstData);

            var workingData = (SessionBar)consolidator.WorkingData;
            var currentTimeAfterFirstUpdate = workingData.Time;

            // Second update with higher-resolution overlapping data (should be ignored)
            var secondData = dataDictionary[(tickType, secondResolution)];
            consolidator.Update(secondData);

            workingData = (SessionBar)consolidator.WorkingData;
            var currentTimeAfterSecondUpdate = workingData.Time;

            // Verify that the higher-resolution update did not overwrite the current session state
            Assert.AreEqual(currentTimeAfterFirstUpdate, currentTimeAfterSecondUpdate);
        }

        [TestCase(TickType.Trade, true)]
        [TestCase(TickType.Trade, false)]
        [TestCase(TickType.Quote, true)]
        [TestCase(TickType.Quote, false)]
        public void ConsolidateUsingBars(TickType tickType, bool isTick)
        {
            var symbol = Symbols.SPY;
            using var consolidator = GetConsolidator(tickType);

            var date = new DateTime(2025, 8, 25, 9, 0, 0);

            var tradeBars = new List<TradeBar>
            {
                new TradeBar(date, symbol, 100, 101, 99, 100.5m, 1000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(1), symbol, 200, 201, 199, 200.5m, 2000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(2), symbol, 300, 301, 299, 300.5m, 3000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(3), symbol, 400, 401, 399, 400.5m, 4000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(4), symbol, 500, 501, 499, 500.5m, 5000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(5), symbol, 600, 601, 599, 600.5m, 6000, TimeSpan.FromHours(1)),
                new TradeBar(date.AddHours(6), symbol, 700, 701, 699, 700.5m, 7000, TimeSpan.FromHours(1))
            };

            var tradeTicks = new List<Tick>
            {
                new Tick(date.AddHours(1), symbol, "", "", 600, 15),
                new Tick(date.AddHours(2), symbol, "", "", 700, 25),
                new Tick(date.AddHours(3), symbol, "", "", 800, 35),
                new Tick(date.AddHours(4), symbol, "", "", 900, 45),
                new Tick(date.AddHours(5), symbol, "", "", 1000, 55),
                new Tick(date.AddHours(6), symbol, "", "", 1100, 65),
                new Tick(date.AddHours(7), symbol, "", "", 1200, 75)
            };

            var quoteBars = new List<QuoteBar>
            {
                new QuoteBar(date, symbol, new Bar(100, 101, 100, 101), 0, new Bar(100, 101, 100, 101), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(1), symbol, new Bar(200, 201, 200, 201), 0, new Bar(200, 201, 200, 201), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(2), symbol, new Bar(300, 301, 300, 301), 0, new Bar(300, 301, 300, 301), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(3), symbol, new Bar(400, 401, 400, 401), 0, new Bar(400, 401, 400, 401), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(4), symbol, new Bar(500, 501, 500, 501), 0, new Bar(500, 501, 500, 501), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(5), symbol, new Bar(600, 601, 600, 601), 0, new Bar(600, 601, 600, 601), 0, TimeSpan.FromHours(1)),
                new QuoteBar(date.AddHours(6), symbol, new Bar(700, 701, 700, 701), 0, new Bar(700, 701, 700, 701), 0, TimeSpan.FromHours(1))
            };

            var quoteTicks = new List<Tick>
            {
                new Tick(date.AddHours(1), symbol, 100, 101),
                new Tick(date.AddHours(2), symbol, 200, 201),
                new Tick(date.AddHours(3), symbol, 300, 301),
                new Tick(date.AddHours(4), symbol, 400, 401),
                new Tick(date.AddHours(5), symbol, 500, 501),
                new Tick(date.AddHours(6), symbol, 600, 601),
                new Tick(date.AddHours(7), symbol, 700, 701)
            };

            var dataToUpdate = tickType == TickType.Trade
                ? (isTick ? tradeTicks.Cast<BaseData>() : tradeBars.Cast<BaseData>())
                : (isTick ? quoteTicks.Cast<BaseData>() : quoteBars.Cast<BaseData>());

            foreach (var data in dataToUpdate)
            {
                consolidator.Update(data);
            }


            var eventTime = new DateTime(2025, 8, 26, 0, 0, 0);
            // This should fire the scan, because is the end of the day
            consolidator.ValidateAndScan(eventTime);

            Assert.IsNotNull(consolidator.Consolidated);
            var consolidated = (SessionBar)consolidator.Consolidated;

            var (expectedOpen, expectedHigh, expectedLow, expectedClose, expectedVolume) =
                (tickType, isTick) switch
                {
                    (TickType.Trade, true) => (15m, 65m, 15m, 65m, 5100L),
                    (TickType.Trade, false) => (100m, 701m, 99m, 700.5m, 28000L),
                    (TickType.Quote, true) => (100.5m, 600.5m, 100.5m, 600.5m, 0L),
                    (TickType.Quote, false) => (100m, 701m, 100m, 701m, 0L),
                    _ => throw new NotImplementedException()
                };

            Assert.AreEqual(expectedOpen, consolidated.Open);
            Assert.AreEqual(expectedHigh, consolidated.High);
            Assert.AreEqual(expectedLow, consolidated.Low);
            Assert.AreEqual(expectedClose, consolidated.Close);
            Assert.AreEqual(expectedVolume, consolidated.Volume);
        }

        private static SessionConsolidator GetConsolidator(TickType tickType)
        {
            var symbol = Symbols.SPY;
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            return new SessionConsolidator(exchangeHours, tickType, symbol);
        }
    }
}
