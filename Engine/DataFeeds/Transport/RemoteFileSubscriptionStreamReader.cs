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
using System.IO;
using System.Net;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capabable of downloading a remote file and then
    /// reading it from disk
    /// </summary>
    public class RemoteFileSubscriptionStreamReader : IStreamReader
    {
        private readonly IStreamReader _streamReader;

        /// <summary>
        /// Gets whether or not this stream reader should be rate limited
        /// </summary>
        public bool RateLimit => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="dataCacheProvider">The <see cref="IDataCacheProvider"/> used to retrieve a stream of data</param>
        /// <param name="source">The remote url to be downloaded via web client</param>
        /// <param name="downloadDirectory">The local directory and destination of the download</param>
        /// <param name="headers">Defines header values to add to the request</param>
        public RemoteFileSubscriptionStreamReader(IDataCacheProvider dataCacheProvider, string source, string downloadDirectory, IEnumerable<KeyValuePair<string, string>> headers)
        {
            // create a hash for a new filename
            var filename = Guid.NewGuid() + source.GetExtension();
            var destination = Path.Combine(downloadDirectory, filename);

            using (var client = new WebClient())
            {
                client.Proxy = WebRequest.GetSystemWebProxy();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.Headers.Add(header.Key, header.Value);
                    }
                }

                client.DownloadFile(source, destination);
            }

            // Send the file to the dataCacheProvider so it is available when the streamReader asks for it
            dataCacheProvider.Store(destination, File.ReadAllBytes(destination));

            // now we can just use the local file reader
            _streamReader = new LocalFileSubscriptionStreamReader(dataCacheProvider, destination);
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.RemoteFile"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.RemoteFile; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get { return _streamReader.EndOfStream; }
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream
        /// </summary>
        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        public void Dispose()
        {
            _streamReader.Dispose();
        }
    }
}
