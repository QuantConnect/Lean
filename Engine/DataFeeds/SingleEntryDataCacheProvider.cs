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
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Default implementation of the <see cref="IDataCacheProvider"/>
    /// Does not cache data.  If the data is a zip, the first entry is returned
    /// </summary>
    public class SingleEntryDataCacheProvider : IDataCacheProvider
    {
        private readonly IDataProvider _dataProvider;
        private ZipFile _zipFile;
        private Stream _zipFileStream;

        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral { get; }

        /// <summary>
        /// Constructor that takes the <see cref="IDataProvider"/> to be used to retrieve data
        /// </summary>
        public SingleEntryDataCacheProvider(IDataProvider dataProvider, bool isDataEphemeral = true)
        {
            _dataProvider = dataProvider;
            IsDataEphemeral = isDataEphemeral;
        }

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream Fetch(string key)
        {
            var stream = _dataProvider.Fetch(key);

            if (key.EndsWith(".zip") && stream != null)
            {
                // get the first entry from the zip file
                try
                {
                    var entryStream = Compression.UnzipStream(stream, out _zipFile);

                    // save the file stream so it can be disposed later
                    _zipFileStream = stream;

                    return entryStream;
                }
                catch (ZipException exception)
                {
                    Log.Error("SingleEntryDataCacheProvider.Fetch(): Corrupt file: " + key + " Error: " + exception);
                    stream.DisposeSafely();
                    return null;
                }
            }

            return stream;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data to cache as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _zipFile?.DisposeSafely();
            _zipFileStream?.DisposeSafely();
        }
    }
}
