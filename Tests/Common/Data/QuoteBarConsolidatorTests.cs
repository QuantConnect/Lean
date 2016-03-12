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
    public class QuoteBarConsolidatorTests
    {
        [Test]
        public void AggregatesNewQuoteBarProperly()
        {
            QuoteBar quoteBar = null;
            var creator = new QuoteBarConsolidator(4);
            creator.DataConsolidated += (sender, args) =>
            {
                quoteBar = args;
            };

            var time = DateTime.Today;
            var bar1 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.75m, 1.25m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0
            };
            creator.Update(bar1);
            Assert.IsNull(quoteBar);

            var bar2 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1.1m, 2.2m, 0.9m, 2.1m),
                LastBidSize = 3,
                Ask = new Bar(2.2m, 4.4m, 3.3m, 3.3m),
                LastAskSize = 0
            };
            creator.Update(bar2);
            Assert.IsNull(quoteBar);

            var bar3 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = new Bar(1, 2, 0.5m, 1.75m),
                LastBidSize = 3,
                Ask = null,
                LastAskSize = 0
            };
            creator.Update(bar3);
            Assert.IsNull(quoteBar);

            var bar4 = new QuoteBar
            {
                Time = time,
                Symbol = Symbols.SPY,
                Bid = null,
                LastBidSize = 0,
                Ask = new Bar(1, 7, 0.5m, 4.4m),
                LastAskSize = 4
            };
            creator.Update(bar4);
            Assert.IsNotNull(quoteBar);
            Assert.AreEqual(bar1.Symbol, quoteBar.Symbol);
            Assert.AreEqual(bar1.Bid.Open, quoteBar.Bid.Open);
            Assert.AreEqual(bar2.Ask.Open, quoteBar.Ask.Open);
            Assert.AreEqual(bar2.Bid.High, quoteBar.Bid.High);
            Assert.AreEqual(bar4.Ask.High, quoteBar.Ask.High);
            Assert.AreEqual(bar3.Bid.Low, quoteBar.Bid.Low);
            Assert.AreEqual(bar4.Ask.Low, quoteBar.Ask.Low);
            Assert.AreEqual(bar3.Bid.Close, quoteBar.Bid.Close);
            Assert.AreEqual(bar4.Ask.Close, quoteBar.Ask.Close);
            Assert.AreEqual(bar3.LastBidSize, quoteBar.LastBidSize);
            Assert.AreEqual(bar4.LastAskSize, quoteBar.LastAskSize);
        }
    }
}