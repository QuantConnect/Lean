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
using System.Collections.Generic;

namespace QuantConnect.Scheduling
{
    /// <summary>
    /// Helper class that will monitor timer consumers and request more time if required.
    /// Used by <see cref="IsolatorLimitResultProvider"/>
    /// </summary>
    public class TimeMonitor : IDisposable
    {
        private readonly int _monitorIntervalMs;
        private readonly Timer _timer;
        /// <summary>
        /// List to store the coming TimeConsumer objects
        /// </summary>
        /// <remarks>This field is protected because it's used in a test class 
        /// in `IsolatorLimitResultProviderTests.cs</remarks>
        protected List<TimeConsumer> TimeConsumers { get; init; }

        /// <summary>
        /// Returns the number of time consumers currently being monitored
        /// </summary>
        public int Count
        {
            get
            {
                lock (TimeConsumers)
                {
                    return TimeConsumers.Count;
                }
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public TimeMonitor(int monitorIntervalMs = 100)
        {
            _monitorIntervalMs = monitorIntervalMs;
            TimeConsumers = new List<TimeConsumer>();
            _timer = new Timer(state =>
            {
                lock (TimeConsumers)
                {
                    try
                    {
                        RemoveAll();

                        foreach (var consumer in TimeConsumers)
                        {
                            ProcessConsumer(consumer);
                        }
                    }
                    finally
                    {
                        if (TimeConsumers.Count > 0)
                        {
                            // there's some remaning and we are at the timer callback so let's re schedule
                            TrySchedule();
                        }
                    }
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Process the TimeConsumer object in TimeConsumers list
        /// </summary>
        /// <param name="consumer">The TimeConsumer object to be processed</param>
        /// <remarks>This method is protected because it's overrode by a test class
        /// in `IsolatorLimitResultProviderTests.cs`</remarks>
        protected virtual void ProcessConsumer(TimeConsumer consumer)
        {
            if (consumer.NextTimeRequest == null)
            {
                // first time, for performance we register this here and not the time consumer
                consumer.NextTimeRequest = consumer.TimeProvider.GetUtcNow().AddMinutes(1);
            }
            else if (consumer.TimeProvider.GetUtcNow() >= consumer.NextTimeRequest)
            {
                // each minute request additional time from the isolator
                consumer.NextTimeRequest = consumer.NextTimeRequest.Value.AddMinutes(1);
                try
                {
                    // this will notify the isolator that we've exceed the limits
                    consumer.IsolatorLimitProvider.RequestAdditionalTime(minutes: 1);
                }
                catch
                {
                    // pass
                }
            }
        }

        /// <summary>
        /// Remove all TimeConsumer objects where the `Finished` field is marked as true
        /// </summary>
        /// <remarks>This method is protected because it's overrode by a test class in 
        /// `IsolatorLimitResultProviderTests.cs`</remarks>
        protected virtual void RemoveAll()
        {
            TimeConsumers.RemoveAll(time => time.Finished);
        }

        /// <summary>
        /// Adds a new time consumer element to be monitored
        /// </summary>
        /// <param name="consumer">Time consumer instance</param>
        public void Add(TimeConsumer consumer)
        {
            lock (TimeConsumers)
            {
                if (TimeConsumers.Count == 0)
                {
                    // there was none left, schedule is not running, let's schedule it
                    TrySchedule();
                }
                TimeConsumers.Add(consumer);
            }
        }

        /// <summary>
        /// Disposes of the inner timer
        /// </summary>
        public void Dispose()
        {
            _timer.DisposeSafely();
        }

        /// <summary>
        /// Lazy scheduling
        /// </summary>
        private void TrySchedule()
        {
            try
            {
                _timer.Change(Time.GetSecondUnevenWait(_monitorIntervalMs), Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // ignored disposed
            }
        }
    }
}
