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

using System.IO;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Represents a single entry in an archive
    /// </summary>
    public interface IArchiveEntry
    {
        /// <summary>
        /// Gets the entry's key
        /// </summary>
        string Key { get; }

        /// <summary>
        /// True if the archive contains and entry with this key, false if it does not exist
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Opens the entry's stream for reading
        /// </summary>
        /// <returns>The entry's stream</returns>
        Stream Read();

        /// <summary>
        /// Writes the specified stream to the entry as the FULL contents of the entry
        /// </summary>
        /// <param name="stream">The data stream to write</param>
        void Write(Stream stream);
    }
}