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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Collection Subscription Factory takes a BaseDataCollection from BaseData factories
    /// and yields it one point at a time to the algorithm
    /// </summary>
    public class CollectionSubscriptionDataSourceReader : BaseSubscriptionDataSourceReader
    {
        private readonly DateTime _date;
        private readonly BaseData _factory;
        private readonly SubscriptionDataConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSubscriptionDataSourceReader"/> class
        /// </summary>
        /// <param name="dataCacheProvider">Used to cache data for requested from the IDataProvider</param>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="date">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public CollectionSubscriptionDataSourceReader(IDataCacheProvider dataCacheProvider, SubscriptionDataConfig config, DateTime date, bool isLiveMode, IObjectStore objectStore)
            :base(dataCacheProvider, isLiveMode, objectStore)
        {
            _date = date;
            _config = config;
            _factory = _config.GetBaseDataInstance();
        }

        /// <summary>
        /// Event fired when an exception is thrown during a call to
        /// <see cref="BaseData.Reader(SubscriptionDataConfig, string, DateTime, bool)"/>
        /// </summary>
        public event EventHandler<ReaderErrorEventArgs> ReaderError;

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            SubscriptionDataSourceReader.CheckRemoteFileCache();

            IStreamReader reader = null;
            try
            {
                reader = CreateStreamReader(source);
                if (reader == null)
                {
                    yield break;
                }

                var raw = "";
                while (!reader.EndOfStream)
                {
                    BaseDataCollection instances = null;
                    try
                    {
                        raw = reader.ReadLine();
                        var result = _factory.Reader(_config, raw, _date, IsLiveMode);
                        instances = result as BaseDataCollection;
                        if (instances == null && !reader.ShouldBeRateLimited)
                        {
                            OnInvalidSource(source, new Exception("Reader must generate a BaseDataCollection with the FileFormat.Collection"));
                            continue;
                        }
                    }
                    catch (Exception err)
                    {
                        OnReaderError(raw, err);
                        if (!reader.ShouldBeRateLimited)
                        {
                            continue;
                        }
                    }

                    if (IsLiveMode
                        // this shouldn't happen, rest reader is the only one to be rate limited
                        // and in live mode, but just in case...
                        || instances == null && reader.ShouldBeRateLimited)
                    {
                        // in live trading these data points will be unrolled at the
                        // 'LiveCustomDataSubscriptionEnumeratorFactory' level
                        yield return instances;
                    }
                    else
                    {
                        foreach (var instance in instances.Data)
                        {
                            if (instance != null && instance.EndTime != default(DateTime))
                            {
                                yield return instance;
                            }
                        }
                    }
                }
            }
            finally
            {
                reader.DisposeSafely();
            }
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
    }
}
