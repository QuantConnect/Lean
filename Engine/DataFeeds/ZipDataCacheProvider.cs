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
using System.IO;
using System.Collections.Concurrent;
using QuantConnect.Logging;
using System.Linq;
using Ionic.Zip;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    // Cache entry tuple contains: date of the cache entry, and reference to .net zip archive
    using CacheEntry = Tuple<DateTime, ZipFile>;

    /// <summary>
    /// File provider implements optimized zip archives caching facility. Cache is thread safe.
    /// </summary>
    public class ZipDataCacheProvider : IDataCacheProvider
    {
        private const int CacheSeconds = 10;

        // ZipArchive cache used by the class
        private readonly ConcurrentDictionary<string, Lazy<CacheEntry>> _zipFileCache = new ConcurrentDictionary<string, Lazy<CacheEntry>>();
        private DateTime _lastDate = DateTime.MinValue;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream Fetch(string key)
        {
            string entryName = null; // default to all entries
            var filename = key;
            var hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = key.Substring(hashIndex + 1);
                filename = key.Substring(0, hashIndex);
            }

            // handles zip files
            if (filename.GetExtension() == ".zip")
            {
                Stream stream = null;

                var date = DateTime.Now.Date.AddSeconds(-CacheSeconds);

                CleanCache(date);

                try
                {
                    if (_zipFileCache.ContainsKey(filename))
                    {
                        Lazy<CacheEntry> existingEntry;
                        _zipFileCache.TryGetValue(filename, out existingEntry);
                        stream = CreateStream(existingEntry.Value.Item2, entryName);
                    }
                    else
                    {
                        var dataStream = _dataProvider.Fetch(filename);
                        if (dataStream == null)
                        {
                            return null;
                        }
                        var zipFile = ZipFile.Read(dataStream);
                        var newItem = Tuple.Create(date.Date, zipFile);
                        stream = CreateStream(newItem.Item2, entryName);
                    }

                    return stream;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Inner try/catch");
                    if (stream != null) stream.Dispose();
                    return null;
                }
            }
            else
            {
                // handles text files
                return _dataProvider.Fetch(filename);
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            foreach (var zip in _zipFileCache)
            {
                zip.Value.Value.Item2.Dispose();
            }

            _zipFileCache.Clear();
        }

        /// <summary>
        /// Remove items in the cache that are older than the cutoff date
        /// </summary>
        /// <param name="date">The cutoff date</param>
        private void CleanCache(DateTime date)
        {
            // cleaning the outdated cache items
            if (_lastDate == DateTime.MinValue || _lastDate < date)
            {
                // clean all items that that are older than _cachePeriodBars bars than the current date
                foreach (var zip in _zipFileCache.Where(x => x.Value.Value.Item1 < date))
                {
                    // removing it from the cache
                    Lazy<CacheEntry> removed;
                    if (_zipFileCache.TryRemove(zip.Key, out removed))
                    {
                        // disposing zip archive
                        removed.Value.Item2.Dispose();
                    }
                }

                _lastDate = date.Date;
            }
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFile">The zipFile containing the zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(ZipFile zipFile, string entryName)
        {
            var entry = zipFile.Entries.FirstOrDefault(x => entryName == null || string.Compare(x.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0);
            if (entry != null)
            {
                var stream = new MemoryStream();
                entry.OpenReader().CopyTo(stream);
                stream.Position = 0;
                return stream;
            }

            return null;
        }
    }
}