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
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementations of <see cref="ISubscriptionDataSourceReader"/> that uses the
    /// <see cref="BaseData.Reader(SubscriptionDataConfig,string,DateTime,bool)"/>
    /// method to read lines of text from a <see cref="SubscriptionDataSource"/>
    /// </summary>
    public class TextSubscriptionDataSourceReader : BaseSubscriptionDataSourceReader
    {
        private readonly bool _implementsStreamReader;
        private readonly DateTime _date;
        private readonly SubscriptionDataConfig _config;
        private BaseData _factory;
        private bool _shouldCacheDataPoints;

        private static int CacheSize = 100;
        private static volatile Dictionary<string, List<BaseData>> BaseDataSourceCache = new Dictionary<string, List<BaseData>>(100);
        private static Queue<string> CacheKeys = new Queue<string>(100);

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public override event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Event fired when an exception is thrown during a call to
        /// <see cref="BaseData.Reader(SubscriptionDataConfig,string,DateTime,bool)"/>
        /// </summary>
        public event EventHandler<ReaderErrorEventArgs> ReaderError;

        /// <summary>
        /// Event fired when there's an error creating an <see cref="IStreamReader"/> or the
        /// instantiated <see cref="IStreamReader"/> has no data.
        /// </summary>
        public event EventHandler<CreateStreamReaderErrorEventArgs> CreateStreamReaderError;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextSubscriptionDataSourceReader"/> class
        /// </summary>
        /// <param name="dataCacheProvider">This provider caches files if needed</param>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="date">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public TextSubscriptionDataSourceReader(IDataCacheProvider dataCacheProvider, SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            : base(dataCacheProvider, isLiveMode)
        {
            _date = date;
            _config = config;
            _shouldCacheDataPoints = !_config.IsCustomData && _config.Resolution >= Resolution.Hour
                && _config.Type != typeof(FineFundamental) && _config.Type != typeof(CoarseFundamental)
                && !DataCacheProvider.IsDataEphemeral;

            // we know these type implement the streamReader interface lets avoid dynamic reflection call to figure it out
            if (_config.Type == typeof(TradeBar) || _config.Type == typeof(QuoteBar) || _config.Type == typeof(Tick))
            {
                _implementsStreamReader = true;
            }
            else
            {
                var method = _config.Type.GetMethod("Reader",
                    new[] { typeof(SubscriptionDataConfig), typeof(StreamReader), typeof(DateTime), typeof(bool) });
                if (method != null && method.DeclaringType == _config.Type)
                {
                    _implementsStreamReader = true;
                }
            }
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            List<BaseData> cache = null;
            _shouldCacheDataPoints = _shouldCacheDataPoints &&
                // only cache local files
                source.TransportMedium == SubscriptionTransportMedium.LocalFile;

            string cacheKey = null;
            if (_shouldCacheDataPoints)
            {
                cacheKey = source.Source + _config.Type;
                BaseDataSourceCache.TryGetValue(cacheKey, out cache);
            }
            if (cache == null)
            {
                cache = _shouldCacheDataPoints ? new List<BaseData>(30000) : null;
                using (var reader = CreateStreamReader(source))
                {
                    // if the reader doesn't have data then we're done with this subscription
                    if (reader == null || reader.EndOfStream)
                    {
                        OnCreateStreamReaderError(_date, source);
                        yield break;
                    }

                    if (_factory == null)
                    {
                        // only create a factory if the stream isn't null
                        _factory = _config.GetBaseDataInstance();
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
                                instance = _factory.Reader(_config, reader.StreamReader, _date, IsLiveMode);
                            }
                            else
                            {
                                // read a line and pass it to the base data factory
                                line = reader.ReadLine();
                                instance = _factory.Reader(_config, line, _date, IsLiveMode);
                            }
                        }
                        catch (Exception err)
                        {
                            OnReaderError(line ?? "StreamReader", err);
                        }

                        if (instance != null && instance.EndTime != default(DateTime))
                        {
                            if (_shouldCacheDataPoints)
                            {
                                cache.Add(instance);
                            }
                            else
                            {
                                yield return instance;
                            }
                        }
                        else if (reader.ShouldBeRateLimited)
                        {
                            yield return instance;
                        }
                    }
                }

                if (!_shouldCacheDataPoints)
                {
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
            // Find the first data point 10 days (just in case) before the desired date
            // and subtract one item (just in case there was a time gap and data.Time is after _date)
            var frontier = _date.AddDays(-10);
            var index = cache.FindIndex(data => data.Time > frontier);
            index = index > 0 ? (index - 1) : 0;
            foreach (var data in cache.Skip(index))
            {
                var clone = data.Clone();
                clone.Symbol = _config.Symbol;
                yield return clone;
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="InvalidSource"/> event
        /// </summary>
        /// <param name="source">The <see cref="SubscriptionDataSource"/> that was invalid</param>
        /// <param name="exception">The exception if one was raised, otherwise null</param>
        private void OnInvalidSource(SubscriptionDataSource source, Exception exception)
        {
            var handler = InvalidSource;
            if (handler != null) handler(this, new InvalidSourceEventArgs(source, exception));
        }

        /// <summary>
        /// Event invocator for the <see cref="ReaderError"/> event
        /// </summary>
        /// <param name="line">The line that caused the exception</param>
        /// <param name="exception">The exception that was caught</param>
        private void OnReaderError(string line, Exception exception)
        {
            var handler = ReaderError;
            if (handler != null) handler(this, new ReaderErrorEventArgs(line, exception));
        }

        /// <summary>
        /// Event invocator for the <see cref="CreateStreamReaderError"/> event
        /// </summary>
        /// <param name="date">The date of the source</param>
        /// <param name="source">The source that caused the error</param>
        private void OnCreateStreamReaderError(DateTime date, SubscriptionDataSource source)
        {
            var handler = CreateStreamReaderError;
            if (handler != null) handler(this, new CreateStreamReaderErrorEventArgs(date, source));
        }

        /// <summary>
        /// Set the cache size to use
        /// </summary>
        /// <remarks>How to size this cache: Take worst case scenario, BTCUSD hour, 60k QuoteBar entries, which are roughly 200 bytes in size -> 11 MB * CacheSize</remarks>
        public static void SetCacheSize(int megaBytesToUse)
        {
            if (megaBytesToUse != 0)
            {
                // we take worst case scenario, each entry is 12 MB
                CacheSize = megaBytesToUse / 12;
                Log.Trace($"TextSubscriptionDataSourceReader.SetCacheSize(): Setting cache size to {CacheSize} items");
            }
        }
    }
}