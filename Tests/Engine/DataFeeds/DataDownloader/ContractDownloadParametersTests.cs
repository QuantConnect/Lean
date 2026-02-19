/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2026 QuantConnect Corporation.
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

using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds.DataDownloader;
using System.Collections.Generic;

namespace QuantConnect.Tests.Engine.DataFeeds.DataDownloader
{
    [TestFixture]
    public class ContractDownloadParametersTests
    {
        private Symbol _symbol1;
        private Symbol _symbol2;

        [SetUp]
        public void SetUp()
        {
            _symbol1 = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            _symbol2 = Symbol.Create("GOOG", SecurityType.Equity, Market.USA);
        }

        [Test]
        public void EqualsDifferentParameters()
        {
            var p1 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p2 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);

            Assert.IsTrue(p1.Equals(p2));

            var p3 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p4 = new ContractDownloadParameters(_symbol2, TickType.Trade, Resolution.Daily);

            Assert.IsFalse(p3.Equals(p4));

            var p5 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p6 = new ContractDownloadParameters(_symbol1, TickType.Quote, Resolution.Daily);

            Assert.IsFalse(p5.Equals(p6));

            var p7 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p8 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Minute);

            Assert.IsFalse(p7.Equals(p8));

            var p9 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            Assert.IsFalse(p9.Equals("not a ContractDownloadParameters"));

            var p10 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p11 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);

            Assert.AreEqual(p10.GetHashCode(), p11.GetHashCode());

            var p12 = new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily);
            var p13 = new ContractDownloadParameters(_symbol2, TickType.Quote, Resolution.Minute);

            Assert.AreNotEqual(p12.GetHashCode(), p13.GetHashCode());
        }

        [Test]
        public void UsableAsHashSetKeyNoDuplicates()
        {
            var set = new HashSet<ContractDownloadParameters>();
            Assert.IsTrue(set.Add(new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily)));
            Assert.IsFalse(set.Add(new ContractDownloadParameters(_symbol1, TickType.Trade, Resolution.Daily)));
            Assert.IsTrue(set.Add(new ContractDownloadParameters(_symbol2, TickType.Quote, Resolution.Minute)));

            Assert.AreEqual(2, set.Count);
        }
    }
}
