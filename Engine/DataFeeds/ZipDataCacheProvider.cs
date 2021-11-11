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
        private readonly double _cacheSeconds;

        // ZipArchive cache used by the class
        private readonly ConcurrentDictionary<string, CachedZipFile> _zipFileCache = new ConcurrentDictionary<string, CachedZipFile>();
        private readonly IDataProvider _dataProvider;
        private readonly Timer _cacheCleaner;

        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral { get; }

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider, bool isDataEphemeral = true, double cacheTimer = 10)
        {
            IsDataEphemeral = isDataEphemeral;
            _cacheSeconds = cacheTimer;
            _dataProvider = dataProvider;
            _cacheCleaner = new Timer(state => CleanCache(), null, TimeSpan.FromSeconds(_cacheSeconds), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream Fetch(string key)
        {
            LeanData.ParseKey(key, out var filename, out var entryName);

            // handles zip files
            if (filename.EndsWith(".zip", StringComparison.InvariantCulture))
            {
                Stream stream = null;

                try
                {
                    CachedZipFile existingZip;
                    if (!_zipFileCache.TryGetValue(filename, out existingZip))
                    {
                        stream = CacheAndCreateEntryStream(filename, entryName);
                    }
                    else
                    {
                        try
                        {
                            lock (existingZip)
                            {
                                if (existingZip.Disposed)
                                {
                                    // bad luck, thread race condition
                                    // it was disposed and removed after we got it
                                    // so lets create it again and add it
                                    stream = CacheAndCreateEntryStream(filename, entryName);
                                }
                                else
                                {
                                    stream = CreateEntryStream(existingZip, entryName, filename);
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
        /// Store the data in the cache.
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            LeanData.ParseKey(key, out var fileName, out var entryName);

            // We only support writing to zips with this provider, we also need an entryName to write
            // Return silently because RemoteFileSubscriptionStreamReader depends on this function not throwing.
            if (!fileName.EndsWith(".zip", StringComparison.InvariantCulture) || entryName == null)
            {
                return;
            }

            // Only allow one thread at a time to modify our cache
            lock (_zipFileCache)
            {
                // If its not in the cache, and can not be cached we need to create it
                if (!_zipFileCache.TryGetValue(fileName, out var cachedZip) && !Cache(fileName, out cachedZip))
                {
                    // Create the zip, if successful, cache it for later use
                    if (Compression.ZipCreateAppendData(fileName, entryName, data))
                    {
                        Cache(fileName, out _);
                    }

                    return;
                }

                cachedZip.WriteEntry(entryName, data);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            // stop the cache cleaner timer
            _cacheCleaner.DisposeSafely();
            CachedZipFile zip;
            foreach (var zipFile in _zipFileCache)
            {
                if (_zipFileCache.TryRemove(zipFile.Key, out zip))
                {
                    zip.DisposeSafely();
                }
            }
        }

        /// <summary>
        /// Remove items in the cache that are older than the cutoff date
        /// </summary>
        private void CleanCache()
        {
            var utcNow = DateTime.UtcNow;
            try
            {
                var clearCacheIfOlderThan = utcNow.AddSeconds(-_cacheSeconds);
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
                try
                {
                    _cacheCleaner.Change(TimeSpan.FromSeconds(_cacheSeconds), Timeout.InfiniteTimeSpan);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private Stream CacheAndCreateEntryStream(string filename, string entryName)
        {
            Stream stream = null;
            var dataStream = _dataProvider.Fetch(filename);

            if (dataStream != null)
            {
                try
                {
                    var newItem = new CachedZipFile(dataStream, DateTime.UtcNow, filename);

                    // here we don't need to lock over the cache item
                    // because it was still not added in the cache
                    stream = CreateEntryStream(newItem, entryName, filename);

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
        private static Stream CreateEntryStream(CachedZipFile zipFile, string entryName, string fileName)
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

        /// <summary>
        /// Cache a Zip
        /// </summary>
        /// <param name="filename">Zip to cache</param>
        /// <param name="cachedZip">The resulting CachedZipFile</param>
        /// <returns></returns>
        private bool Cache(string filename, out CachedZipFile cachedZip)
        {
            cachedZip = null;
            var dataStream = _dataProvider.Fetch(filename);
            if (dataStream != null)
            {
                try
                {
                    cachedZip = new CachedZipFile(dataStream, DateTime.UtcNow, filename);

                    if (!_zipFileCache.TryAdd(filename, cachedZip))
                    {
                        // some other thread could of added it already, lets dispose ours
                        cachedZip.Dispose();
                        return _zipFileCache.TryGetValue(filename, out cachedZip);
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    if (exception is ZipException || exception is ZlibException)
                    {
                        Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + " Error: " + exception);
                    }
                    else throw;
                }

                dataStream.Dispose();
            }

            return false;
        }


        /// <summary>
        /// Type for storing zipfile in cache
        /// </summary>
        private class CachedZipFile : IDisposable
        {
            private readonly DateTime _dateCached;
            private readonly Stream _dataStream;
            private bool _modified;
            private string _filePath;

            /// <summary>
            /// The ZipFile this object represents
            /// </summary>
            private readonly ZipFile _zipFile;

            /// <summary>
            /// Contains all entries of the zip file by filename
            /// </summary>
            public readonly Dictionary<string, ZipEntry> EntryCache = new Dictionary<string, ZipEntry>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Returns if this cached zip file is disposed
            /// </summary>
            public bool Disposed { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CachedZipFile"/>
            /// </summary>
            /// <param name="dataStream">Stream containing the zip file</param>
            /// <param name="utcNow">Current utc time</param>
            /// <param name="filePath">Path of the zip file</param>
            public CachedZipFile(Stream dataStream, DateTime utcNow, string filePath)
            {
                _modified = false;
                _dataStream = dataStream;
                _zipFile = ZipFile.Read(dataStream);
                foreach (var entry in _zipFile.Entries)
                {
                    EntryCache[entry.FileName] = entry;
                }
                _dateCached = utcNow;
                _filePath = filePath;
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
            /// Write to this entry, will be updated on disk when uncached
            /// Meaning either when timer finishes or on dispose
            /// </summary>
            /// <param name="entryName">Entry to write this as</param>
            /// <param name="content">Content of the entry</param>
            public void WriteEntry(string entryName, byte[] content)
            {
                // If the entry already exists remove it 
                if (_zipFile.ContainsEntry(entryName))
                {
                    _zipFile.RemoveEntry(entryName);
                    EntryCache.Remove(entryName);
                }

                // Write this entry to zip file
                var newEntry = _zipFile.AddEntry(entryName, content);
                EntryCache.Add(entryName, newEntry);

                _modified = true;
            }

            /// <summary>
            /// Dispose of the ZipFile
            /// </summary>
            public void Dispose()
            {
                if (Disposed)
                {
                    return;
                }

                // If we changed this zip we need to save
                string tempFileName = null;
                if (_modified)
                {
                    // Write our changes to disk as temp
                    tempFileName = Path.GetTempFileName();
                    _zipFile.Save(tempFileName);
                }

                EntryCache.Clear();
                _zipFile?.DisposeSafely();
                _dataStream?.DisposeSafely();

                //After disposal we will move it to the final location
                if (_modified && tempFileName != null)
                { 
                    File.Move(tempFileName, _filePath, true);
                }

                Disposed = true;
            }
        }
    }
}
