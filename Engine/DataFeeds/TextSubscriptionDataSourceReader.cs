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
using System.ComponentModel;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementations of <see cref="ISubscriptionDataSourceReader"/> that uses the
    /// <see cref="BaseData.Reader(QuantConnect.Data.SubscriptionDataConfig,string,System.DateTime,bool)"/>
    /// method to read lines of text from a <see cref="SubscriptionDataSource"/>
    /// </summary>
    public class TextSubscriptionDataSourceReader : ISubscriptionDataSourceReader
    {
        private readonly bool _isLiveMode;
        private readonly BaseData _factory;
        private readonly DateTime _date;
        private readonly SubscriptionDataConfig _config;
        private readonly IDataCacheProvider _dataCacheProvider;

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Event fired when an exception is thrown during a call to
        /// <see cref="BaseData.Reader(QuantConnect.Data.SubscriptionDataConfig,string,System.DateTime,bool)"/>
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
        {
            _dataCacheProvider = dataCacheProvider;
            _date = date;
            _config = config;
            _isLiveMode = isLiveMode;
            _factory = (BaseData) ObjectActivator.GetActivator(config.Type).Invoke(new object[] { config.Type });
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            using (var reader = CreateStreamReader(source))
            {
                // if the reader doesn't have data then we're done with this subscription
                if (reader == null || reader.EndOfStream)
                {
                    OnCreateStreamReaderError(_date, source);
                    yield break;
                }

                // while the reader has data
                while (!reader.EndOfStream)
                {
                    // read a line and pass it to the base data factory
                    var line = reader.ReadLine();
                    BaseData instance = null;
                    try
                    {
                        instance = _factory.Reader(_config, line, _date, _isLiveMode);
                    }
                    catch (Exception err)
                    {
                        OnReaderError(line, err);
                    }

                    if (instance != null && instance.EndTime != default(DateTime))
                    {
                        yield return instance;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="IStreamReader"/> for the specified <paramref name="subscriptionDataSource"/>
        /// </summary>
        /// <param name="subscriptionDataSource">The source to produce an <see cref="IStreamReader"/> for</param>
        /// <returns>A new instance of <see cref="IStreamReader"/> to read the source, or null if there was an error</returns>
        private IStreamReader CreateStreamReader(SubscriptionDataSource subscriptionDataSource)
        {
            IStreamReader reader;
            switch (subscriptionDataSource.TransportMedium)
            {
                case SubscriptionTransportMedium.LocalFile:
                    reader = HandleLocalFileSource(subscriptionDataSource);
                    break;

                case SubscriptionTransportMedium.RemoteFile:
                    reader = HandleRemoteSourceFile(subscriptionDataSource);
                    break;

                case SubscriptionTransportMedium.Rest:
                    reader = new RestSubscriptionStreamReader(subscriptionDataSource.Source, subscriptionDataSource.Headers, _isLiveMode);
                    break;

                default:
                    throw new InvalidEnumArgumentException("Unexpected SubscriptionTransportMedium specified: " + subscriptionDataSource.TransportMedium);
            }
            return reader;
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
        /// Opens up an IStreamReader for a local file source
        /// </summary>
        private IStreamReader HandleLocalFileSource(SubscriptionDataSource source)
        {
            // handles zip or text files
            return new LocalFileSubscriptionStreamReader(_dataCacheProvider, source.Source);
        }

        /// <summary>
        /// Opens up an IStreamReader for a remote file source
        /// </summary>
        private IStreamReader HandleRemoteSourceFile(SubscriptionDataSource source)
        {
            SubscriptionDataSourceReader.CheckRemoteFileCache();

            try
            {
                // this will fire up a web client in order to download the 'source' file to the cache
                return new RemoteFileSubscriptionStreamReader(_dataCacheProvider, source.Source, Globals.Cache, source.Headers);
            }
            catch (Exception err)
            {
                OnInvalidSource(source, err);
                return null;
            }
        }
    }
}