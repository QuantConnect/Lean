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
using QuantConnect.Algorithm;
using QuantConnect.Packets;
using QuantConnect.Tests.Engine.DataFeeds;
using LeanEngine = QuantConnect.Lean.Engine.Engine;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class EngineTests
    {
        [Test]
        public void DetectsOutOfMemoryExceptionsInTheInnerExceptionChain()
        {
            Assert.IsTrue(LeanEngine.IsOutOfMemoryError(new OutOfMemoryException()));
            Assert.IsTrue(LeanEngine.IsOutOfMemoryError(new Exception("wrapper", new OutOfMemoryException())));
            Assert.IsTrue(LeanEngine.IsOutOfMemoryError(
                new Exception("outer", new InvalidOperationException("inner", new OutOfMemoryException()))));

            Assert.IsFalse(LeanEngine.IsOutOfMemoryError(new Exception("not oom")));
            Assert.IsFalse(LeanEngine.IsOutOfMemoryError(new Exception("outer", new InvalidOperationException("inner"))));
        }

        [Test]
        public void OutOfMemoryErrorDetailsIncludeRamAllocationAndAlgorithmState()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddEquity("SPY");

            var job = new BacktestNodePacket { Controls = new Controls { RamAllocation = 512 } };

            var details = LeanEngine.GetOutOfMemoryErrorDetails(job, algorithm);

            StringAssert.Contains("512MB of RAM", details);
            StringAssert.Contains($"{algorithm.Securities.Count} securities", details);
            StringAssert.Contains($"{algorithm.SubscriptionManager.Count} data subscriptions", details);
            StringAssert.Contains($"{algorithm.UniverseManager.Count} universes", details);
            // guidance on the common causes is always present
            StringAssert.Contains("Common causes", details);
        }

        [Test]
        public void OutOfMemoryErrorDetailsAreProducedWithoutAnAlgorithmInstance()
        {
            var job = new BacktestNodePacket { Controls = new Controls { RamAllocation = 512 } };

            var details = LeanEngine.GetOutOfMemoryErrorDetails(job, null);

            StringAssert.Contains("512MB of RAM", details);
            StringAssert.Contains("Common causes", details);
        }
    }
}
