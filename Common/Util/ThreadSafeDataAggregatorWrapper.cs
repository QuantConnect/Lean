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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a thread-safe singleton wrapper around an <see cref="IDataAggregator"/> instance.
    /// Ensures all calls to <see cref="Update"/> are synchronized internally.
    /// </summary>
    public sealed class ThreadSafeDataAggregatorWrapper
    {
        /// <summary>
        /// The underlying <see cref="IDataAggregator"/> instance to which updates are delegated.
        /// </summary>
        private readonly IDataAggregator _inner;

        /// <summary>
        /// Lock object used to synchronize access to the underlying aggregator's update method.
        /// </summary>
        private readonly Lock _lock = new();

        /// <summary>
        /// The singleton instance of the thread-safe wrapper.
        /// </summary>
        private static ThreadSafeDataAggregatorWrapper _instance;

        /// <summary>
        /// Lock object used to synchronize singleton instance initialization.
        /// </summary>
        private static readonly Lock _instanceLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadSafeDataAggregatorWrapper"/> class
        /// wrapping the specified <see cref="IDataAggregator"/>.
        /// </summary>
        /// <param name="inner">The underlying aggregator to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> is null.</exception>

        private ThreadSafeDataAggregatorWrapper(IDataAggregator inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Initializes the singleton instance with the given <see cref="IDataAggregator"/> wrapper.
        /// Returns the same singleton instance on subsequent calls.
        /// Throws an exception if called again with a different aggregator instance.
        /// </summary>
        /// <param name="aggregator">The aggregator instance to wrap.</param>
        /// <returns>The singleton thread-safe aggregator wrapper instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="aggregator"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if called more than once with different aggregators.</exception>
        public static ThreadSafeDataAggregatorWrapper Initialize(IDataAggregator aggregator)
        {
            ArgumentNullException.ThrowIfNull(aggregator, nameof(aggregator));

            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    _instance = new ThreadSafeDataAggregatorWrapper(aggregator);
                }
                else if (!ReferenceEquals(_instance._inner, aggregator))
                {
                    throw new InvalidOperationException("ThreadSafeDataAggregator has already been initialized with a different IDataAggregator instance.");
                }

                return _instance;
            }
        }

        /// <summary>
        /// Performs a thread-safe update by delegating to the underlying aggregator.
        /// </summary>
        /// <param name="tick">The tick data to update.</param>
        public void Update(Tick tick)
        {
            lock (_lock)
            {
                _inner.Update(tick);
            }
        }
    }
}
