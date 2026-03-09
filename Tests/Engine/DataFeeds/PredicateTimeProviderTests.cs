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
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class PredicateTimeProviderTests
    {
        [Test]
        public void RespectsCustomStepEvaluator()
        {
            var startTime = new DateTime(2018, 1, 1);
            var manualTimeProvider = new ManualTimeProvider(startTime);
            var stepTimeProvider = new PredicateTimeProvider(manualTimeProvider,
                // only step when minute is a pair number
                time => time.Minute % 2 == 0);

            Assert.AreEqual(manualTimeProvider.GetUtcNow(), stepTimeProvider.GetUtcNow());

            manualTimeProvider.AdvanceSeconds(45 * 60); // advance 45 minutes, past the interval

            // still the same because 45 minutes isn't pair
            Assert.AreEqual(startTime, stepTimeProvider.GetUtcNow());
            Assert.AreNotEqual(manualTimeProvider.GetUtcNow(), stepTimeProvider.GetUtcNow());

            manualTimeProvider.AdvanceSeconds(60);
            Assert.AreEqual(manualTimeProvider.GetUtcNow(), stepTimeProvider.GetUtcNow());
        }
    }
}
