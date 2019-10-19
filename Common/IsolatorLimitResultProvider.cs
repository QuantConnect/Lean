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
using System.Threading.Tasks;
using QuantConnect.Scheduling;

namespace QuantConnect
{
    /// <summary>
    /// Provides access to the <see cref="NullIsolatorLimitResultProvider"/> and extension methods supporting <see cref="ScheduledEvent"/>
    /// </summary>
    public static class IsolatorLimitResultProvider
    {
        /// <summary>
        /// Provides access to a null implementation of <see cref="IIsolatorLimitResultProvider"/>
        /// </summary>
        public static readonly IIsolatorLimitResultProvider Null = new NullIsolatorLimitResultProvider();

        /// <summary>
        /// Convenience method for invoking a scheduled event's Scan method inside the <see cref="IsolatorLimitResultProvider"/>
        /// </summary>
        public static void Consume(
            this IIsolatorLimitResultProvider isolatorLimitProvider,
            ScheduledEvent scheduledEvent,
            DateTime scanTimeUtc
            )
        {
            // perform initial filtering to prevent starting a task when not necessary
            if (scheduledEvent.NextEventUtcTime > scanTimeUtc)
            {
                return;
            }

            var timeProvider = RealTimeProvider.Instance;
            isolatorLimitProvider.Consume(timeProvider, () => scheduledEvent.Scan(scanTimeUtc));
        }

        /// <summary>
        /// Executes the provided code block and while the code block is running, continually consume from
        /// the limit result provided one token each minute. This function allows the code to run for the
        /// first full minute without requesting additional time from the provider. Following that, every
        /// minute an additional one minute will be requested from the provider.
        /// </summary>
        /// <remarks>
        /// This method exists to support scheduled events, and as such, intercepts any errors raised via the
        /// provided code and wraps them in a <see cref="ScheduledEventException"/>. If in the future this is
        /// usable elsewhere, consider refactoring to handle the errors in a different fashion.
        /// </remarks>
        public static void Consume(
            this IIsolatorLimitResultProvider isolatorLimitProvider,
            ITimeProvider timeProvider,
            Action code
            )
        {
            var finished = 0L;
            Task.Run(async () =>
            {
                if (Interlocked.Read(ref finished) != 0L)
                {
                    // case when the code block has virtually no code in it and
                    // was able to complete faster than the task was able to start
                    return;
                }

                var next = timeProvider.GetUtcNow().AddMinutes(1);
                while (Interlocked.Read(ref finished) == 0L)
                {
                    if (timeProvider.GetUtcNow() >= next)
                    {
                        // each minute request additional time from the isolator
                        next = next.AddMinutes(1);

                        // this will throw and notify the isolator that we've exceed the limits
                        isolatorLimitProvider.RequestAdditionalTime(minutes: 1);
                    }

                    await Task.Delay(5).ConfigureAwait(false);
                }
            });

            code();
            Interlocked.Increment(ref finished);
        }


        private sealed class NullIsolatorLimitResultProvider : IIsolatorLimitResultProvider
        {
            private static readonly IsolatorLimitResult OK = new IsolatorLimitResult(TimeSpan.Zero, string.Empty);

            public void RequestAdditionalTime(int minutes) { }
            public IsolatorLimitResult IsWithinLimit() { return OK; }
            public bool TryRequestAdditionalTime(int minutes) { return true; }
        }
    }
}