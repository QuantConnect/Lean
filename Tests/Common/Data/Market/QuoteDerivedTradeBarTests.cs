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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class QuoteDerivedTradeBarTests
    {
        private static readonly DateTime Time = new(2014, 5, 5, 10, 0, 0);

        private static QuoteBar CreateQuoteBar(DateTime time)
        {
            var bid = new Bar(1m, 2m, 0.5m, 1.5m);
            var ask = new Bar(1.1m, 2.1m, 0.6m, 1.7m);
            return new QuoteBar(time, Symbols.EURUSD, bid, 0, ask, 0, QuantConnect.Time.OneMinute);
        }

        [Test]
        public void CollapsesQuoteBarIntoMidPointPrices()
        {
            var bar = new QuoteDerivedTradeBar(CreateQuoteBar(Time));

            Assert.AreEqual(Symbols.EURUSD, bar.Symbol);
            Assert.AreEqual(Time, bar.Time);
            Assert.AreEqual(QuantConnect.Time.OneMinute, bar.Period);
            Assert.AreEqual(MarketDataType.TradeBar, bar.DataType);
            Assert.AreEqual(1.05m, bar.Open);
            Assert.AreEqual(2.05m, bar.High);
            Assert.AreEqual(0.55m, bar.Low);
            Assert.AreEqual(1.6m, bar.Close);
            Assert.AreEqual(1.6m, bar.Value);
        }

        [Test]
        public void VolumeAccessThrows()
        {
            var bar = new QuoteDerivedTradeBar(CreateQuoteBar(Time));

            var exception = Assert.Throws<InvalidOperationException>(() => _ = bar.Volume);
            StringAssert.Contains("EURUSD", exception.Message);
            StringAssert.Contains("no volume", exception.Message);
            Assert.Throws<InvalidOperationException>(() => bar.Volume = 1);
        }

        [Test]
        public void ClonesKeepTheTypeWithoutTouchingVolume()
        {
            var bar = new QuoteDerivedTradeBar(CreateQuoteBar(Time));

            var clone = bar.Clone();
            Assert.IsInstanceOf<QuoteDerivedTradeBar>(clone);
            Assert.AreEqual(bar.Close, clone.Value);

            var fillForward = bar.Clone(fillForward: true);
            Assert.IsInstanceOf<QuoteDerivedTradeBar>(fillForward);
            Assert.IsTrue(fillForward.IsFillForward);
            Assert.AreEqual(bar.Close, fillForward.Value);
        }

        [Test]
        public void ToStringOmitsVolume()
        {
            var bar = new QuoteDerivedTradeBar(CreateQuoteBar(Time));

            string text = null;
            Assert.DoesNotThrow(() => text = bar.ToString());
            StringAssert.Contains("C: 1.6", text);
            StringAssert.DoesNotContain("V:", text);
        }

        [Test]
        public void TradeBarConsolidatorKeepsConsolidatedBarsWithoutVolume()
        {
            using var consolidator = new TradeBarConsolidator(TimeSpan.FromHours(1));
            TradeBar consolidated = null;
            consolidator.DataConsolidated += (_, bar) => consolidated = bar;

            consolidator.Update(new QuoteDerivedTradeBar(Time, Symbols.EURUSD, 1m, 2m, 0.5m, 1.5m));
            consolidator.Update(new QuoteDerivedTradeBar(Time.AddMinutes(1), Symbols.EURUSD, 1.5m, 3m, 1m, 2.5m));
            // the working bar is cloned on access and must not read the volume either
            Assert.IsInstanceOf<QuoteDerivedTradeBar>(consolidator.WorkingData);
            consolidator.Update(new QuoteDerivedTradeBar(Time.AddHours(1), Symbols.EURUSD, 2.5m, 2.6m, 2.4m, 2.5m));

            Assert.IsInstanceOf<QuoteDerivedTradeBar>(consolidated);
            Assert.AreEqual(1m, consolidated.Open);
            Assert.AreEqual(3m, consolidated.High);
            Assert.AreEqual(0.5m, consolidated.Low);
            Assert.AreEqual(2.5m, consolidated.Close);
            Assert.AreEqual(TimeSpan.FromHours(1), consolidated.Period);
            Assert.Throws<InvalidOperationException>(() => _ = consolidated.Volume);
        }
    }
}
