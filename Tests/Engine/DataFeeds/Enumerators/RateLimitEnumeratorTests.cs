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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class RateLimitEnumeratorTests
    {
        [Test]
        public void LimitsBasedOnTimeBetweenCalls()
        {
            var currentTime = new DateTime(2015, 10, 10, 13, 6, 0);
            var timeProvider = new ManualTimeProvider(currentTime, TimeZones.Utc);
            var data = Enumerable.Range(0, 100).Select(x => new Tick {Symbol = CreateSymbol(x)}).GetEnumerator();
            var rateLimit = new RateLimitEnumerator<BaseData>(data, timeProvider, Time.OneSecond);

            Assert.IsTrue(rateLimit.MoveNext());

            while (rateLimit.MoveNext() && rateLimit.Current == null)
            {
                timeProvider.AdvanceSeconds(0.1);
            }

            var delta = (timeProvider.GetUtcNow() - currentTime).TotalSeconds;

            Assert.AreEqual(1, delta);

            Assert.AreEqual("1", data.Current.Symbol.Value);

            rateLimit.Dispose();
        }

        private static Symbol CreateSymbol(int x)
        {
            return new Symbol(
                SecurityIdentifier.GenerateBase(null, x.ToString(CultureInfo.InvariantCulture), Market.USA),
                 x.ToString(CultureInfo.InvariantCulture));
        }
    }
}
