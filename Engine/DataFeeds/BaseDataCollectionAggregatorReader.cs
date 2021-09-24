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
using QuantConnect.Data;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Data source reader that will aggregate data points into a base data collection
    /// </summary>
    public class BaseDataCollectionAggregatorReader : ISubscriptionDataSourceReader
    {
        private Type _collectionType;
        private BaseDataCollection _collection;
        private ISubscriptionDataSourceReader _reader;

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BaseDataCollectionAggregatorReader(ISubscriptionDataSourceReader reader, Type collectionType)
        {
            _reader = reader;
            _collectionType = collectionType;
            _reader.InvalidSource += (sender, args) => InvalidSource?.Invoke(sender, args);
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            foreach (var point in _reader.Read(source))
            {
                if (point is BaseDataCollection)
                {
                    yield return point;
                }
                else
                {
                    if (_collection != null && _collection.EndTime != point.EndTime)
                    {
                        yield return _collection;
                        _collection = null;
                    }

                    if (_collection == null)
                    {
                        _collection = (BaseDataCollection)Activator.CreateInstance(_collectionType);
                        _collection.Time = point.Time;
                        _collection.Symbol = point.Symbol;
                        _collection.EndTime = point.EndTime;
                    }
                    _collection.Data.Add(point);
                }
            }
            if (_collection != null)
            {
                yield return _collection;
                _collection = null;
            }
        }
    }
}
