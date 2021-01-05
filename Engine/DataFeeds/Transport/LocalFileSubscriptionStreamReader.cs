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

using System.IO;
using Ionic.Zip;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capable of reading lines from disk
    /// </summary>
    public class LocalFileSubscriptionStreamReader : IStreamReader
    {
        private readonly ZipFile _zipFile;

        /// <summary>
        /// Gets whether or not this stream reader should be rate limited
        /// </summary>
        public bool ShouldBeRateLimited => false;

        /// <summary>
        /// Direct access to the StreamReader instance
        /// </summary>
        public StreamReader StreamReader { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="dataCacheProvider">The <see cref="IDataCacheProvider"/> used to retrieve a stream of data</param>
        /// <param name="source">The local file to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// or to open the first zip entry found regardless of name</param>
        public LocalFileSubscriptionStreamReader(IDataCacheProvider dataCacheProvider, string source, string entryName = null)
        {
            var stream = dataCacheProvider.Fetch(source);

            if (stream != null)
            {
                StreamReader = new StreamReader(stream);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="dataCacheProvider">The <see cref="IDataCacheProvider"/> used to retrieve a stream of data</param>
        /// <param name="source">The local file to be read</param>
        /// <param name="startingPosition">The position in the stream from which to start reading</param>
        public LocalFileSubscriptionStreamReader(IDataCacheProvider dataCacheProvider, string source, long startingPosition)
        {
            var stream = dataCacheProvider.Fetch(source);

            if (stream != null)
            {
                StreamReader = new StreamReader(stream);

                if (startingPosition != 0)
                {
                    StreamReader.BaseStream.Seek(startingPosition, SeekOrigin.Begin);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="zipFile">The local zip archive to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// or to open the first zip entry found regardless of name</param>
        public LocalFileSubscriptionStreamReader(ZipFile zipFile, string entryName = null)
        {
            _zipFile = zipFile;
            var entry = _zipFile.Entries.FirstOrDefault(x => entryName == null || string.Compare(x.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0);
            if (entry != null)
            {
                var stream = new MemoryStream();
                entry.OpenReader().CopyTo(stream);
                stream.Position = 0;
                StreamReader = new StreamReader(stream);
            }
        }

        /// <summary>
        /// Returns the list of zip entries if local file stream reader is reading zip archive
        /// </summary>
        public IEnumerable<string> EntryFileNames
        {
            get
            {
                return _zipFile != null ? _zipFile.Entries.Select(x => x.FileName).ToList() : Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.LocalFile"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.LocalFile; }
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
            if (StreamReader != null)
            {
                StreamReader.Dispose();
                StreamReader = null;
            }
        }
    }
}