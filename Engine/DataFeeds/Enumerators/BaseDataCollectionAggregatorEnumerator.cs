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
        private bool _liveMode;
        private readonly Symbol _symbol;
        private readonly IEnumerator<BaseData> _enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollectionAggregatorEnumerator"/> class
        /// This will aggregate instances emitted from the underlying enumerator and tag them with the
        /// specified symbol
        /// </summary>
        /// <param name="enumerator">The underlying enumerator to aggregate</param>
        /// <param name="symbol">The symbol to place on the aggregated collection</param>
        /// <param name="liveMode">True if running in live mode</param>
        public BaseDataCollectionAggregatorEnumerator(
            IEnumerator<BaseData> enumerator,
            Symbol symbol,
            bool liveMode = false
        )
        {
            _symbol = symbol;
            _enumerator = enumerator;
            _liveMode = liveMode;
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
                        if (!IsValid(collection))
                        {
                            // we don't emit
                            collection = null;
                        }
                        break;
                    }
                }

                if (_enumerator.Current == null)
                {
                    // the underlying returned null, stop here and start again on the next call
                    _needsMoveNext = true;
                    break;
                }

                if (collection == null)
                {
                    // we have new data, set the collection's symbol/times
                    var current = _enumerator.Current;
                    collection = CreateCollection(_symbol, current.Time, current.EndTime);
                }

                if (collection.EndTime != _enumerator.Current.EndTime)
                {
                    // the data from the underlying is at a different time, stop here
                    _needsMoveNext = false;
                    if (IsValid(collection))
                    {
                        // we emit
                        break;
                    }
                    // we try again
                    collection = null;
                    continue;
                }

                // this data belongs in this collection, keep going until null or bad time
                Add(collection, _enumerator.Current);
                _needsMoveNext = true;
            }

            Current = collection;
            return _liveMode || collection != null;
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
        public BaseDataCollection Current { get; private set; }

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

        /// <summary>
        /// Creates a new, empty <see cref="BaseDataCollection"/>.
        /// </summary>
        /// <param name="symbol">The base data collection symbol</param>
        /// <param name="time">The start time of the collection</param>
        /// <param name="endTime">The end time of the collection</param>
        /// <returns>A new, empty <see cref="BaseDataCollection"/></returns>
        private BaseDataCollection CreateCollection(Symbol symbol, DateTime time, DateTime endTime)
        {
            return new BaseDataCollection
            {
                Symbol = symbol,
                Time = time,
                EndTime = endTime
            };
        }

        /// <summary>
        /// Adds the specified instance of <see cref="BaseData"/> to the current collection
        /// </summary>
        /// <param name="collection">The collection to be added to</param>
        /// <param name="current">The data to be added</param>
        private void Add(BaseDataCollection collection, BaseData current)
        {
            var baseDataCollection = current as BaseDataCollection;
            if (_symbol.HasUnderlying && _symbol.Underlying == current.Symbol)
            {
                // if the underlying has been aggregated, even if it shouldn't need to be, let's handle it nicely
                if (baseDataCollection != null)
                {
                    collection.Underlying = baseDataCollection.Data[0];
                }
                else
                {
                    collection.Underlying = current;
                }
            }
            else
            {
                if (baseDataCollection != null)
                {
                    // datapoint is already aggregated, let's see if it's a single point or a collection we can use already
                    if (baseDataCollection.Data.Count > 1)
                    {
                        collection.Data = baseDataCollection.Data;
                    }
                    else
                    {
                        collection.Data.Add(baseDataCollection.Data[0]);
                    }
                }
                else
                {
                    collection.Data.Add(current);
                }
            }
        }

        /// <summary>
        /// Determines if a given data point is valid and can be emitted
        /// </summary>
        /// <param name="collection">The collection to be emitted</param>
        /// <returns>True if its a valid data point</returns>
        private static bool IsValid(BaseDataCollection collection)
        {
            return collection != null && collection.Data?.Count > 0;
        }
    }
}
