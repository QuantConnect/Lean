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

using System;
using System.IO;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines a cache for data
    /// </summary>
    public interface IDataCacheProvider : IDisposable
    {
        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached
        /// </summary>
        bool IsDataEphemeral { get; }

        /// <summary>
        /// Fetch data from the cache
        /// </summary>
        /// <param name="key">A string representing the key of the cached data</param>
        /// <returns>An <see cref="Stream"/> of the cached data</returns>
        Stream Fetch(string key);

        /// <summary>
        /// Store the data in the cache
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data to cache as a byte array</param>
        void Store(string key, byte[] data);
    }

    // TODO: Better place for this?
    public static class DataCacheProviderExtensions {
        /// <summary>
        /// Helper to separate filename and entry from a given key
        /// </summary>
        /// <param name="key">The key to parse</param>
        /// <param name="fileName">File name extracted</param>
        /// <param name="entryName">Entry name extracted</param>
        public static void ParseKey(string key, out string fileName, out string entryName)
        {
            // Default scenario, no entryName included in key
            entryName = null; // default to all entries
            fileName = key;

            if (key == null)
            {
                return;
            }

            // Try extracting an entry name; Anything after a # sign
            var hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = key.Substring(hashIndex + 1);
                fileName = key.Substring(0, hashIndex);
            }
        }
    }
}
