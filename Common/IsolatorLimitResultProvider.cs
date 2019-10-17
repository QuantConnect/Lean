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
using QuantConnect.Logging;
using QuantConnect.Scheduling;

namespace QuantConnect
{
    /// <summary>
    /// Provides access to the <see cref="NullIsolatorLimitResultProvider"/> and extension methods supporting <see cref="ScheduledEvent"/>
    /// </summary>
    public static class IsolatorLimitResultProvider
    {
        // this just makes the code below prettier
        private static TimeSpan Second => TimeSpan.FromSeconds(1);

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
            isolatorLimitProvider.Consume(scheduledEvent.Name, () => scheduledEvent.Scan(scanTimeUtc));
        }

        /// <summary>
        /// Executes the provided code block and while the code block is running, continually consume from
        /// the limit result provided one token each minute. This function allows the code to run for the
        /// first full second without requesting additional time from the provider. Following that, every
        /// minute an additional one minute will be requested from the provider.
        /// </summary>
        /// <remarks>
        /// This method exists to support scheduled events, and as such, intercepts any errors raised via the
        /// provided code and wraps them in a <see cref="ScheduledEventException"/>. If in the future this is
        /// usable elsewhere, consider refactoring to handle the errors in a different fashion.
        /// </remarks>
        public static void Consume(
            this IIsolatorLimitResultProvider isolatorLimitProvider,
            string name,
            Action code
            )
        {
            Exception error = null;
            var memoryFence = new object();
            var codeFinished = new ManualResetEvent(false);

            // execute the code in a separate task so we can track time and request more as needed
            Task.Run(() =>
            {
                code();
                codeFinished.Set();
            }).ContinueWith(task =>
            {
                // use full locking semantics to guarantee the value is propagated between threads
                lock (memoryFence)
                {
                    // in the event an error is raised, capture the exception
                    error = task.Exception;
                    codeFinished.Set();
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            // permit up to one second w/out requesting additional time from the provider
            if (!codeFinished.WaitOne(Second))
            {
                var count = 0;
                do
                {
                    if (count % 60 != 0)
                    {
                        continue;
                    }

                    // on the first iteration and every minute following, request additional time
                    Log.Trace($"IsolatorLimitResultProvider.ConsumeForScheduledEvent({name}): Requesting additional time. Elapsed minutes: {count / 60}");
                    if (!isolatorLimitProvider.TryRequestAdditionalTime(minutes: 1))
                    {
                        error = new TimeoutException("The scheduled event exceeded the available amount of time for processing. " +
                                                    $"Elapsed: {count/60.0:0.0} minutes. Scheduled Event: {name}");
                        break;
                    }

                    count++;
                } while (!codeFinished.WaitOne(Second));
            }

            lock (memoryFence)
            {
                if (error != null)
                {
                    var scheduledEventError = error as ScheduledEventException;
                    if (scheduledEventError != null)
                    {
                        throw scheduledEventError;
                    }

                    // otherwise wrap it in a ScheduledEventException
                    var errorMessage = $"There was an error in a scheduled event {name}. The error was {error.Message}";
                    throw new ScheduledEventException(name, errorMessage, error);
                }
            }
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