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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Represents an enumerator capable of synchronizing other enumerators of type T in time.
    /// This assumes that all enumerators have data time stamped in the same time zone
    /// </summary>
    public abstract class SynchronizingEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> _syncer;
        private readonly IEnumerator<T>[] _enumerators;

        /// <summary>
        /// Gets the Timestamp for the data
        /// </summary>
        protected abstract DateTime GetInstanceTime(T instance);

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current { get; private set; }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingEnumerator{T}"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        /// <typeparam name="T">The type of data we want, for example, <see cref="BaseData"/> or <see cref="Slice"/>, ect...</typeparam>
        protected SynchronizingEnumerator(params IEnumerator<T>[] enumerators)
            : this((IEnumerable<IEnumerator<T>>)enumerators) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingEnumerator{T}"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        /// <typeparam name="T">The type of data we want, for example, <see cref="BaseData"/> or <see cref="Slice"/>, ect...</typeparam>
        protected SynchronizingEnumerator(IEnumerable<IEnumerator<T>> enumerators)
        {
            _enumerators = enumerators.ToArray();
            _syncer = GetSynchronizedEnumerator(_enumerators);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public bool MoveNext()
        {
            var moveNext = _syncer.MoveNext();
            Current = moveNext ? _syncer.Current : default(T);
            return moveNext;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public void Reset()
        {
            foreach (var enumerator in _enumerators)
            {
                enumerator.Reset();
            }
            // don't call syncer.reset since the impl will just throw
            _syncer = GetSynchronizedEnumerator(_enumerators);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var enumerator in _enumerators)
            {
                enumerator.Dispose();
            }
            _syncer.Dispose();
        }

        /// <summary>
        /// Synchronization system for the enumerator:
        /// </summary>
        /// <param name="enumerators"></param>
        /// <returns></returns>
        private IEnumerator<T> GetSynchronizedEnumerator(IEnumerator<T>[] enumerators)
        {
            return GetBruteForceMethod(enumerators);
        }

        /// <summary>
        /// Brute force implementation for synchronizing the enumerator.
        /// Will remove enumerators returning false to the call to MoveNext.
        /// Will not remove enumerators with Current Null returning true to the call to MoveNext
        /// </summary>
        private IEnumerator<T> GetBruteForceMethod(IEnumerator<T>[] enumerators)
        {
            var ticks = DateTime.MaxValue.Ticks;
            var collection = new HashSet<IEnumerator<T>>();
            foreach (var enumerator in enumerators)
            {
                if (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                    {
                        ticks = Math.Min(ticks, GetInstanceTime(enumerator.Current).Ticks);
                    }
                    collection.Add(enumerator);
                }
                else
                {
                    enumerator.Dispose();
                }
            }

            var frontier = new DateTime(ticks);
            var toRemove = new List<IEnumerator<T>>();
            while (collection.Count > 0)
            {
                var nextFrontierTicks = DateTime.MaxValue.Ticks;
                foreach (var enumerator in collection)
                {
                    while (
                        enumerator.Current == null
                        || GetInstanceTime(enumerator.Current) <= frontier
                    )
                    {
                        if (enumerator.Current != null)
                        {
                            yield return enumerator.Current;
                        }
                        if (!enumerator.MoveNext())
                        {
                            toRemove.Add(enumerator);
                            break;
                        }
                        if (enumerator.Current == null)
                        {
                            break;
                        }
                    }

                    if (enumerator.Current != null)
                    {
                        nextFrontierTicks = Math.Min(
                            nextFrontierTicks,
                            GetInstanceTime(enumerator.Current).Ticks
                        );
                    }
                }

                if (toRemove.Count > 0)
                {
                    foreach (var enumerator in toRemove)
                    {
                        collection.Remove(enumerator);
                    }
                    toRemove.Clear();
                }

                frontier = new DateTime(nextFrontierTicks);
                if (frontier == DateTime.MaxValue)
                {
                    break;
                }
            }
        }
    }
}
