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

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class TickQuoteBarConsolidatorTests
    {
        [Test]
        public void AggregatesNewQuoteBarProperly()
        {
            QuoteBar quoteBar = null;
            var creator = new TickQuoteBarConsolidator(4);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };
            var reference = DateTime.Today;
            var tick1 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference,
                BidPrice = 10,
                BidSize = 20,
                TickType = TickType.Quote
            };
            creator.Update(tick1);
            Assert.IsNull(quoteBar);

            var tick2 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(1),
                AskPrice = 20,
                AskSize = 10,
                TickType = TickType.Quote
            };

            var badTick = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(1),
                AskPrice = 25,
                AskSize = 100,
                BidPrice = -100,
                BidSize = 2,
                Value = 50,
                Quantity = 1234,
                TickType = TickType.Trade
            };
            creator.Update(badTick);
            Assert.IsNull(quoteBar);
            
            creator.Update(tick2);
            Assert.IsNull(quoteBar);
            var tick3 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(2),
                BidPrice = 12,
                BidSize = 50,
                TickType = TickType.Quote
            };
            creator.Update(tick3);
            Assert.IsNull(quoteBar);

            var tick4 = new Tick
            {
                Symbol = Symbols.SPY,
                Time = reference.AddHours(3),
                AskPrice = 17,
                AskSize = 15,
                TickType = TickType.Quote
            };
            creator.Update(tick4);
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
        public void DoesNotConsolidateDifferentSymbols()
        {
            var consolidator = new TickQuoteBarConsolidator(2);

            var reference = DateTime.Today;

            var tick1 = new Tick
            {
                Symbol = Symbols.AAPL,
                Time = reference,
                BidPrice = 1000,
                BidSize = 20,
                TickType = TickType.Quote,
            };

            var tick2 = new Tick
            {
                Symbol = Symbols.ZNGA,
                Time = reference,
                BidPrice = 20,
                BidSize = 30,
                TickType = TickType.Quote,
            };

            consolidator.Update(tick1);

            Exception ex = Assert.Throws<InvalidOperationException>(() => consolidator.Update(tick2));
            Assert.That(ex.Message, Is.StringContaining("is not the same"));
        }
    }
}