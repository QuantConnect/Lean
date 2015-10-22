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

using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Provides an implementation of <see cref="IEnumerator{BaseDataCollection}"/>
    /// that aggregates an underlying <see cref="IEnumerator{BaseData}"/> into a single
    /// data packet
    /// </summary>
    public class BaseDataCollectionAggregatorEnumerator : IEnumerator<BaseDataCollection>
    {
        private bool _endOfStream;
        private bool _needsMoveNext;
        private readonly Symbol _symbol;
        private readonly IEnumerator<BaseData> _enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollectionAggregatorEnumerator"/> class
        /// This will aggregate instances emitted from the underlying enumerator and tag them with the
        /// specified symbol
        /// </summary>
        /// <param name="enumerator">The underlying enumerator to aggregate</param>
        /// <param name="symbol">The symbol to place on the aggregated collection</param>
        public BaseDataCollectionAggregatorEnumerator(IEnumerator<BaseData> enumerator, Symbol symbol)
        {
            _symbol = symbol;
            _enumerator = enumerator;

            _needsMoveNext = true;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            if (_endOfStream)
            {
                return false;
            }

            BaseDataCollection collection = null;
            while (true)
            {
                if (_needsMoveNext)
                {
                    // move next if we dequeued the last item last time we were invoked
                    if (!_enumerator.MoveNext())
                    {
                        _endOfStream = true;
                        break;
                    }
                }

                if (_enumerator.Current == null)
                {
                    // the underlying returned false, stop here and start again on the next call
                    _needsMoveNext = true;
                    break;
                }

                if (collection == null)
                {
                    // we have new data, set the collection's symbol/times
                    collection = new BaseDataCollection
                    {
                        Symbol = _symbol,
                        Time = _enumerator.Current.Time,
                        EndTime = _enumerator.Current.EndTime
                    };
                }

                if (collection.EndTime != _enumerator.Current.EndTime)
                {
                    // the data from the underlying is at a different time, stop here
                    _needsMoveNext = false;
                    break;
                }

                // this data belongs in this collection, keep going until null or bad time
                collection.Data.Add(_enumerator.Current);
                _needsMoveNext = true;
            }

            Current = collection;
            return !_endOfStream;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseDataCollection Current
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
