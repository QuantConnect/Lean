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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ScannableEnumeratorTests
    {
        [Test]
        public void PassesTicksStraightThrough()
        {
            var currentTime = new DateTime(2000, 01, 01);
            var enumerator = new ScannableEnumerator<Tick>(
                new IdentityDataConsolidator<Tick>(),
                DateTimeZone.ForOffset(Offset.FromHours(-5)),
                new ManualTimeProvider(currentTime),
                (s, e) => { }
            );

            // returns true even if no data present until stop is called
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick1 = new Tick(currentTime, Symbols.SPY, 199.55m, 199, 200) { Quantity = 10 };
            enumerator.Update(tick1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick1, enumerator.Current);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick2 = new Tick(currentTime, Symbols.SPY, 199.56m, 199.21m, 200.02m) { Quantity = 5 };
            enumerator.Update(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(tick2, enumerator.Current);

            enumerator.Dispose();
        }

        [Test]
        public void NewDataAvailableShouldFire()
        {
            var currentTime = new DateTime(2000, 01, 01);
            var available = false;
            var enumerator = new ScannableEnumerator<Tick>(
                new IdentityDataConsolidator<Tick>(),
                DateTimeZone.ForOffset(Offset.FromHours(-5)),
                new ManualTimeProvider(currentTime),
                (s, e) => { available = true; }
            );

            // returns true even if no data present until stop is called
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.IsFalse(available);

            var tick1 = new Tick(currentTime, Symbols.SPY, 199.55m, 199, 200) { Quantity = 10 };
            enumerator.Update(tick1);
            Assert.IsTrue(available);

            enumerator.Dispose();
        }

        [Test]
        public void AggregatesNewQuoteBarProperly()
        {
            var reference = DateTime.Today;

            var enumerator = new ScannableEnumerator<Data.BaseData>(
                new TickQuoteBarConsolidator(4),
                DateTimeZone.ForOffset(Offset.FromHours(-5)),
                new ManualTimeProvider(reference),
                (s, e) => { }
            );

            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference,
                BidPrice = 10,
                BidSize = 20,
                TickType = TickType.Quote
            };
            enumerator.Update(tick1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddMinutes(1),
                AskPrice = 20,
                AskSize = 10,
                TickType = TickType.Quote
            };

            var badTick = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddMinutes(1),
                AskPrice = 25,
                AskSize = 100,
                BidPrice = -100,
                BidSize = 2,
                Value = 50,
                Quantity = 1234,
                TickType = TickType.Trade
            };
            enumerator.Update(badTick);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            enumerator.Update(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddMinutes(2),
                BidPrice = 12,
                BidSize = 50,
                TickType = TickType.Quote
            };
            enumerator.Update(tick3);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddMinutes(3),
                AskPrice = 17,
                AskSize = 15,
                TickType = TickType.Quote
            };
            enumerator.Update(tick4);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            QuoteBar quoteBar = enumerator.Current as QuoteBar;
            Assert.IsNotNull(quoteBar);

            Assert.AreEqual(Symbols.SPY, quoteBar.Symbol);
            Assert.AreEqual(tick1.Time, quoteBar.Time);
            Assert.AreEqual(tick4.EndTime, quoteBar.EndTime);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Open);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Low);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.High);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.Close);
            Assert.AreEqual(tick3.BidSize, quoteBar.LastBidSize);

            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Open);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Low);
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.High);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Close);
            Assert.AreEqual(tick4.AskSize, quoteBar.LastAskSize);
        }

        [Test]
        public void ForceScanQuoteBar()
        {
            var reference = new DateTime(2020, 2, 2, 1, 0, 0);
            var timeProvider = new ManualTimeProvider(reference);
            var dateTimeZone = DateTimeZone.ForOffset(Offset.FromHours(-5));
            var enumerator = new ScannableEnumerator<Data.BaseData>(
                new TickQuoteBarConsolidator(TimeSpan.FromMinutes(1)),
                dateTimeZone,
                timeProvider,
                (s, e) => { }
            );

            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.ConvertFromUtc(dateTimeZone),
                BidPrice = 10,
                BidSize = 20,
                TickType = TickType.Quote
            };
            enumerator.Update(tick1);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(1).ConvertFromUtc(dateTimeZone),
                AskPrice = 20,
                AskSize = 10,
                TickType = TickType.Quote
            };

            enumerator.Update(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(2).ConvertFromUtc(dateTimeZone),
                BidPrice = 12,
                BidSize = 50,
                TickType = TickType.Quote
            };
            enumerator.Update(tick3);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddSeconds(3).ConvertFromUtc(dateTimeZone),
                AskPrice = 17,
                AskSize = 15,
                TickType = TickType.Quote
            };
            enumerator.Update(tick4);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            timeProvider.AdvanceSeconds(120);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            QuoteBar quoteBar = enumerator.Current as QuoteBar;
            Assert.IsNotNull(quoteBar);

            Assert.AreEqual(Symbols.SPY, quoteBar.Symbol);
            Assert.AreEqual(tick1.Time, quoteBar.Time);
            Assert.AreNotEqual(tick4.EndTime, quoteBar.EndTime);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Open);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Low);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.High);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.Close);
            Assert.AreEqual(tick3.BidSize, quoteBar.LastBidSize);

            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Open);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Low);
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.High);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Close);
            Assert.AreEqual(tick4.AskSize, quoteBar.LastAskSize);
        }

        [Test]
        public void MoveNextScanQuoteBar()
        {
            var offset = Offset.FromHours(-5);
            var timeZone = DateTimeZone.ForOffset(offset);
            var utc = new DateTimeOffset(DateTime.Today);
            var reference = utc.ToOffset(offset.ToTimeSpan());
            var timeProvider = new ManualTimeProvider(reference.DateTime, timeZone);

            var enumerator = new ScannableEnumerator<Data.BaseData>(
                new TickQuoteBarConsolidator(TimeSpan.FromMinutes(1)),
                timeZone,
                timeProvider,
                (s, e) => { }
            );

            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.DateTime,
                BidPrice = 10,
                BidSize = 20,
                TickType = TickType.Quote
            };
            enumerator.Update(tick1);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.DateTime.AddSeconds(1),
                AskPrice = 20,
                AskSize = 10,
                TickType = TickType.Quote
            };

            enumerator.Update(tick2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.DateTime.AddSeconds(2),
                BidPrice = 12,
                BidSize = 50,
                TickType = TickType.Quote
            };
            enumerator.Update(tick3);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            var tick4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.DateTime.AddSeconds(3),
                AskPrice = 17,
                AskSize = 15,
                TickType = TickType.Quote
            };
            enumerator.Update(tick4);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);

            timeProvider.SetCurrentTime(reference.DateTime.AddMinutes(2));

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            QuoteBar quoteBar = enumerator.Current as QuoteBar;
            Assert.IsNotNull(quoteBar);

            Assert.AreEqual(Symbols.SPY, quoteBar.Symbol);
            Assert.AreEqual(tick1.Time, quoteBar.Time);
            Assert.AreNotEqual(tick4.EndTime, quoteBar.EndTime);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Open);
            Assert.AreEqual(tick1.BidPrice, quoteBar.Bid.Low);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.High);
            Assert.AreEqual(tick3.BidPrice, quoteBar.Bid.Close);
            Assert.AreEqual(tick3.BidSize, quoteBar.LastBidSize);

            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.Open);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Low);
            Assert.AreEqual(tick2.AskPrice, quoteBar.Ask.High);
            Assert.AreEqual(tick4.AskPrice, quoteBar.Ask.Close);
            Assert.AreEqual(tick4.AskSize, quoteBar.LastAskSize);
        }
    }
}