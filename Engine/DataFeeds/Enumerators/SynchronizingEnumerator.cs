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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Represents an enumerator capable of synchronizing other base data enumerators in time.
    /// This assumes that all enumerators have data time stamped in the same time zone
    /// </summary>
    public class SynchronizingEnumerator : IEnumerator<BaseData>
    {
        private IEnumerator<BaseData> _syncer;
        private readonly IEnumerator<BaseData>[] _enumerators;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get; private set;
        }

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
        /// Initializes a new instance of the <see cref="SynchronizingEnumerator"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        public SynchronizingEnumerator(params IEnumerator<BaseData>[] enumerators)
            : this ((IEnumerable<IEnumerator<BaseData>>)enumerators)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingEnumerator"/> class
        /// </summary>
        /// <param name="enumerators">The enumerators to be synchronized. NOTE: Assumes the same time zone for all data</param>
        public SynchronizingEnumerator(IEnumerable<IEnumerator<BaseData>> enumerators)
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
            var moveNext =  _syncer.MoveNext();
            Current = moveNext ? _syncer.Current : null;
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

        private static IEnumerator<BaseData> GetSynchronizedEnumerator(IEnumerator<BaseData>[] enumerators)
        {
            var ticks = DateTime.MaxValue.Ticks;
            var collection = new ConcurrentDictionary<IEnumerator<BaseData>, int>();
            foreach (var enumerator in enumerators)
            {
                if (enumerator.MoveNext())
                {
                    ticks = Math.Min(ticks, enumerator.Current.EndTime.Ticks);
                    collection.TryAdd(enumerator, 0);
                }
                else
                {
                    enumerator.Dispose();
                }
            }

            var frontier = new DateTime(ticks);
            while (!collection.IsEmpty)
            {
                var nextFrontierTicks = DateTime.MaxValue.Ticks;
                foreach (var kvp in collection)
                {
                    var enumerator = kvp.Key;
                    while (enumerator.Current.EndTime <= frontier)
                    {
                        yield return enumerator.Current;
                        if (!enumerator.MoveNext())
                        {
                            int value;
                            collection.TryRemove(enumerator, out value);
                            break;
                        }
                    }

                    if (enumerator.Current != null)
                    {
                        nextFrontierTicks = Math.Min(nextFrontierTicks, enumerator.Current.EndTime.Ticks);
                    }
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
