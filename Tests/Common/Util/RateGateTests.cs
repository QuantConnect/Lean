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

using NUnit.Framework;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class RateGateTests
    {
        [TestCase(5)]
        [TestCase(10)]
        public void RateGateWithTimeout(int count)
        {
            var rate = TimeSpan.FromMilliseconds(500);
            using var gate  = new RateGate(1, rate);
            var timer = Stopwatch.StartNew();

            for (var i = 0; i <= count; i++)
            {
                Assert.IsTrue(gate.WaitToProceed(TimeSpan.FromSeconds(5)));
            }

            timer.Stop();

            var elapsed = timer.Elapsed;
            var expectedDelay = rate * count;
            var lowerBound = expectedDelay - expectedDelay * 0.25;
            var upperBound = expectedDelay + expectedDelay * 0.25;

            Assert.GreaterOrEqual(elapsed, lowerBound, $"RateGate was early: {lowerBound - elapsed}");
            Assert.LessOrEqual(elapsed, upperBound, $"RateGate was late: {elapsed - upperBound}");
        }

        [Test]
        public void RateGate_ShouldSkipBecauseOfTimeout()
        {
            var gate = new RateGate(1, TimeSpan.FromSeconds(20));
            var timer = Stopwatch.StartNew();

            Assert.IsTrue(gate.WaitToProceed(-1));
            Assert.IsFalse(gate.WaitToProceed(0));

            timer.Stop();

            Assert.LessOrEqual(timer.Elapsed, TimeSpan.FromSeconds(10));

            gate.Dispose();
        }
    }
}
