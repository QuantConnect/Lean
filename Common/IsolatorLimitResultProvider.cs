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

namespace QuantConnect
{
    /// <summary>
    /// Provides access to the <see cref="NullIsolatorLimitResultProvider"/> and can be a place for future
    /// extension methods of <see cref="IIsolatorLimitResultProvider"/>
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
        /// Executes the provided code block and while the code block is running, continually consume from
        /// the limit result provided one token each minute. This function allows the code to run for the
        /// first full second without requesting additional time from the provider. Following that, every
        /// minute an additional one minute will be requested from the provider.
        /// </summary>
        public static void ConsumeWhileExecuting(
            this IIsolatorLimitResultProvider isolatorLimitProvider,
            Action code
            )
        {
            isolatorLimitProvider.ConsumeWhileExecuting(string.Empty, code);
        }

        /// <summary>
        /// Executes the provided code block and while the code block is running, continually consume from
        /// the limit result provided one token each minute. This function allows the code to run for the
        /// first full second without requesting additional time from the provider. Following that, every
        /// minute an additional one minute will be requested from the provider.
        /// </summary>
        public static void ConsumeWhileExecuting(
            this IIsolatorLimitResultProvider isolatorLimitProvider,
            string name,
            Action code
            )
        {
            var codeFinished = new ManualResetEvent(false);
            Task.Run(() =>
            {
                code();
                codeFinished.Set();
            });

            // permit up to one second w/out requesting additional time from the provider
            if (codeFinished.WaitOne(Second))
            {
                return;
            }

            var count = 0;
            do
            {
                if (count % 60 != 0)
                {
                    continue;
                }

                // on the first iteration and every minute following, request additional time
                Log.Trace($"IsolatorLimitResultProvider.ConsumeWhileExecuting({name}): Requesting additional time. Elapsed minutes: {count / 60}");
                isolatorLimitProvider.RequestAdditionalTime(minutes: 1);

            } while (!codeFinished.WaitOne(Second));
        }


        private sealed class NullIsolatorLimitResultProvider : IIsolatorLimitResultProvider
        {
            private static readonly IsolatorLimitResult OK = new IsolatorLimitResult(TimeSpan.Zero, string.Empty);

            public void RequestAdditionalTime(int minutes) { }
            public IsolatorLimitResult IsWithinLimit() { return OK; }
        }
    }
}