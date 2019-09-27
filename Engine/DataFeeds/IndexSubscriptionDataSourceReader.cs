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
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This <see cref="ISubscriptionDataSourceReader"/> implementation supports
    /// the <see cref="FileFormat.Index"/> and <see cref="IndexedBaseData"/> types.
    /// Handles the layer of indirection for the index data source and forwards
    /// the target source to the corresponding <see cref="ISubscriptionDataSourceReader"/>
    /// </summary>
    public class IndexSubscriptionDataSourceReader : BaseSubscriptionDataSourceReader
    {
        private readonly SubscriptionDataConfig _config;
        private readonly DateTime _date;
        private readonly IndexedBaseData _factory;

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public override event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Creates a new instance of this <see cref="ISubscriptionDataSourceReader"/>
        /// </summary>
        public IndexSubscriptionDataSourceReader(IDataCacheProvider dataCacheProvider,
            SubscriptionDataConfig config,
            DateTime date,
            bool isLiveMode)
        : base(dataCacheProvider, isLiveMode)
        {
            _config = config;
            _date = date;
            _factory = config.Type.GetBaseDataInstance() as IndexedBaseData;
            if (_factory == null)
            {
                throw new ArgumentException($"{nameof(IndexSubscriptionDataSourceReader)} should be used" +
                                            $"with a data type which implements {nameof(IndexedBaseData)}");
            }
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            // handles zip or text files
            using (var reader = CreateStreamReader(source))
            {
                // if the reader doesn't have data then we're done with this subscription
                if (reader == null || reader.EndOfStream)
                {
                    OnInvalidSource(source, new Exception($"The reader was empty for source: ${source.Source}"));
                    yield break;
                }

                // while the reader has data
                while (!reader.EndOfStream)
                {
                    // read a line and pass it to the base data factory
                    var line = reader.ReadLine();
                    if (line.IsNullOrEmpty())
                    {
                        continue;
                    }

                    SubscriptionDataSource dataSource;
                    try
                    {
                        dataSource = _factory.GetSourceForAnIndex(_config, _date, line, IsLiveMode);
                    }
                    catch
                    {
                        OnInvalidSource(source, new Exception("Factory.GetSourceForAnIndex() failed to return a valid source"));
                        yield break;
                    }

                    if (dataSource != null)
                    {
                        var dataReader = SubscriptionDataSourceReader.ForSource(
                            dataSource,
                            DataCacheProvider,
                            _config,
                            _date,
                            IsLiveMode);

                        var enumerator = dataReader.Read(dataSource).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            yield return enumerator.Current;
                        }
                        enumerator.DisposeSafely();
                    }
                }
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
    }
}
