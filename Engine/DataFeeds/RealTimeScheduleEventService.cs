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
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Allows to setup a real time scheduled event, internally using a <see cref="Thread"/>,
    /// that is guaranteed to trigger at or after the requested time, never before.
    /// </summary>
    /// <remarks>This class is of value because <see cref="Timer"/> could fire the
    /// event before time.</remarks>
    public class RealTimeScheduleEventService : IDisposable
    {
        private readonly Thread _pulseThread;
        private readonly Queue<DateTime> _work;
        private readonly ManualResetEvent _event;
        private readonly CancellationTokenSource _tokenSource;

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
            _tokenSource = new CancellationTokenSource();
            _event = new ManualResetEvent(false);
            _work = new Queue<DateTime>();
            _pulseThread = new Thread(() =>
            {
                while (!_tokenSource.Token.IsCancellationRequested)
                {
                    DateTime nextUtcScheduledEvent;
                    lock (_work)
                    {
                        _work.TryDequeue(out nextUtcScheduledEvent);
                    }

                    if (nextUtcScheduledEvent == default)
                    {
                        _event.WaitOne(_tokenSource.Token);
                        _event.Reset();
                        if (_tokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }
                        continue;
                    }

                    // testing has shown that it sometimes requires more than one loop
                    var diff = nextUtcScheduledEvent - timeProvider.GetUtcNow();
                    while (diff.Ticks > 0)
                    {
                        _tokenSource.Token.WaitHandle.WaitOne(diff);

                        diff = nextUtcScheduledEvent - timeProvider.GetUtcNow();

                        if (_tokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    NewEvent?.Invoke(this, EventArgs.Empty);
                }
            })
            {
                IsBackground = true,
                Name = "RealTimeScheduleEventService"
            };
            _pulseThread.Start();
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
            lock (_work)
            {
                _work.Enqueue(utcNow + dueTime);
                _event.Set();
            }
        }

        /// <summary>
        /// Disposes of the underlying <see cref="Timer"/> instance
        /// </summary>
        public void Dispose()
        {
            _pulseThread.StopSafely(TimeSpan.FromSeconds(1), _tokenSource);
            _tokenSource.DisposeSafely();
            _event.DisposeSafely();
        }
    }
}
