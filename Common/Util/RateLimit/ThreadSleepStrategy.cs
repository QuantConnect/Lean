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

using System.Threading;

namespace QuantConnect.Util.RateLimit
{
    /// <summary>
    /// Provides a CPU non-intensive means of waiting for more tokens to be available in <see cref="ITokenBucket"/>.
    /// This strategy should be the most commonly used as it either sleeps or yields the currently executing thread,
    /// allowing for other threads to execute while the current thread is blocked and waiting for new tokens to become
    /// available in the bucket for consumption.
    /// </summary>
    public class ThreadSleepStrategy : ISleepStrategy
    {
        /// <summary>
        /// Gets an instance of <see cref="ISleepStrategy"/> that yields the current thread
        /// </summary>
        public static readonly ISleepStrategy Yielding = new ThreadSleepStrategy(0);

        /// <summary>
        /// Gets an instance of <see cref="ISleepStrategy"/> that sleeps the current thread for
        /// the specified number of milliseconds
        /// </summary>
        /// <param name="milliseconds">The duration of time to sleep, in milliseconds</param>
        public static ISleepStrategy Sleeping(int milliseconds) => new ThreadSleepStrategy(milliseconds);

        private readonly int _milliseconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSleepStrategy"/> using the specified
        /// number of <paramref name="milliseconds"/> for each <see cref="Sleep"/> invocation.
        /// </summary>
        /// <param name="milliseconds">The duration of time to sleep, in milliseconds</param>
        public ThreadSleepStrategy(int milliseconds)
        {
            _milliseconds = milliseconds;
        }

        /// <summary>
        /// Sleeps the current thread using the initialized number of milliseconds
        /// </summary>
        public void Sleep()
        {
            Thread.Sleep(_milliseconds);
        }
    }
}