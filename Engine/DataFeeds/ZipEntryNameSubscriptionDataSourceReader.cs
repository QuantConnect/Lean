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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds.Transport;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionDataSourceReader"/> that reads zip entry names
    /// </summary>
    public class ZipEntryNameSubscriptionDataSourceReader : BaseSubscriptionDataSourceReader
    {
        private readonly IDataCacheProvider _dataProvider;
        private readonly SubscriptionDataConfig _config;
        private readonly DateTime _date;
        private readonly bool _isLiveMode;
        private readonly BaseData _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipEntryNameSubscriptionDataSourceReader"/> class
        /// </summary>
        /// <param name="dataProvider">Used to fetch data</param>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="date">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public ZipEntryNameSubscriptionDataSourceReader(IDataCacheProvider dataProvider, SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            : base(dataProvider, isLiveMode, null)
        {
            _date = date;
            _config = config;
            _isLiveMode = isLiveMode;
            _dataProvider = dataProvider;
            _factory = config.GetBaseDataInstance();
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public override IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            var fileName = source.Source;

            if (source.TransportMedium == SubscriptionTransportMedium.RemoteFile)
            {
                using var reader = CreateStreamReader(source) as RemoteFileSubscriptionStreamReader;
                if (reader != null)
                {
                    fileName = reader.LocalFileName;
                }
            }

            List<string> entryNames;
            try
            {
                entryNames = _dataProvider.GetZipEntries(fileName);
            }
            catch (Exception err)
            {
                OnInvalidSource(source, err);
                yield break;
            }

            foreach (var entryFileName in entryNames)
            {
                var instance = _factory.Reader(_config, entryFileName, _date, _isLiveMode);
                if (instance != null && instance.EndTime != default(DateTime))
                {
                    yield return instance;
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
            OnInvalidSource(source, exception);
        }
    }
}
