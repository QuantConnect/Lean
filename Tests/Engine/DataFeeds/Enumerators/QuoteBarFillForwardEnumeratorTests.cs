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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class QuoteBarFillForwardEnumeratorTests
    {
        [Test]
        public void FillsForwardBidAskBars()
        {
            var bar1 = new QuoteBar
            {
                Bid = new Bar(3m, 4m, 1m, 2m),
                Ask = new Bar(3.1m, 4.1m, 1.1m, 2.1m),
            };

            var bar2 = new QuoteBar
            {
                Bid = null,
                Ask = null,
            };

            var data = new[] { bar1, bar2 }.ToList();
            var enumerator = data.GetEnumerator();

            var fillForwardEnumerator = new QuoteBarFillForwardEnumerator(enumerator);

            // 9:31
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            var quoteBar1 = (QuoteBar)fillForwardEnumerator.Current;
            Assert.AreSame(bar1.Bid, quoteBar1.Bid);
            Assert.AreSame(bar1.Ask, quoteBar1.Ask);

            // 9:32
            Assert.IsTrue(fillForwardEnumerator.MoveNext());
            var quoteBar2 = (QuoteBar)fillForwardEnumerator.Current;
            Assert.AreSame(quoteBar1.Bid, quoteBar2.Bid);
            Assert.AreSame(quoteBar1.Ask, quoteBar2.Ask);

            fillForwardEnumerator.Dispose();
        }
    }
}
