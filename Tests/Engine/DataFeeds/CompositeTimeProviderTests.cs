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
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class CompositeTimeProviderTests
    {
        [Test]
        public void NoProviderGiven()
        {
            var compositeTimeProvider = new CompositeTimeProvider(
                Enumerable.Empty<ITimeProvider>()
            );

            var utcNow = DateTime.UtcNow;

            Assert.LessOrEqual(utcNow, compositeTimeProvider.GetUtcNow());
        }

        [Test]
        public void SmallestTime()
        {
            var time = new DateTime(1999, 1, 1);

            var timeProvider1 = new ManualTimeProvider();
            timeProvider1.SetCurrentTimeUtc(time.AddYears(1));
            var timeProvider2 = new ManualTimeProvider();
            timeProvider2.SetCurrentTimeUtc(time);
            var timeProvider3 = new ManualTimeProvider();
            timeProvider3.SetCurrentTimeUtc(time.AddYears(2));

            var compositeTimeProvider = new CompositeTimeProvider(
                new[] { timeProvider1, timeProvider2, timeProvider3 }
            );

            Assert.LessOrEqual(time, compositeTimeProvider.GetUtcNow());
        }
    }
}
