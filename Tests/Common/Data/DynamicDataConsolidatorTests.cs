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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DynamicDataConsolidatorTests
    {
        [Test]
        public void AggregatesTimeValuePairsWithOutVolumeProperly()
        {
            TradeBar newTradeBar = null;
            var consolidator = new DynamicDataConsolidator(4);
            consolidator.DataConsolidated += (sender, tradeBar) =>
            {
                newTradeBar = tradeBar;
            };
            var reference = DateTime.Today;
            var bar1 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference,
                Value = 5
            };
            consolidator.Update(bar1);
            Assert.IsNull(newTradeBar);

            var bar2 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(1),
                Value = 10
            };
            consolidator.Update(bar2);
            Assert.IsNull(newTradeBar);
            var bar3 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(2),
                Value = 1
            };
            consolidator.Update(bar3);
            Assert.IsNull(newTradeBar);

            var bar4 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(3),
                Value = 9
            };
            consolidator.Update(bar4);
            Assert.IsNotNull(newTradeBar);

            Assert.AreEqual(Symbols.SPY, newTradeBar.Symbol);
            Assert.AreEqual(bar1.Time, newTradeBar.Time);
            Assert.AreEqual(bar1.Value, newTradeBar.Open);
            Assert.AreEqual(bar2.Value, newTradeBar.High);
            Assert.AreEqual(bar3.Value, newTradeBar.Low);
            Assert.AreEqual(bar4.Value, newTradeBar.Close);
            Assert.AreEqual(0, newTradeBar.Volume);
            Assert.AreEqual(bar4.EndTime, newTradeBar.EndTime);
        }

        [Test]
        public void AggregatesTimeValuePairsWithVolumeProperly()
        {
            TradeBar newTradeBar = null;
            var consolidator = new DynamicDataConsolidator(4);
            consolidator.DataConsolidated += (sender, tradeBar) =>
            {
                newTradeBar = tradeBar;
            };
            var reference = DateTime.Today;
            dynamic bar1 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference,
                Value = 5,
            };
            bar1.Volume = 75L;

            consolidator.Update(bar1);
            Assert.IsNull(newTradeBar);

            dynamic bar2 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(1),
                Value = 10
            };
            bar2.Volume = 100L;

            consolidator.Update(bar2);
            Assert.IsNull(newTradeBar);
            dynamic bar3 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(2),
                Value = 1
            };
            bar3.Volume = 115L;

            consolidator.Update(bar3);
            Assert.IsNull(newTradeBar);

            dynamic bar4 = new CustomData
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(3),
                Value = 9
            };
            bar4.Volume = 85L;

            consolidator.Update(bar4);
            Assert.IsNotNull(newTradeBar);

            Assert.AreEqual(Symbols.SPY, newTradeBar.Symbol);
            Assert.AreEqual(bar1.Time, newTradeBar.Time);
            Assert.AreEqual(bar1.Value, newTradeBar.Open);
            Assert.AreEqual(bar2.Value, newTradeBar.High);
            Assert.AreEqual(bar3.Value, newTradeBar.Low);
            Assert.AreEqual(bar4.Value, newTradeBar.Close);
            Assert.AreEqual(bar1.Volume + bar2.Volume + bar3.Volume + bar4.Volume, newTradeBar.Volume);
        }

        [Test]
        public void AggregatesTradeBarsWithVolumeProperly()
        {
            TradeBar consolidated = null;
            var consolidator = new DynamicDataConsolidator(3);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = DateTime.Today;
            dynamic bar1 = new CustomData();
            bar1.Symbol = Symbols.SPY;
            bar1.Time = reference;
            bar1.Open = 10;
            bar1.High = 100m;
            bar1.Low = 1m;
            bar1.Close = 50m;
            bar1.Volume = 75L;

            dynamic bar2 = new CustomData();
            bar2.Symbol = Symbols.SPY;
            bar2.Time = reference.AddHours(1);
            bar2.Open = 50m;
            bar2.High = 123m;
            bar2.Low = 35m;
            bar2.Close = 75m;
            bar2.Volume = 100L;

            dynamic bar3 = new CustomData();
            bar3.Symbol = Symbols.SPY;
            bar3.Time = reference.AddHours(1);
            bar3.Open = 75m;
            bar3.High = 100m;
            bar3.Low = 50m;
            bar3.Close = 83m;
            bar3.Volume = 125L;

            consolidator.Update(bar1);
            Assert.IsNull(consolidated);

            consolidator.Update(bar2);
            Assert.IsNull(consolidated);

            consolidator.Update(bar3);

            Assert.IsNotNull(consolidated);
            Assert.AreEqual(Symbols.SPY, consolidated.Symbol);
            Assert.AreEqual(bar1.Open, consolidated.Open);
            Assert.AreEqual(Math.Max(bar1.High, Math.Max(bar2.High, bar3.High)), consolidated.High);
            Assert.AreEqual(Math.Min(bar1.Low, Math.Min(bar2.Low, bar3.Low)), consolidated.Low);
            Assert.AreEqual(bar3.Close, consolidated.Close);
            Assert.AreEqual(bar1.Volume + bar2.Volume + bar3.Volume, consolidated.Volume);
        }

        [Test]
        public void AggregatesTradeBarsWithOutVolumeProperly()
        {
            TradeBar consolidated = null;
            var consolidator = new DynamicDataConsolidator(3);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                consolidated = bar;
            };

            var reference = DateTime.Today;
            dynamic bar1 = new CustomData();
            bar1.Symbol = Symbols.SPY;
            bar1.Time = reference;
            bar1.Open = 10;
            bar1.High = 100m;
            bar1.Low = 1m;
            bar1.Close = 50m;

            dynamic bar2 = new CustomData();
            bar2.Symbol = Symbols.SPY;
            bar2.Time = reference.AddHours(1);
            bar2.Open = 50m;
            bar2.High = 123m;
            bar2.Low = 35m;
            bar2.Close = 75m;

            dynamic bar3 = new CustomData();
            bar3.Symbol = Symbols.SPY;
            bar3.Time = reference.AddHours(1);
            bar3.Open = 75m;
            bar3.High = 100m;
            bar3.Low = 50m;
            bar3.Close = 83m;

            consolidator.Update(bar1);
            Assert.IsNull(consolidated);

            consolidator.Update(bar2);
            Assert.IsNull(consolidated);

            consolidator.Update(bar3);

            Assert.IsNotNull(consolidated);
            Assert.AreEqual(Symbols.SPY, consolidated.Symbol);
            Assert.AreEqual(bar1.Open, consolidated.Open);
            Assert.AreEqual(Math.Max(bar1.High, Math.Max(bar2.High, bar3.High)), consolidated.High);
            Assert.AreEqual(Math.Min(bar1.Low, Math.Min(bar2.Low, bar3.Low)), consolidated.Low);
            Assert.AreEqual(bar3.Close, consolidated.Close);
            Assert.AreEqual(0, consolidated.Volume);
        }

        private class CustomData : DynamicData
        {
            public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
            {
                throw new NotImplementedException();
            }

            public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                throw new NotImplementedException();
            }
        }
    }
}
