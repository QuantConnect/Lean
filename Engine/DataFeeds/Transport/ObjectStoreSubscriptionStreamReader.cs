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
using System.IO;
using Ionic.Zip;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capable of reading lines from the object store
    /// </summary>
    public class ObjectStoreSubscriptionStreamReader : IStreamReader
    {
        private IObjectStore _objectStore;
        private string _key;
        private StreamReader _streamReader;

        /// <summary>
        /// Gets whether or not this stream reader should be rate limited
        /// </summary>
        public bool ShouldBeRateLimited => false;

        /// <summary>
        /// Direct access to the StreamReader instance
        /// </summary>
        public StreamReader StreamReader
        {
            get
            {
                if (_streamReader == null && !string.IsNullOrEmpty(_key) && _objectStore.ContainsKey(_key))
                {
                    var data = _objectStore.ReadBytes(_key);
                    var stream = new MemoryStream(data);

                    if (_key.EndsWith(".zip", StringComparison.InvariantCulture))
                    {
                        using var zipFile = ZipFile.Read(stream);
                        // we only support single file zip files for now
                        var zipEntry = zipFile[0];
                        var tempStream = new MemoryStream();
                        zipEntry.Extract(tempStream);
                        tempStream.Position = 0;
                        _streamReader = new StreamReader(tempStream);

                        stream.DisposeSafely();
                    }
                    else
                    {
                        _streamReader = new StreamReader(stream);
                    }
                }

                return _streamReader;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectStoreSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="objectStore">The <see cref="IObjectStore"/> used to retrieve a stream of data</param>
        /// <param name="key">The object store key the data should be fetched from</param>
        public ObjectStoreSubscriptionStreamReader(IObjectStore objectStore, string key)
        {
            _objectStore = objectStore;
            _key = key;
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.LocalFile"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.ObjectStore; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get { return StreamReader == null || StreamReader.EndOfStream; }
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream
        /// </summary>
        public string ReadLine()
        {
            return StreamReader.ReadLine();
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        public void Dispose()
        {
            if (_streamReader != null)
            {
                _streamReader.Dispose();
                _streamReader = null;
            }
        }
    }
}
