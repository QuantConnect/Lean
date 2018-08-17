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
using System.Collections.Concurrent;
using System.Threading;
using QuantConnect.Util;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides thread-safe access to multiple archive instance to enable reuse of the instance
    /// This saves tons of time opening up and reading the indexes of zip files just to throw them away
    /// This is also more efficient than having one zip file per path and locking around access to it
    /// </summary>
    public class ArchiveCache : IDisposable
    {
        private readonly ArchiveImplementation _impl;
        private readonly ConcurrentDictionary<string, CacheEntry> _archives;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveCache"/> class
        /// </summary>
        /// <param name="impl">The archive implementation to use</param>
        public ArchiveCache(ArchiveImplementation impl = ArchiveImplementation.DotNetFramework)
        {
            _impl = impl;
            _archives = new ConcurrentDictionary<string, CacheEntry>();
        }

        /// <summary>
        /// Gets the archive at the specified path. This archive instance will not be used by anyone else until it's returned
        /// </summary>
        /// <param name="path">The archive file path</param>
        /// <returns>An archive</returns>
        public IArchive Checkout(string path)
        {
            return _archives.GetOrAdd(path, p => new CacheEntry(path, _impl)).Checkout();
        }

        /// <summary>
        /// Returns the archive to the cache
        /// </summary>
        /// <param name="path">The archive's path</param>
        /// <param name="archive">The arhicve</param>
        public void Return(string path, IArchive archive)
        {
            _archives.AddOrUpdate(path, p => new CacheEntry(archive), (p, cache) => cache.Return(archive));
        }

        /// <summary>
        /// Diposes of all archives at the specified path that are currently returned
        /// </summary>
        /// <param name="path"></param>
        public void Dispose(string path)
        {
            CacheEntry entry;
            if (_archives.TryRemove(path, out entry))
            {
                entry.DisposeSafely();
            }
        }

        /// <summary>
        /// Disposes of all archives currently returned
        /// </summary>
        public void Dispose()
        {
            foreach (var kvp in _archives)
            {
                var path = kvp.Key;
                var cache = kvp.Value;

                cache.DisposeSafely();

                _archives.TryRemove(path, out cache);
            }
        }

        /// <summary>
        /// Defines an entry in the archive cache
        /// </summary>
        private class CacheEntry : IDisposable
        {
            private int _count;
            private readonly ArchiveImplementation _impl;
            private readonly ConcurrentQueue<IArchive> _archives;

            /// <summary>
            /// The total number of archives created by this entry instance
            /// </summary>
            public int Count => _count;

            /// <summary>
            /// The file path to the archive on disk
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheEntry"/> class
            /// </summary>
            /// <param name="path">The file path</param>
            /// <param name="impl">The archive implementations</param>
            public CacheEntry(string path, ArchiveImplementation impl)
            {
                Path = path;
                _impl = impl;
                _archives = new ConcurrentQueue<IArchive>();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CacheEntry"/> class
            /// </summary>
            /// <param name="archive">the archive</param>
            public CacheEntry(IArchive archive)
            {
                _archives = new ConcurrentQueue<IArchive>();
                _archives.Enqueue(archive);
            }

            /// <summary>
            /// Gets an archive instance for this path
            /// </summary>
            /// <returns>An archive</returns>
            public IArchive Checkout()
            {
                IArchive archive;
                if (_archives.TryDequeue(out archive))
                {
                    return archive;
                }

                Interlocked.Increment(ref _count);
                return Archive.OpenReadOnly(Path, _impl);
            }

            /// <summary>
            /// Returns an archive instance to the cache for reuse
            /// </summary>
            /// <param name="archive">The archive</param>
            /// <returns>Returns 'this' for ease-of-use with concurrent dictionary</returns>
            public CacheEntry Return(IArchive archive)
            {
                _archives.Enqueue(archive);
                return this;
            }

            /// <summary>
            /// Diposes of all currently returned archives
            /// </summary>
            public void Dispose()
            {
                IArchive archive;
                while (_archives.TryDequeue(out archive))
                {
                    archive.DisposeSafely();
                    Interlocked.Decrement(ref _count);
                }
            }
        }
    }
}