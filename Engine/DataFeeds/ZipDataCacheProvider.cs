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
using System.Collections.Generic;
using QuantConnect.Logging;
using System.Linq;
using System.Threading;
using Ionic.Zip;
using Ionic.Zlib;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// File provider implements optimized zip archives caching facility. Cache is thread safe.
    /// </summary>
    public class ZipDataCacheProvider : IDataCacheProvider
    {
        private const int CacheSeconds = 10;

        // ZipArchive cache used by the class
        private readonly ConcurrentDictionary<string, CachedZipFile> _zipFileCache = new ConcurrentDictionary<string, CachedZipFile>();
        private DateTime _nextCacheScan = DateTime.MinValue;
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral { get; }

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider, bool isDataEphemeral = true)
        {
            IsDataEphemeral = isDataEphemeral;
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
            if (filename.EndsWith(".zip"))
            {
                Stream stream = null;

                // scan the cache once every CacheSeconds
                var utcNow = DateTime.UtcNow;
                if (_nextCacheScan < utcNow)
                {
                    CleanCache(utcNow);
                }

                try
                {
                    CachedZipFile existingEntry;
                    if (!_zipFileCache.TryGetValue(filename, out existingEntry))
                    {
                        stream = CacheAndCreateStream(filename, entryName, utcNow);
                    }
                    else
                    {
                        try
                        {
                            lock (existingEntry)
                            {
                                if (existingEntry.Disposed)
                                {
                                    // bad luck, thread race condition
                                    // it was disposed and removed after we got it
                                    // so lets create it again and add it
                                    stream = CacheAndCreateStream(filename, entryName, utcNow);
                                }
                                else
                                {
                                    stream = CreateStream(existingEntry, entryName, filename);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            if (exception is ZipException || exception is ZlibException)
                            {
                                Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                            }
                            else throw;
                        }
                    }

                    return stream;
                }
                catch (Exception err)
                {
                    Log.Error(err, "Inner try/catch");
                    stream?.DisposeSafely();
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
            CachedZipFile zip;
            foreach (var zipFile in _zipFileCache)
            {
                if (_zipFileCache.TryRemove(zipFile.Key, out zip))
                {
                    zip.Dispose();
                }
            }
        }

        /// <summary>
        /// Remove items in the cache that are older than the cutoff date
        /// </summary>
        private void CleanCache(DateTime utcNow)
        {
            // we just want one thread cleaning the cache at the same time
            if (Monitor.TryEnter(_zipFileCache))
            {
                try
                {
                    var clearCacheIfOlderThan = utcNow.AddSeconds(-CacheSeconds);
                    // clean all items that that are older than CacheSeconds than the current date
                    foreach (var zip in _zipFileCache)
                    {
                        if (zip.Value.Uncache(clearCacheIfOlderThan))
                        {
                            // only clear items if they are not being used
                            if (Monitor.TryEnter(zip.Value))
                            {
                                try
                                {
                                    // removing it from the cache
                                    CachedZipFile removed;
                                    if (_zipFileCache.TryRemove(zip.Key, out removed))
                                    {
                                        // disposing zip archive
                                        removed.Dispose();
                                    }
                                }
                                finally
                                {
                                    Monitor.Exit(zip.Value);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(_zipFileCache);
                }

                _nextCacheScan = utcNow.AddSeconds(CacheSeconds);
            }
        }

        private Stream CacheAndCreateStream(string filename, string entryName, DateTime utcNow)
        {
            Stream stream = null;
            var dataStream = _dataProvider.Fetch(filename);

            if (dataStream != null)
            {
                try
                {
                    var newItem = new CachedZipFile(dataStream, filename, utcNow);

                    // here we don't need to lock over the cache item
                    // because it was still not added in the cache
                    stream = CreateStream(newItem, entryName, filename);

                    if (!_zipFileCache.TryAdd(filename, newItem))
                    {
                        // some other thread could of added it already, lets dispose ours
                        newItem.Dispose();
                    }
                }
                catch (Exception exception)
                {
                    if (exception is ZipException || exception is ZlibException)
                    {
                        Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" + entryName + " Error: " + exception);
                    }
                    else throw;
                }
            }
            return stream;
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFile">The zipFile containing the zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <param name="fileName">The name of the zip file on disk</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(CachedZipFile zipFile, string entryName, string fileName)
        {
            ZipEntry entry;
            if (entryName == null)
            {
                entry = zipFile.EntryCache.FirstOrDefault().Value;
            }
            else
            {
                zipFile.EntryCache.TryGetValue(entryName, out entry);
            }

            if (entry != null)
            {
                var stream = new MemoryStream();

                try
                {
                    stream.SetLength(entry.UncompressedSize);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // The needed size of the MemoryStream is longer than allowed.
                    // just read the data directly from the file.
                    // Note that we cannot use entry.OpenReader() because only one OpenReader
                    // can be open at a time without causing corruption.

                    // We must use fileName instead of zipFile.Name,
                    // because zipFile is initialized from a stream and not a file.
                    var zipStream = new ZipInputStream(fileName);

                    var zipEntry = zipStream.GetNextEntry();

                    // The zip file was empty!
                    if (zipEntry == null)
                    {
                        return null;
                    }

                    // Null entry name, return the first.
                    if (entryName == null)
                    {
                        return zipStream;
                    }

                    // Non-default entry name, return matching one if it exists, otherwise null.
                    while (zipEntry != null)
                    {
                        if (string.Compare(zipEntry.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return zipStream;
                        }

                        zipEntry = zipStream.GetNextEntry();
                    }
                }

                // extract directly into the stream
                entry.Extract(stream);
                stream.Position = 0;
                return stream;
            }

            return null;
        }
    }


    /// <summary>
    /// Type for storing zipfile in cache
    /// </summary>
    public class CachedZipFile : IDisposable
    {
        private readonly DateTime _dateCached;
        private readonly Stream _dataStream;

        /// <summary>
        /// The ZipFile this object represents
        /// </summary>
        private readonly ZipFile _zipFile;

        /// <summary>
        /// Contains all entries of the zip file by filename
        /// </summary>
        public readonly Dictionary<string, ZipEntry> EntryCache = new Dictionary<string, ZipEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Path to the ZipFile
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Returns if this cached zip file is disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedZipFile"/>
        /// </summary>
        /// <param name="dataStream">Stream containing the zip file</param>
        /// <param name="key">Key that represents the path to the data</param>
        /// <param name="utcNow">Current utc time</param>
        public CachedZipFile(Stream dataStream, string key, DateTime utcNow)
        {
            _dataStream = dataStream;
            _zipFile = ZipFile.Read(dataStream);
            foreach (var entry in _zipFile.Entries)
            {
                EntryCache[entry.FileName] = entry;
            }
            Key = key;
            _dateCached = utcNow;
        }

        /// <summary>
        /// Method used to check if this object was created before a certain time
        /// </summary>
        /// <param name="date">DateTime which is compared to the DateTime this object was created</param>
        /// <returns>Bool indicating whether this object is older than the specified time</returns>
        public bool Uncache(DateTime date)
        {
            return _dateCached < date;
        }

        /// <summary>
        /// Dispose of the ZipFile
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException("CachedZipFile instance is already disposed");
            }
            EntryCache.Clear();
            _zipFile?.DisposeSafely();
            _dataStream?.DisposeSafely();

            Key = null;
            Disposed = true;
        }
    }
}
