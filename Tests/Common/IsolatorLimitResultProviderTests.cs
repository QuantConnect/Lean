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
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class IsolatorLimitResultProviderTests
    {
        [Test]
        public void ConsumeWhileExecutingRequestsAdditionalTimeAfterOneSecond()
        {
            var provider = new FakeIsolatorLimitResultProvider();
            Action code = () => Thread.Sleep(TimeSpan.FromSeconds(1.01));
            provider.ConsumeWhileExecuting(code);

            Assert.AreEqual(1, provider.Invocations.Count);
            Assert.AreEqual(1, provider.Invocations[0]);
        }

        [Test]
        public void ConsumeWhileExecutingDoesNotRequestAdditionalTimeBeforeOneSecond()
        {
            var provider = new FakeIsolatorLimitResultProvider();
            Action code = () => Thread.Sleep(TimeSpan.FromSeconds(0.99));
            provider.ConsumeWhileExecuting(code);

            Assert.IsEmpty(provider.Invocations);
        }

        private class FakeIsolatorLimitResultProvider : IIsolatorLimitResultProvider
        {
            public List<int> Invocations { get; } = new List<int>();

            public IsolatorLimitResult IsWithinLimit()
            {
                return new IsolatorLimitResult(TimeSpan.Zero, string.Empty);
            }

            public void RequestAdditionalTime(int minutes)
            {
                Invocations.Add(minutes);
            }
        }
    }
}
