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
using System.Linq;
using Ionic.Zip;
using QuantConnect.Util;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides an implementation of <see cref="IArchive"/> that uses <see cref="ZipFile"/> from Ionic
    /// </summary>
    public class IonicZipArchive : IArchive
    {
        private readonly ZipFile _zipFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonicZipArchive"/> class
        /// </summary>
        /// <param name="zipFile">The zip file</param>
        public IonicZipArchive(ZipFile zipFile)
        {
            _zipFile = zipFile;
        }

        /// <summary>
        /// Creates a new collection containing all of this archive's entries
        /// </summary>
        /// <returns>A new collection containing all of this archive's entries</returns>
        public IReadOnlyCollection<IArchiveEntry> GetEntries()
        {
            return _zipFile.Entries.Select(entry => new Entry(entry.FileName, _zipFile, entry)).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the entry by the specified name or null if it does not exist
        /// </summary>
        /// <param name="key">The entry's key</param>
        /// <returns>The archive entry, or null if not found</returns>
        public IArchiveEntry GetEntry(string key)
        {
            return new Entry(key, _zipFile, _zipFile[key]);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _zipFile.Save();
            _zipFile.DisposeSafely();
        }

        /// <summary>
        /// Defines an entry in a <see cref="IonicZipArchive"/>
        /// </summary>
        private class Entry : IArchiveEntry
        {
            private ZipEntry _entry;
            private readonly ZipFile _zipFile;

            /// <summary>
            /// Gets the entry's key
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// True if the archive contains an entry with this key, false if it does not exist
            /// </summary>
            public bool Exists => _entry != null;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class
            /// </summary>
            /// <param name="key">The zip entry name</param>
            /// <param name="zipFile">The zip file</param>
            /// <param name="entry">The zip entry</param>
            public Entry(string key, ZipFile zipFile, ZipEntry entry)
            {
                Key = key;
                _zipFile = zipFile;
                _entry = entry;
            }

            /// <summary>
            /// Opens the entry's stream for reading
            /// </summary>
            /// <returns>The entry's stream</returns>
            public Stream Read()
            {
                if (_entry == null)
                {
                    throw new InvalidOperationException("Unable to read from zip entry because it does not exist.");
                }

                return _entry.OpenReader();
            }

            /// <summary>
            /// Writes the specified stream to the entry as the FULL contents of the entry
            /// </summary>
            /// <param name="stream">The data stream to write</param>
            public void Write(Stream stream)
            {
                if (_entry != null)
                {
                    _zipFile.RemoveEntry(_entry);
                }

                _entry = _zipFile.AddEntry(Key, stream);
            }
        }
    }
}