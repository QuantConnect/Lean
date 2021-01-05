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
using System.Threading;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Allows to setup a real time scheduled event, internally using a <see cref="Timer"/>,
    /// that is guaranteed to trigger at or after the requested time, never before.
    /// </summary>
    /// <remarks>This class is of value because <see cref="Timer"/> could fire the
    /// event before time.</remarks>
    public class RealTimeScheduleEventService : IDisposable
    {
        private readonly Timer _timer;
        private readonly Ref<ReferenceWrapper<DateTime>> _nextUtcScheduledEvent;

        /// <summary>
        /// Event fired when the scheduled time is past
        /// </summary>
        public event EventHandler NewEvent;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeProvider">The time provider to use</param>
        public RealTimeScheduleEventService(ITimeProvider timeProvider)
        {
            _nextUtcScheduledEvent = Ref.Create(new ReferenceWrapper<DateTime>(DateTime.MinValue));
            _timer = new Timer(
                async state =>
                {
                    var nextUtcScheduledEvent = ((Ref<ReferenceWrapper<DateTime>>)state).Value.Value;
                    var diff = nextUtcScheduledEvent - timeProvider.GetUtcNow();
                    // we need to guarantee we trigger the event after the requested due time
                    // has past, if we got called earlier lets wait until time is right
                    while (diff.Ticks > 0)
                    {
                        if (diff.Milliseconds >= 1)
                        {
                            Thread.Sleep(diff);
                        }
                        else
                        {
                            Thread.SpinWait(1000);
                        }
                        // testing has shown that it sometimes requires more than one loop
                        diff = nextUtcScheduledEvent - timeProvider.GetUtcNow();
                    }
                    NewEvent?.Invoke(this, EventArgs.Empty);
                },
                _nextUtcScheduledEvent,
                // Due time is never, has to be scheduled
                Timeout.InfiniteTimeSpan,
                // Do not trigger periodically
                Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Schedules a new event
        /// </summary>
        /// <param name="dueTime">The desired due time</param>
        /// <param name="utcNow">Current utc time</param>
        /// <remarks>Scheduling a new event will try to disable previous scheduled event,
        /// but it is not guaranteed.</remarks>
        public void ScheduleEvent(TimeSpan dueTime, DateTime utcNow)
        {
            _nextUtcScheduledEvent.Value = new ReferenceWrapper<DateTime>(utcNow + dueTime);
            // the timer will wake up a little earlier to improve accuracy
            _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Disposes of the underlying <see cref="Timer"/> instance
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
