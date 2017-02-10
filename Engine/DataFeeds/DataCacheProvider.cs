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
using System.IO;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
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
    public class DataCacheProvider : IDataCacheProvider
    {
        private const int CachePeriodBars = 10;

        // ZipArchive cache used by the class
        private readonly ConcurrentDictionary<string, Lazy<CacheEntry>> _zipFileCache = new ConcurrentDictionary<string, Lazy<CacheEntry>>();
        private DateTime _lastDate = DateTime.MinValue;

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream Fetch(string source, string entryName)
        {
            //entryName = null; // default to all entries
            var filename = source;
            var hashIndex = source.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = source.Substring(hashIndex + 1);
                filename = source.Substring(0, hashIndex);
            }

            if (!File.Exists(filename))
            {
                return null;
            }

            // handles zip files
            if (filename.GetExtension() == ".zip")
            {
                Stream reader = null;

                var date = DateTime.Now.Date.AddSeconds(-CachePeriodBars);
                //// cleaning the outdated cache items
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

                try
                {
                    _zipFileCache.AddOrUpdate(filename,
                        x =>
                        {
                            var newItem = Tuple.Create(date.Date, new ZipFile(filename));
                            reader = CreateStream(newItem.Item2, entryName);
                            return newItem;
                        },
                        (x, existingEntry) =>
                        {
                            reader = CreateStream(existingEntry.Item2, entryName);
                            return existingEntry;
                        });

                    return reader;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Inner try/catch");
                    if (reader != null) reader.Dispose();
                    return null;
                }
            }
            else
            {
                // handles text files
                return new FileStream(source, FileMode.Open, FileAccess.Read);
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implementated in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="source">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string source, byte[] data)
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

//// cleaning the outdated cache items
//if (_lastDate == DateTime.MinValue || _lastDate < date.Date)
//{
//    // clean all items that that are older than _cachePeriodBars bars than the current date
//    foreach (var zip in _zipFileCache.Where(x => x.Value.Value.Item1 < date.Date.AddDays(-CachePeriodBars)))
//    {
//        // removing it from the cache
//        Lazy<CacheEntry> removed;
//        if (_zipFileCache.TryRemove(zip.Key, out removed))
//        {
//            // disposing zip archive
//            removed.Value.Item2.Dispose();
//        }
//    }

//    _lastDate = date.Date;
//}