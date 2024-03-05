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
using QuantConnect.Util;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Data source reader that will aggregate data points into a base data collection
    /// </summary>
    public class BaseDataCollectionAggregatorReader : TextSubscriptionDataSourceReader
    {
        private readonly Type _collectionType;
        private BaseDataCollection _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextSubscriptionDataSourceReader"/> class
        /// </summary>
        /// <param name="dataCacheProvider">This provider caches files if needed</param>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="date">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public BaseDataCollectionAggregatorReader(IDataCacheProvider dataCacheProvider, SubscriptionDataConfig config, DateTime date,
            bool isLiveMode, IObjectStore objectStore)
            : base(dataCacheProvider, config, date, isLiveMode, objectStore)
        {
            _collectionType = config.Type;
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            foreach (var point in base.Read(source))
            {
                if (point is BaseDataCollection collection && !collection.Data.IsNullOrEmpty())
                {
                    // if underlying already is returning an aggregated collection let it through as is
                    yield return point;
                }
                else
                {
                    if (_collection != null && _collection.EndTime != point.EndTime)
                    {
                        // when we get a new time we flush current collection instance, if any
                        yield return _collection;
                        _collection = null;
                    }

                    if (_collection == null)
                    {
                        _collection = (BaseDataCollection)Activator.CreateInstance(_collectionType);
                        _collection.Time = point.Time;
                        _collection.Symbol = Config.Symbol;
                        _collection.EndTime = point.EndTime;
                    }
                    // aggregate the data points
                    _collection.Add(point);
                }
            }

            // underlying reader ended, flush current collection instance if any
            if (_collection != null)
            {
                yield return _collection;
                _collection = null;
            }
        }
    }
}
