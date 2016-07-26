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
using System.IO;
using Ionic.Zip;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionDataSourceReader"/> that reads zip entry names
    /// </summary>
    public class ZipEntryNameSubscriptionDataSourceReader : ISubscriptionDataSourceReader
    {
        private readonly SubscriptionDataConfig _config;
        private readonly DateTime _dateTime;
        private readonly bool _isLiveMode;
        private readonly BaseData _factory;

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipEntryNameSubscriptionDataSourceReader"/> class
        /// </summary>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="dateTime">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public ZipEntryNameSubscriptionDataSourceReader(SubscriptionDataConfig config, DateTime dateTime, bool isLiveMode)
        {
            _config = config;
            _dateTime = dateTime;
            _isLiveMode = isLiveMode;
            _factory = (BaseData) Activator.CreateInstance(config.Type);
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            if (!File.Exists(source.Source))
            {
                OnInvalidSource(source, new FileNotFoundException("The specified file was not found", source.Source));
            }

            ZipFile zip;
            try
            {
                zip = new ZipFile(source.Source);
            }
            catch (ZipException err)
            {
                OnInvalidSource(source, err);
                yield break;
            }

            foreach (var entryFileName in zip.EntryFileNames)
            {
                yield return _factory.Reader(_config, entryFileName, _dateTime, _isLiveMode);
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
