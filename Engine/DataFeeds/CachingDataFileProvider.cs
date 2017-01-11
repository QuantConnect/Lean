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
using QuantConnect.Interfaces;
using QuantConnect.Data;
using System.IO;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using System.Collections.Concurrent;
using System.IO.Compression;
using QuantConnect.Logging;
using System.Linq;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    using CacheEntry = Tuple<DateTime, ZipArchive>;

    /// <summary>
    /// File provider implements optimized zip archives caching facility
    /// </summary>
    public class CachingDataFileProvider : IDataFileProvider
    {
        // ZipArchive cache used by UnzipCached method
        private ConcurrentDictionary<string, Lazy<CacheEntry>> _zipArchiveCache = new ConcurrentDictionary<string, Lazy<CacheEntry>>();
        private DateTime lastDate = DateTime.MinValue;

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public IStreamReader Fetch(Symbol symbol, SubscriptionDataSource source, DateTime date, Resolution resolution, TickType tickType)
        {
            string entryName = null; // default to all entries
            var filename = source.Source;
            var hashIndex = source.Source.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = source.Source.Substring(hashIndex + 1);
                filename = source.Source.Substring(0, hashIndex);
            }

            if (!File.Exists(filename))
            {
                return null;
            }

            // handles zip files
            if (filename.GetExtension() == ".zip")
            {
                IStreamReader reader = null;

                try
                {
                    // cleaning the outdated cache items
                    if (lastDate == DateTime.MinValue || lastDate < date.Date)
                    {
                        foreach (var zip in _zipArchiveCache.Where(x => x.Value.Value.Item1 < date.Date))
                        {
                            // disposing zip archive
                            zip.Value.Value.Item2.Dispose();

                            // removing it from the cache
                            Lazy<CacheEntry> removed;
                            _zipArchiveCache.TryRemove(zip.Key, out removed);
                        }

                        lastDate = date.Date;
                    }

                    _zipArchiveCache.AddOrUpdate(filename,
                        x =>
                        {
                            var file = File.OpenRead(filename);
                            // reading up first two bytes 
                            // http://george.chiramattel.com/blog/2007/09/deflatestream-block-length-does-not-match.html
                            file.ReadByte(); file.ReadByte();

                            var newItem = Tuple.Create(date.Date, new ZipArchive(file));
                            reader = new LocalFileSubscriptionStreamReader(newItem.Item2, entryName);
                            return newItem;
                        },
                        (x, existingEntry) =>
                        {
                            reader = new LocalFileSubscriptionStreamReader(existingEntry.Item2, entryName);
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
                return new LocalFileSubscriptionStreamReader(filename, entryName);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            foreach (var zip in _zipArchiveCache)
            {
                zip.Value.Value.Item2.Dispose();
            }

            _zipArchiveCache.Clear();
        }
    }
}
