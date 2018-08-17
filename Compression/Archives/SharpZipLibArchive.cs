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
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using QuantConnect.Util;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides an implementation of <see cref="IArchive"/> that uses <see cref="ZipFile"/> from ICSharpZipLib
    /// </summary>
    public class SharpZipLibArchive : IArchive
    {
        private readonly ZipFile _zipFile;
        private readonly ZipOutputStream _zipOutputStream;
        private readonly Dictionary<string, IArchiveEntry> _archiveEntriesByKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpZipLibArchive"/> class
        /// </summary>
        /// <remarks>
        ///This is read-only constructor
        /// </remarks>
        /// <param name="zipFile">The zip file</param>
        public SharpZipLibArchive(ZipFile zipFile)
        {
            _zipFile = zipFile;
            _archiveEntriesByKey = new Dictionary<string, IArchiveEntry>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharpZipLibArchive"/> class
        /// </summary>
        /// <remarks>
        /// This is write-only constructor
        /// </remarks>
        /// <param name="zipOutputStream">The zip output stream</param>
        public SharpZipLibArchive(ZipOutputStream zipOutputStream)
        {
            _zipOutputStream = zipOutputStream;
            _archiveEntriesByKey = new Dictionary<string, IArchiveEntry>();
        }

        /// <summary>
        /// Creates a new collection containing all of this archive's entries
        /// </summary>
        /// <returns>A new collection containing all of this archive's entries</returns>
        public IReadOnlyCollection<IArchiveEntry> GetEntries()
        {
            if (_zipFile == null)
            {
                throw new InvalidOperationException("Unable to get entries of SharpZipLibArchive in write-only mode.");
            }

            var entries = new List<IArchiveEntry>();
            var enumerator = _zipFile.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Current as ZipEntry;
                if (entry != null)
                {
                    entries.Add(new Entry(entry.Name, _zipFile, null, entry));
                }
            }

            return entries.AsReadOnly();

        }

        /// <summary>
        /// Gets the entry by the specified name or null if it does not exist
        /// </summary>
        /// <param name="key">The entry's key</param>
        /// <returns>The archive entry, or null if not found</returns>
        public IArchiveEntry GetEntry(string key)
        {
            ZipEntry entry = null;
            if (_zipFile != null)
            {
                entry = _zipFile.GetEntry(key);
            }

            return new Entry(key, _zipFile, _zipOutputStream, entry);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _zipOutputStream?.Finish();
            _zipOutputStream?.DisposeSafely();
            _zipFile?.DisposeSafely();
        }

        /// <summary>
        /// Defines an entry in a <see cref="ZipFile"/>
        /// </summary>
        private class Entry : IArchiveEntry
        {
            private ZipEntry _entry;
            private readonly ZipFile _zipFile;
            private readonly ZipOutputStream _zipOutputStream;

            /// <summary>
            /// Gets the entry's key
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// True if the archive contains and entry with this key, false if it does not exist
            /// </summary>
            public bool Exists => _entry != null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class
            /// </summary>
            /// <param name="key">The entry's key</param>
            /// <param name="zipFile">The zip file</param>
            /// <param name="outputStream">The output stream for writing</param>
            /// <param name="entry">The zip entry, null if the entry hasn't been 'put' yet</param>
            public Entry(string key, ZipFile zipFile, ZipOutputStream outputStream, ZipEntry entry)
            {
                Key = key;
                _entry = entry;
                _zipFile = zipFile;
                _zipOutputStream = outputStream;
            }

            /// <summary>
            /// Opens the entry's stream for reading
            /// </summary>
            /// <returns>The entry's stream</returns>
            public Stream Read()
            {
                return _zipFile.GetInputStream(_entry);
            }

            /// <summary>
            /// Writes the specified stream to the entry as the FULL contents of the entry
            /// </summary>
            /// <param name="stream">The data stream to write</param>
            public void Write(Stream stream)
            {
                if (_entry == null)
                {
                    _entry = new ZipEntry(Key);
                    _zipOutputStream.PutNextEntry(_entry);
                }
                else
                {
                    throw new NotSupportedException("SharpZipLibArchive does not support updating entries after they've been written");
                }

                stream.CopyTo(_zipOutputStream);
            }
        }
    }
}