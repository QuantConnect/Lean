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
using QuantConnect.Data;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class CollectionSubscriptionDataSourceReaderTests
    {
        [Test]
        public void HandlesInitializationErrors()
        {
            var date = new DateTime(2018, 7, 7);
            var config = new SubscriptionDataConfig(typeof(TiingoPrice), Symbols.AAPL, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, true);
            var reader = new CollectionSubscriptionDataSourceReader(null, config, date, false);
            var source = new TiingoPrice().GetSource(config, date, false);

            // should not throw with an empty or invalid Tiingo API token
            Assert.DoesNotThrow(() =>
            {
                var list = reader.Read(source).ToList();
                Assert.AreEqual(0, list.Count);
            });
        }
    }
}
