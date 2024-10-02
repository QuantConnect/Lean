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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;


namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// 
    /// </summary>
    public class UnsortedTextSubscriptionDataSourceReader : TextSubscriptionDataSourceReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsortedTextSubscriptionDataSourceReader"/>
        /// </summary>
        /// <param name="dataCacheProvider">This provider caches files if needed.</param>
        /// <param name="config">The subscription's configuration.</param>
        /// <param name="date">The date this factory was produced to read data for.</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting.</param>
        /// <param name="objectStore">The object storage for data persistence.</param>
        public UnsortedTextSubscriptionDataSourceReader(IDataCacheProvider dataCacheProvider, SubscriptionDataConfig config, DateTime date, bool isLiveMode, IObjectStore objectStore)
            : base(dataCacheProvider, config, date, isLiveMode, objectStore)
        {
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read.</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the cache is null, indicating that the data source has not been properly initialized or populated. Key: <paramref name="cacheKey"/>.
        /// </exception>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            List<BaseData> cache = null;
            _shouldCacheDataPoints = _shouldCacheDataPoints &&
                // only cache local files
                source.TransportMedium == SubscriptionTransportMedium.LocalFile;

            string cacheKey = null;
            if (_shouldCacheDataPoints)
            {
                cacheKey = source.Source + Config.Type;
                BaseDataSourceCache.TryGetValue(cacheKey, out cache);
            }

            var instances = new List<BaseData>();

            if (cache == null)
            {
                cache = _shouldCacheDataPoints ? new List<BaseData>(30000) : null;
                using (var reader = CreateStreamReader(source))
                {
                    if (reader == null)
                    {
                        // if the reader doesn't have data then we're done with this subscription
                        yield break;
                    }

                    if (_factory == null)
                    {
                        // only create a factory if the stream isn't null
                        _factory = Config.GetBaseDataInstance();
                    }
                    // while the reader has data
                    while (!reader.EndOfStream)
                    {
                        BaseData instance = null;
                        string line = null;
                        try
                        {
                            if (reader.StreamReader != null && _implementsStreamReader)
                            {
                                instance = _factory.Reader(Config, reader.StreamReader, _date, IsLiveMode);
                            }
                            else
                            {
                                // read a line and pass it to the base data factory
                                line = reader.ReadLine();
                                instance = _factory.Reader(Config, line, _date, IsLiveMode);
                            }
                        }
                        catch (Exception err)
                        {
                            OnReaderError(line ?? "StreamReader", err);
                        }

                        if (instance != null && instance.EndTime != default)
                        {
                            if (_shouldCacheDataPoints)
                            {
                                cache.Add(instance);
                            }
                            else
                            {
                                instances.Add(instance);
                            }
                        }
                        else if (reader.ShouldBeRateLimited)
                        {
                            instances.Add(instance);
                        }
                    }
                }

                if (!_shouldCacheDataPoints)
                {
                    // Sort by EndTime before yielding the instances
                    foreach (var data in instances.OrderBy(i => i.EndTime))
                    {
                        yield return data;
                    }

                    yield break;
                }

                lock (CacheKeys)
                {
                    CacheKeys.Enqueue(cacheKey);
                    // we create a new dictionary, so we don't have to take locks when reading, and add our new item
                    var newCache = new Dictionary<string, List<BaseData>>(BaseDataSourceCache) { [cacheKey] = cache };

                    if (BaseDataSourceCache.Count > CacheSize)
                    {
                        var removeCount = 0;
                        // we remove a portion of the first in entries
                        while (++removeCount < (CacheSize / 4))
                        {
                            newCache.Remove(CacheKeys.Dequeue());
                        }
                        // update the cache instance
                        BaseDataSourceCache = newCache;
                    }
                    else
                    {
                        // update the cache instance
                        BaseDataSourceCache = newCache;
                    }
                }
            }

            if (cache == null)
            {
                throw new InvalidOperationException($"Cache should not be null. Key: {cacheKey}");
            }

            // Sort the cache by EndTime and yield
            var sortedCache = cache.OrderBy(data => data.EndTime).ToList();

            // Find the first data point 10 days (just in case) before the desired date
            // and subtract one item (just in case there was a time gap and data.Time is after _date)
            var frontier = _date.AddDays(-10);
            var index = sortedCache.FindIndex(data => data.Time > frontier);
            index = index > 0 ? (index - 1) : 0;
            foreach (var data in sortedCache.Skip(index))
            {
                var clone = data.Clone();
                clone.Symbol = Config.Symbol;
                yield return clone;
            }
        }

    }
}
