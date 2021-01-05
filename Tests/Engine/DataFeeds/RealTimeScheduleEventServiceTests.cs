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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Ignore("Performance test")]
    public class RealTimeScheduleEventServiceTests
    {
        [Test]
        public void Accuracy()
        {
            var scheduledEventService = new RealTimeScheduleEventService(RealTimeProvider.Instance);
            EventHandler handler = (_, __) =>
            {
                Log.Trace($"{DateTime.UtcNow:O}");

                var now = DateTime.UtcNow;
                var nextEventTime = now.RoundDown(TimeSpan.FromSeconds(1)).Add(TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(101));
                scheduledEventService.ScheduleEvent(nextEventTime - now, now);
            };
            scheduledEventService.NewEvent += handler;
            handler(this, null);
            Thread.Sleep(5000);
            scheduledEventService.DisposeSafely();
        }
    }
}
