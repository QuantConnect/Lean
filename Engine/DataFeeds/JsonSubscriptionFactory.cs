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
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// JSON Subscription Factory breaks a JSON object into pieces for the BaseData objects to consume.
    /// </summary>
    public class JsonSubscriptionFactory : ISubscriptionFactory
    {
        
        private readonly DateTime _date;
        private readonly bool _isLiveMode;
        private readonly BaseData _factory;
        private readonly SubscriptionDataConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSubscriptionFactory"/> class
        /// </summary>
        /// <param name="config">The subscription's configuration</param>
        /// <param name="date">The date this factory was produced to read data for</param>
        /// <param name="isLiveMode">True if we're in live mode, false for backtesting</param>
        public JsonSubscriptionFactory(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            _date = date;
            _config = config;
            _isLiveMode = isLiveMode;
            _factory = (BaseData)ObjectActivator.GetActivator(config.Type).Invoke(new object[0]);
        }

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
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            // Only handle REST JSON calls for now.
            if (source.TransportMedium != SubscriptionTransportMedium.Rest)
            {
                throw new NotImplementedException("Reading JSON from " + source.TransportMedium + " is not implemented.");
            }   

            // Convert to a collection string reader.
            var collection = source as SubscriptionDataSourceCollection;
            if (collection == null)
            {
                OnInvalidSource(source, new Exception("Json file format must be used with SubscriptionDataSourceCollection source."));
            }

            using (var reader = new RestSubscriptionStreamReader(source.Source))
            {
                var json = reader.ReadLine();
                foreach (var entry in collection.Explode(json, _config, _date, _isLiveMode))
                {
                    BaseData instance = null;
                    try
                    {
                        instance = _factory.Reader(_config, entry, _date, _isLiveMode);
                    }
                    catch (Exception err)
                    {
                        OnReaderError(entry, err);
                    }

                    if (instance != null)
                    {
                        yield return instance;
                    }
                }
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
