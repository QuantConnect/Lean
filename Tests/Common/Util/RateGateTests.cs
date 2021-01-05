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
        [Test, Ignore("Running multiple tests at once causes this test to fail")]
        public void RateGate_200InstancesWaitOnAveragePlus150msMinus20ms()
        {
            var gates = new Dictionary<int, RateGate>();

            for (var i = 300; i < 500; i++)
            {
                gates[i] = new RateGate(10, TimeSpan.FromMilliseconds(i));
            }

            foreach (var kvp in gates)
            {
                var gate = kvp.Value;
                var timer = Stopwatch.StartNew();

                for (var i = 0; i <= 10; i++)
                {
                    gate.WaitToProceed();
                }

                timer.Stop();

                var elapsed = timer.Elapsed;
                var lowerBound = TimeSpan.FromMilliseconds(kvp.Key - 20);
                var upperBound = TimeSpan.FromMilliseconds(kvp.Key + 150);

                Assert.GreaterOrEqual(elapsed, lowerBound, $"RateGate was early: {lowerBound - elapsed}");
                Assert.LessOrEqual(elapsed, upperBound, $"RateGate was late: {elapsed - upperBound}");

                gate.Dispose();
            }
        }

        [Test]
        public void RateGate_400InstancesWaitOnAveragePlus150msMinus20msWithTimeout()
        {
            var gates = new Dictionary<int, RateGate>();

            for (var i = 100; i < 500; i++)
            {
                gates[i] = new RateGate(10, TimeSpan.FromMilliseconds(i));
            }

            Parallel.ForEach(gates, kvp =>
            {
                var gate = kvp.Value;
                var timer = Stopwatch.StartNew();

                for (var i = 0; i <= 10; i++)
                {
                    gate.WaitToProceed(kvp.Key);
                }

                timer.Stop();

                var elapsed = timer.Elapsed;
                var lowerBound = TimeSpan.FromMilliseconds(kvp.Key - 20);
                var upperBound = TimeSpan.FromMilliseconds(kvp.Key + 150);

                Assert.GreaterOrEqual(elapsed, lowerBound, $"RateGate was early: {lowerBound - elapsed}");
                Assert.LessOrEqual(elapsed, upperBound, $"RateGate was late: {elapsed - upperBound}");

                gate.Dispose();
            });
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
