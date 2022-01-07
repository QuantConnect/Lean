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

using System.IO;
using Ionic.Zip;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Simple data cache provider, writes and reads directly from disk
    /// Used as default for <see cref="LeanDataWriter"/>
    /// </summary>
    public class DiskDataCacheProvider : IDataCacheProvider
    {
        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral => false;

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        public Stream Fetch(string key)
        {
            LeanData.ParseKey(key, out var filePath, out var entryName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                using (var zip = ZipFile.Read(filePath))
                {
                    ZipEntry entry;
                    if (entryName.IsNullOrEmpty())
                    {
                        // Return the first entry
                        entry = zip[0];
                    }
                    else
                    {
                        // Attempt to find our specific entry
                        if (!zip.ContainsEntry(entryName))
                        {
                            return null;
                        }

                        entry = zip[entryName];
                    }

                    // Extract our entry and return it
                    var stream = new MemoryStream();
                    entry.Extract(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
            catch (ZipException exception)
            {
                Log.Error("DiskDataCacheProvider.Fetch(): Corrupt file: " + key + " Error: " + exception);
                return null;
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            LeanData.ParseKey(key, out var filePath, out var entryName);
            Compression.ZipCreateAppendData(filePath, entryName, data, true);
        }

        /// <summary>
        /// Dispose for this class
        /// </summary>
        public void Dispose()
        {
            //NOP
        }
    }
}
