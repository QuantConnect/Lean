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
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class FuturesContractTests
    {
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void QuoteBarNullBidAsk(bool hasBid, bool hasAsk)
        {
            var futureContract = new FuturesContract(Symbols.Future_CLF19_Jan2019);

            Bar bid = hasBid ? new Bar(1, 1, 1, 1) : null;
            Bar ask = hasAsk ? new Bar(2, 2, 2, 2) : null;
            var quoteBar = new QuoteBar(new DateTime(2025, 12, 10), Symbols.Future_CLF19_Jan2019, bid, 10, ask, 20);
            futureContract.Update(quoteBar);
            Assert.AreEqual(hasBid ? bid.Close : 0, futureContract.BidPrice);
            Assert.AreEqual(hasAsk ? ask.Close : 0, futureContract.AskPrice);
            Assert.AreEqual(hasAsk ? 20 : 0, futureContract.AskSize);
            Assert.AreEqual(hasBid ? 10 : 0, futureContract.BidSize);
            Assert.AreEqual(0, futureContract.Volume);
            Assert.AreEqual(0, futureContract.LastPrice);
            Assert.AreEqual(0, futureContract.OpenInterest);
        }

        [Test]
        public void QuoteTickUpdate()
        {
            var futureContract = new FuturesContract(Symbols.Future_CLF19_Jan2019);

            var tick = new Tick(new DateTime(2025, 12, 10), Symbols.Future_CLF19_Jan2019, 1, 2, 3, 4);
            futureContract.Update(tick);
            Assert.AreEqual(1, futureContract.BidSize);
            Assert.AreEqual(2, futureContract.BidPrice);
            Assert.AreEqual(3, futureContract.AskSize);
            Assert.AreEqual(4, futureContract.AskPrice);
            Assert.AreEqual(0, futureContract.Volume);
            Assert.AreEqual(0, futureContract.LastPrice);
            Assert.AreEqual(0, futureContract.OpenInterest);
        }

        [Test]
        public void TradeTickUpdate()
        {
            var futureContract = new FuturesContract(Symbols.Future_CLF19_Jan2019);

            var tick = new Tick(new DateTime(2025, 12, 10), Symbols.Future_CLF19_Jan2019, string.Empty, Exchange.UNKNOWN, 1, 2);
            futureContract.Update(tick);
            Assert.AreEqual(1, futureContract.Volume);
            Assert.AreEqual(2, futureContract.LastPrice);
            Assert.AreEqual(0, futureContract.BidSize);
            Assert.AreEqual(0, futureContract.BidPrice);
            Assert.AreEqual(0, futureContract.AskSize);
            Assert.AreEqual(0, futureContract.AskPrice);
            Assert.AreEqual(0, futureContract.OpenInterest);
        }

        [Test]
        public void TradeBarUpdate()
        {
            var futureContract = new FuturesContract(Symbols.Future_CLF19_Jan2019);

            var tick = new TradeBar(new DateTime(2025, 12, 10), Symbols.Future_CLF19_Jan2019, 1, 2, 3, 4, 5);
            futureContract.Update(tick);
            Assert.AreEqual(5, futureContract.Volume);
            Assert.AreEqual(4, futureContract.LastPrice);
            Assert.AreEqual(0, futureContract.BidSize);
            Assert.AreEqual(0, futureContract.BidPrice);
            Assert.AreEqual(0, futureContract.AskSize);
            Assert.AreEqual(0, futureContract.AskPrice);
            Assert.AreEqual(0, futureContract.OpenInterest);
        }

        [Test]
        public void OpenInterest()
        {
            var futureContract = new FuturesContract(Symbols.Future_CLF19_Jan2019);
            var tick = new OpenInterest(new DateTime(2025, 12, 10), Symbols.Future_CLF19_Jan2019, 10);
            futureContract.Update(tick);
            Assert.AreEqual(10, futureContract.OpenInterest);
            Assert.AreEqual(0, futureContract.Volume);
            Assert.AreEqual(0, futureContract.LastPrice);
            Assert.AreEqual(0, futureContract.BidSize);
            Assert.AreEqual(0, futureContract.BidPrice);
            Assert.AreEqual(0, futureContract.AskSize);
            Assert.AreEqual(0, futureContract.AskPrice);
        }
    }
}
