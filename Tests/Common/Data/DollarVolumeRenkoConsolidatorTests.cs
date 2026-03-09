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
using Common.Data.Consolidators;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DollarVolumeRenkoConsolidatorTests : BaseConsolidatorTests
    {
        protected override IDataConsolidator CreateConsolidator()
        {
            return new DollarVolumeRenkoConsolidator(10m);
        }

        [Test]
        public void OutputTypeIsVolumeRenkoBar()
        {
            using var consolidator = new DollarVolumeRenkoConsolidator(10);
            Assert.AreEqual(typeof(VolumeRenkoBar), consolidator.OutputType);
        }

        [Test]
        public void ConsolidatesOnTickDollarVolumeReached()
        {
            VolumeRenkoBar bar = null;
            using var consolidator = new DollarVolumeRenkoConsolidator(100m); // $100 bar size
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var startTime = new DateTime(2023, 1, 1);

            // Price: $10, Quantity: 7 -> $70 dollar volume
            consolidator.Update(new Tick(startTime, Symbols.AAPL, "", "", 7m, 10m));
            Assert.IsNull(bar);

            // Price: $3, Quantity: 20 -> $60 dollar volume (total $110)
            consolidator.Update(new Tick(startTime.AddHours(1), Symbols.AAPL, "", "", 20m, 3m));
            Assert.IsNotNull(bar);

            // Verify bar properties
            Assert.AreEqual(10m, bar.Open);
            Assert.AreEqual(10m, bar.High);
            Assert.AreEqual(3m, bar.Low);
            Assert.AreEqual(3m, bar.Close);
            Assert.AreEqual(100m, bar.Volume);
            Assert.AreEqual(100m, bar.BrickSize);
            Assert.AreEqual(Symbols.AAPL, bar.Symbol);
            Assert.AreEqual(startTime, bar.Start);
            Assert.AreEqual(startTime.AddHours(1), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void ConsolidatesOnTradeBarDollarVolumeReached()
        {
            VolumeRenkoBar bar = null;
            using var consolidator = new DollarVolumeRenkoConsolidator(200m); // $200 bar size
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                bar = consolidated;
            };

            var startTime = new DateTime(2023, 1, 1);

            // Close: $11   Volume: 10 -> Dollar volume: $100
            consolidator.Update(new TradeBar(startTime, Symbols.AAPL, 10m, 12m, 9m, 11m, 10m, TimeSpan.FromHours(1)));
            Assert.IsNull(bar);

            // Close: $21   Volume: 6 -> Dollar volume: $126 (total $226)
            consolidator.Update(new TradeBar(startTime.AddHours(1), Symbols.AAPL, 20m, 22m, 18m, 21m, 6m, TimeSpan.FromHours(1)));
            Assert.IsNotNull(bar);

            // Verify bar properties
            Assert.AreEqual(10m, bar.Open);
            Assert.AreEqual(22m, bar.High);
            Assert.AreEqual(9m, bar.Low);
            Assert.AreEqual(21m, bar.Close);
            Assert.AreEqual(200m, bar.Volume);
            Assert.AreEqual(200m, bar.BrickSize);
            Assert.AreEqual(Symbols.AAPL, bar.Symbol);
            Assert.AreEqual(startTime, bar.Start);
            Assert.AreEqual(startTime.AddHours(2), bar.EndTime);
            Assert.IsTrue(bar.IsClosed);
        }

        [Test]
        public void HandlesMultipleConsolidations()
        {
            var consolidatedBars = new List<VolumeRenkoBar>();
            using var consolidator = new DollarVolumeRenkoConsolidator(100m);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                consolidatedBars.Add(consolidated);
            };

            var startTime = new DateTime(2023, 1, 1);

            // First bar: $50 + $60 = $110
            consolidator.Update(new Tick(startTime, Symbols.AAPL, "", "", 10m, 5m));
            consolidator.Update(new Tick(startTime.AddHours(1), Symbols.AAPL, "", "", 20m, 3m));

            // Second bar: $80 + $30 = $110 (total $220)
            consolidator.Update(new Tick(startTime.AddHours(2), Symbols.AAPL, "", "", 20m, 4m));
            consolidator.Update(new Tick(startTime.AddHours(3), Symbols.AAPL, "", "", 20m, 1.5m));

            Assert.AreEqual(2, consolidatedBars.Count);

            // Verify first bar
            Assert.AreEqual(5m, consolidatedBars[0].Open);
            Assert.AreEqual(3m, consolidatedBars[0].Close);
            Assert.AreEqual(100m, consolidatedBars[0].Volume);
            Assert.IsTrue(consolidatedBars[0].IsClosed);

            // Verify second bar
            Assert.AreEqual(3m, consolidatedBars[1].Open);
            Assert.AreEqual(1.5m, consolidatedBars[1].Close);
            Assert.AreEqual(100m, consolidatedBars[1].Volume);
            Assert.IsTrue(consolidatedBars[1].IsClosed);
        }

        [Test]
        public void ThrowsOnNonTradeData()
        {
            using var consolidator = new DollarVolumeRenkoConsolidator(100m);
            var startTime = new DateTime(2023, 1, 1);

            Assert.Throws<ArgumentException>(() =>
                consolidator.Update(new QuoteBar(
                    startTime,
                    Symbols.AAPL,
                    new Bar(1m, 1m, 1m, 1m),
                    1m,
                    new Bar(1m, 1m, 1m, 1m),
                    1m,
                    TimeSpan.FromHours(1)
                ))
            );
        }

        protected override IEnumerable<IBaseData> GetTestValues()
        {
            var time = new DateTime(2023, 1, 1);
            return new List<Tick>()
            {
                new Tick(time, Symbols.AAPL, "", "", 10m, 5m),   // $50
                new Tick(time.AddSeconds(1), Symbols.AAPL, "", "", 12m, 4m),  // $48
                new Tick(time.AddSeconds(2), Symbols.AAPL, "", "", 15m, 2m),  // $30
                new Tick(time.AddSeconds(3), Symbols.AAPL, "", "", 14m, 3m),   // $42
                new Tick(time.AddSeconds(4), Symbols.AAPL, "", "", 16m, 5m),  // $80
                new Tick(time.AddSeconds(5), Symbols.AAPL, "", "", 18m, 3m),   // $54
                new Tick(time.AddSeconds(6), Symbols.AAPL, "", "", 17m, 4m),  // $68
                new Tick(time.AddSeconds(7), Symbols.AAPL, "", "", 19m, 2m),   // $38
                new Tick(time.AddSeconds(8), Symbols.AAPL, "", "", 20m, 6m),  // $120
                new Tick(time.AddSeconds(9), Symbols.AAPL, "", "", 22m, 3m),   // $66
                new Tick(time.AddSeconds(10), Symbols.AAPL, "", "", 21m, 4m), // $84
                new Tick(time.AddSeconds(11), Symbols.AAPL, "", "", 23m, 5m), // $115
                new Tick(time.AddSeconds(12), Symbols.AAPL, "", "", 25m, 2m)   // $50
            };
        }
    }
}
