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

namespace QuantConnect.Util.RateLimit
{
    /// <summary>
    /// Provides an implementation of <see cref="ITokenBucket"/> that implements the leaky bucket algorithm
    /// See: https://en.wikipedia.org/wiki/Leaky_bucket
    /// </summary>
    public class LeakyBucket : ITokenBucket
    {
        private readonly object _sync = new object();

        private long _available;
        private readonly ISleepStrategy _sleep;
        private readonly IRefillStrategy _refill;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Gets the maximum capacity of tokens this bucket can hold.
        /// </summary>
        public long Capacity { get; }

        /// <summary>
        /// Gets the total number of currently available tokens for consumption
        /// </summary>
        public long AvailableTokens
        {
            // synchronized read w/ the modification of available tokens in TryConsume
            get { lock (_sync) return _available; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeakyBucket"/> class.
        /// This constructor initializes the bucket using the <see cref="ThreadSleepStrategy.Sleep"/> with a 1 millisecond
        /// sleep to prevent being CPU intensive and uses the <see cref="FixedIntervalRefillStrategy"/> to refill bucket
        /// tokens according to the <paramref name="refillAmount"/> and <paramref name="refillInterval"/> parameters.
        /// </summary>
        /// <param name="capacity">The maximum number of tokens this bucket can hold</param>
        /// <param name="refillAmount">The number of tokens to add to the bucket each <paramref name="refillInterval"/></param>
        /// <param name="refillInterval">The interval which after passing more tokens are added to the bucket</param>
        public LeakyBucket(long capacity, long refillAmount, TimeSpan refillInterval)
            : this(capacity, ThreadSleepStrategy.Sleeping(1),
                new FixedIntervalRefillStrategy(RealTimeProvider.Instance, refillAmount, refillInterval)
            )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeakyBucket"/> class
        /// </summary>
        /// <param name="capacity">The maximum number of tokens this bucket can hold</param>
        /// <param name="sleep">Defines the <see cref="ISleepStrategy"/> used when <see cref="Consume"/> is invoked
        /// but the bucket does not have enough tokens yet</param>
        /// <param name="refill">Defines the <see cref="IRefillStrategy"/> that computes how many tokens to add
        /// back to the bucket each time consumption is attempted</param>
        /// <param name="timeProvider">Defines the <see cref="ITimeProvider"/> used to enforce timeouts when
        /// invoking <see cref="Consume"/></param>
        public LeakyBucket(long capacity, ISleepStrategy sleep, IRefillStrategy refill, ITimeProvider timeProvider = null)
        {
            _sleep = sleep;
            _refill = refill;
            Capacity = capacity;
            _available = capacity;
            _timeProvider = timeProvider ?? RealTimeProvider.Instance;
        }

        /// <summary>
        /// Blocks until the specified number of tokens are available for consumption
        /// and then consumes that number of tokens.
        /// </summary>
        /// <param name="tokens">The number of tokens to consume</param>
        /// <param name="timeout">The maximum amount of time, in milliseconds, to block. An exception is
        /// throw in the event it takes longer than the stated timeout to consume the requested number
        /// of tokens</param>
        public void Consume(long tokens, long timeout = Timeout.Infinite)
        {
            if (timeout < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout),
                    "Invalid timeout. Use -1 for no timeout, 0 for immediate timeout and a positive number " +
                    "of milliseconds to indicate a timeout. All other values are out of range."
                );
            }

            var startTime = _timeProvider.GetUtcNow();

            while (true)
            {
                if (TryConsume(tokens))
                {
                    break;
                }

                if (timeout != Timeout.Infinite)
                {
                    // determine if the requested timeout has elapsed
                    var currentTime = _timeProvider.GetUtcNow();
                    var elapsedMilliseconds = (currentTime - startTime).TotalMilliseconds;
                    if (elapsedMilliseconds > timeout)
                    {
                        throw new TimeoutException("The operation timed out while waiting for the rate limit to be lifted.");
                    }
                }

                _sleep.Sleep();
            }
        }

        /// <summary>
        /// Attempts to consume the specified number of tokens from the bucket. If the
        /// requested number of tokens are not immediately available, then this method
        /// will return false to indicate that zero tokens have been consumed.
        /// </summary>
        public bool TryConsume(long tokens)
        {
            if (tokens <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokens),
                    "Number of tokens to consume must be positive"
                );
            }

            if (tokens > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(tokens),
                    "Number of tokens to consume must be less than or equal to the capacity"
                );
            }

            lock (_sync)
            {
                // determine how many units have become available since last invocation
                var refilled = Math.Max(0, _refill.Refill());

                // the number of tokens to add, the max of which is the difference between capacity and currently available
                var deltaTokens = Math.Min(Capacity - _available, refilled);

                // update the available number of units with the new tokens
                _available += deltaTokens;

                if (tokens > _available)
                {
                    // we don't have enough tokens yet
                    Logging.Log.Trace($"LeakyBucket.TryConsume({tokens}): Failed to consumed tokens. Available: {_available}");
                    return false;
                }

                // subtract the number of tokens consumed
                _available = _available - tokens;
                Logging.Log.Trace($"LeakyBucket.TryConsume({tokens}): Successfully consumed tokens. Available: {_available}");
                return true;
            }
        }

    }
}
