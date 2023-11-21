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
using Ionic.Zip;
using Ionic.Zlib;
using System.Linq;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Configuration;

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
        public ZipDataCacheProvider(IDataProvider dataProvider, bool isDataEphemeral = true, double cacheTimer = double.NaN)
        {
            IsDataEphemeral = isDataEphemeral;
            _cacheSeconds = double.IsNaN(cacheTimer) ? Config.GetDouble("zip-data-cache-provider", 10) : cacheTimer;
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
                                    existingZip.Refresh();
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
                    else
                    {
                        throw new InvalidOperationException($"Failed to store data {fileName}#{entryName}");
                    }

                    return;
                }

                lock (cachedZip)
                {
                    if (cachedZip.Disposed)
                    {
                        // if disposed and we have the lock means it's not in the dictionary anymore, let's assert it
                        // but there is a window for another thread to add a **new/different** instance which is okay
                        // we will pick it up on the store call bellow
                        if (_zipFileCache.TryGetValue(fileName, out var existing) && ReferenceEquals(existing, cachedZip))
                        {
                            Log.Error($"ZipDataCacheProvider.Store(): unexpected cache state for {fileName}");
                            throw new InvalidOperationException(
                                "LEAN entered an unexpected state. Please contact support@quantconnect.com so we may debug this further.");
                        }
                        Store(key, data);
                    }
                    else
                    {
                        cachedZip.WriteEntry(entryName, data);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of zip entries in a provided zip file
        /// </summary>
        public List<string> GetZipEntries(string zipFile)
        {
            if (!_zipFileCache.TryGetValue(zipFile, out var cachedZip))
            {
                if (!Cache(zipFile, out cachedZip))
                {
                    throw new ArgumentException($"Failed to get zip entries from {zipFile}");
                }
            }

            lock (cachedZip)
            {
                cachedZip.Refresh();
                return cachedZip.EntryCache.Keys.ToList();
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
                                // we first dispose it since if written it will refresh the file on disk and we don't
                                // want anyone reading it directly which should be covered by the entry being in the cache
                                // and us holding the instance lock
                                zip.Value.Dispose();
                                // removing it from the cache
                                _zipFileCache.TryRemove(zip.Key, out _);
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
                    var nextDueTime = Time.GetSecondUnevenWait((int)Math.Ceiling(_cacheSeconds * 1000));
                    _cacheCleaner.Change(nextDueTime, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // ignored disposed
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
                    // don't leak the file stream!
                    dataStream.DisposeSafely();
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
        private Stream CreateEntryStream(CachedZipFile zipFile, string entryName, string fileName)
        {
            ZipEntryCache entryCache;
            if (entryName == null)
            {
                entryCache = zipFile.EntryCache.FirstOrDefault().Value;
            }
            else
            {
                zipFile.EntryCache.TryGetValue(entryName, out entryCache);
            }

            if (entryCache is { Modified: true })
            {
                // we want to read an entry in the zip that has be edited, we need to start over
                // because of the zip library else it blows up, we need to call 'Save'
                zipFile.Dispose();
                _zipFileCache.Remove(fileName, out _);

                return CacheAndCreateEntryStream(fileName, entryName);
            }

            var entry = entryCache?.Entry;

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
            private ReferenceWrapper<DateTime> _dateCached;
            private readonly Stream _dataStream;
            private readonly string _filePath;
            private long _disposed;
            private long _modified;

            /// <summary>
            /// The ZipFile this object represents
            /// </summary>
            private readonly ZipFile _zipFile;

            /// <summary>
            /// Contains all entries of the zip file by filename
            /// </summary>
            public readonly Dictionary<string, ZipEntryCache> EntryCache = new (StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Returns if this cached zip file is disposed
            /// </summary>
            public bool Disposed => Interlocked.Read(ref _disposed) != 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="CachedZipFile"/>
            /// </summary>
            /// <param name="dataStream">Stream containing the zip file</param>
            /// <param name="utcNow">Current utc time</param>
            /// <param name="filePath">Path of the zip file</param>
            public CachedZipFile(Stream dataStream, DateTime utcNow, string filePath)
            {
                _dataStream = dataStream;
                _zipFile = ZipFile.Read(dataStream);
                _zipFile.UseZip64WhenSaving = Zip64Option.Always;
                foreach (var entry in _zipFile.Entries)
                {
                    EntryCache[entry.FileName] = new ZipEntryCache{ Entry = entry };
                }
                _dateCached = new ReferenceWrapper<DateTime>(utcNow);
                _filePath = filePath;
            }

            /// <summary>
            /// Method used to check if this object was created before a certain time
            /// </summary>
            /// <param name="date">DateTime which is compared to the DateTime this object was created</param>
            /// <returns>Bool indicating whether this object is older than the specified time</returns>
            public bool Uncache(DateTime date)
            {
                return _dateCached.Value < date;
            }

            /// <summary>
            /// Write to this entry, will be updated on disk when uncached
            /// Meaning either when timer finishes or on dispose
            /// </summary>
            /// <param name="entryName">Entry to write this as</param>
            /// <param name="content">Content of the entry</param>
            public void WriteEntry(string entryName, byte[] content)
            {
                Interlocked.Increment(ref _modified);
                Refresh();

                // If the entry already exists remove it 
                if (_zipFile.ContainsEntry(entryName))
                {
                    _zipFile.RemoveEntry(entryName);
                    EntryCache.Remove(entryName);
                }

                // Write this entry to zip file
                var newEntry = _zipFile.AddEntry(entryName, content);
                EntryCache.Add(entryName, new ZipEntryCache { Entry = newEntry, Modified = true });
            }

            /// <summary>
            /// We refresh our cache time when used to avoid it being clean up
            /// </summary>
            public void Refresh()
            {
                _dateCached = new ReferenceWrapper<DateTime>(DateTime.UtcNow);
            }

            /// <summary>
            /// Dispose of the ZipFile
            /// </summary>
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                {
                    // compare will return the original value, if it's already 1 means already being disposed off
                    return;
                }

                // If we changed this zip we need to save
                string tempFileName = null;
                var modified = Interlocked.Read(ref _modified) != 0;
                if (modified)
                {
                    // Write our changes to disk as temp
                    tempFileName = Path.GetTempFileName();
                    _zipFile.Save(tempFileName);
                }

                _zipFile?.DisposeSafely();
                _dataStream?.DisposeSafely();

                //After disposal we will move it to the final location
                if (modified && tempFileName != null)
                { 
                    File.Move(tempFileName, _filePath, true);
                }
            }
        }

        /// <summary>
        /// ZipEntry wrapper which handles flagging a modified entry
        /// </summary>
        private class ZipEntryCache
        {
            public ZipEntry Entry { get; set; }
            public bool Modified { get; set; }
        }
    }
}
