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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

using Ionic.Zip;
using Ionic.Zlib;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// A cache provider that cache data points and serves with enumerator
    /// </summary>
    public class DataPointCacheProvider : IDataCacheProvider
    {
        /// <summary>
        /// The cache memory limit. default 256 MBs
        /// </summary>
        private const long DefaultCacheMemorySizeInMb = 256;

        /// <summary>
        /// The cache scanning interval. default 3 seconds
        /// </summary>
        private const int DefaultCachePollingIntervalInSecond = 3;

        /// <summary>
        /// The lifespan of an entry in the cache. Default 10 seconds
        /// </summary>
        private const int DefaultCacheSlidingExpirationInSecond = 10;

        /// <summary>
        /// The cache for data points by file name
        /// </summary>
        private readonly MemoryCache _dataPointCache;

        /// <summary>
        /// The cache policy that controls behavior of each entry
        /// </summary>
        private static readonly CacheItemPolicy CachePolicy = new CacheItemPolicy()
        {
            // No need to eject entry until the cache hits size limit
            // SlidingExpiration = TimeSpan.FromSeconds(Config.GetInt("history-data-cache-expiry", DefaultCacheSlidingExpirationInSecond)),
        };

        /// <summary>
        /// The data provider
        /// </summary>
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPointCacheProvider"/> class.
        /// </summary>
        /// <param name="dataProvider">The data provider</param>
        public DataPointCacheProvider(IDataProvider dataProvider)
        {
            this._dataProvider = dataProvider;
            this._dataPointCache = new MemoryCache(this.GetType().Name, new NameValueCollection()
            {
                { "CacheMemoryLimit", Config.Get("history-data-cache-size", DefaultCacheMemorySizeInMb.ToString()) },
                { "PollingInterval", Config.Get("history-data-cache-polling-interval", DefaultCachePollingIntervalInSecond.ToString())},
            });
        }

        /// <summary>
        /// Fetch data from the cache and return as an enumerator
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <param name="config">The subscription config</param>
        /// <param name="startDate">Provide the start date of data to be fetched. Inclusive.</param>
        /// <param name="endDate">Provide the end date of data to be fetched. Inclusive.</param>
        /// <returns>An enumerator of the cached data</returns>
        public IEnumerator<string> FetchEnumerator(string key, SubscriptionDataConfig config, DateTime? startDate, DateTime? endDate)
        {
            string fileName = key;
            string entryName = null; // Entry is not used
            IEnumerator<string> dataPointEnumerator = default(IEnumerator<string>);

            try
            {
                this.TryExtractEntryName(key, out fileName, out entryName);
                if (!string.Equals(fileName.GetExtension(), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Invalid file type. Only .zip is supported. Provided {fileName}");
                }

                // Create data point cache entry if not exists
                if (!this._dataPointCache.Contains(fileName))
                {
                    // handles zip files
                    using (Stream fileStream = this.CreateStream(this._dataProvider.Fetch(fileName), entryName, fileName))
                    {
                        if (fileStream != null)
                        {
                            DataPointDictionary newItem = new DataPointDictionary(fileStream, fileName, config.Resolution);
                            this._dataPointCache.Add(fileName, newItem, CachePolicy);
                        }
                    }
                }

                dataPointEnumerator = new DataPointEnumerator(
                    this._dataPointCache.Get(fileName) as DataPointDictionary,
                    config,
                    startDate,
                    endDate);
            }
            catch (Exception exception)
            {
                if (exception is ZipException || exception is ZlibException)
                {
                    Log.Error($"ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: {fileName}#{entryName}; Error: {exception}");
                }
                else
                {
                    Log.Error($"Error occurs getting enumerator for {fileName}; Details: {exception.ToString()}");
                }
            }

            return dataPointEnumerator;
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            this._dataPointCache.DisposeSafely();
        }

        /// <summary>
        /// Try to extract the entry name if exists in key
        /// </summary>
        /// <param name="key">The input key with file name and entry key, separated by '#'</param>
        /// <param name="fileName">The file name without entry name</param>
        /// <param name="entryName">The entry name</param>
        private void TryExtractEntryName(string key, out string fileName, out string entryName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(key);
            }

            int splitPos = key.LastIndexOf("#", StringComparison.Ordinal);
            entryName = (splitPos != -1) ? key.Substring(splitPos + 1) : string.Empty;
            fileName = (splitPos != -1) ? key.Substring(0, splitPos) : key;
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFileStream">The zip file stream containing ONLY one zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <param name="fileName">The name of the zip file on disk</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(Stream zipFileStream, string entryName, string fileName)
        {
            if (zipFileStream == null || !zipFileStream.CanRead)
            {
                throw new ArgumentException($"{nameof(zipFileStream)} is broken. Cannot create stream.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException($"fileName cannot be null or empty.");
            }

            ZipEntry entry = ZipFile.Read(zipFileStream).FirstOrDefault();
            MemoryStream stream = new MemoryStream();
            entry.OpenReader().CopyTo(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Fetch data from the cache as stream
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream FetchStream(string key)
        {
            throw new NotImplementedException();
        }
    }
}
