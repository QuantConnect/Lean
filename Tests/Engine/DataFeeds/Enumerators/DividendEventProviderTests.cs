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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Data.Market;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class DividendEventProviderTests
    {
        // From https://www.nasdaq.com/market-activity/stocks/aapl/dividend-history
        [TestCase("20121106", 2.65)]
        [TestCase("20130206", 2.65)]
        [TestCase("20130508", 3.05)]
        [TestCase("20130807", 3.05)]
        [TestCase("20131105", 3.05)]
        [TestCase("20140205", 3.05)]
        [TestCase("20140507", 3.29)]
        [TestCase("20140806", 0.47)]
        [TestCase("20141105", 0.47)]
        [TestCase("20150204", 0.47)]
        [TestCase("20150506", 0.52)]
        [TestCase("20150805", 0.52)]
        [TestCase("20151104", 0.52)]
        [TestCase("20160203", 0.52)]
        [TestCase("20160504", 0.57)]
        [TestCase("20160803", 0.57)]
        [TestCase("20161102", 0.57)]
        [TestCase("20170208", 0.57)]
        [TestCase("20170510", 0.63)]
        [TestCase("20170809", 0.63)]
        [TestCase("20171109", 0.63)]
        [TestCase("20180208", 0.63)]
        [TestCase("20180510", 0.73)]
        public void DividendsDistribution(string exDividendDateStr, decimal expectedDistribution)
        {
            var dividendProvider = new DividendEventProvider();
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.AAPL, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                false, false, false);
            var factorFile = new LocalDiskFactorFileProvider();
            var mapFile = new LocalDiskMapFileProvider();

            var start = new DateTime(1998, 01, 02);
            dividendProvider.Initialize(config, factorFile.Get(Symbols.AAPL), mapFile.Get(Market.USA).ResolveMapFile(Symbols.AAPL, start), start);

            var exDividendDate = DateTime.ParseExact(exDividendDateStr, DateFormat.EightCharacter, CultureInfo.InvariantCulture);

            var events = dividendProvider
                .GetEvents(new NewTradableDateEventArgs(exDividendDate, null, Symbols.AAPL, null))
                .ToList();
            // ex dividend date does not emit anything
            Assert.AreEqual(0, events.Count);

            events = dividendProvider
                .GetEvents(new NewTradableDateEventArgs(exDividendDate.AddDays(1), null, Symbols.AAPL, null))
                .ToList();

            Assert.AreEqual(1, events.Count);
            var dividend = events[0] as Dividend;
            Assert.IsNotNull(dividend);

            Assert.AreEqual(expectedDistribution, dividend.Distribution);
        }

        [Test]
        public void ThrowsWhenEmptyReferencePrice()
        {
            var dividendProvider = new DividendEventProvider();
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.AAPL, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork,
                false, false, false);
            var start = new DateTime(1998, 01, 02);
            var row1 = new DateTime(2000, 01, 02);
            var row2 = new DateTime(2001, 01, 02);
            var row3 = new DateTime(2002, 01, 02);

            var factorFile = new FactorFile("AAPL", new []
            {
                new FactorFileRow(row1, 0.693m, 1),
                new FactorFileRow(row2, 0.77m, 1),
                new FactorFileRow(row3, 0.85555m, 1)
            }, start);
            var mapFile = new LocalDiskMapFileProvider();

            dividendProvider.Initialize(config, factorFile, mapFile.Get(Market.USA).ResolveMapFile(Symbols.AAPL, start), start);

            foreach (var row in factorFile.Take(1))
            {
                var lastRawPrice = 100;
                var events = dividendProvider
                    .GetEvents(new NewTradableDateEventArgs(row.Date, null, Symbols.AAPL, lastRawPrice))
                    .ToList();
                // ex dividend date does not emit anything
                Assert.AreEqual(0, events.Count);

                Assert.Throws<InvalidOperationException>(() =>
                {
                    dividendProvider
                        .GetEvents(new NewTradableDateEventArgs(row.Date.AddDays(1), null, Symbols.AAPL, lastRawPrice))
                        .ToList();
                });
            }
        }
    }
}
