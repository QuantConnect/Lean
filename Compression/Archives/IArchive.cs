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

namespace QuantConnect.Archives
{
    /// <summary>
    /// Represents a generic archive of streamable data
    /// </summary>
    public interface IArchive : IDisposable
    {
        /// <summary>
        /// Creates a new collection containing all of this archive's entries
        /// </summary>
        /// <returns>A new collection containing all of this archive's entries</returns>
        IReadOnlyCollection<IArchiveEntry> GetEntries();

        /// <summary>
        /// Gets the entry by the specified name or null if it does not exist
        /// </summary>
        /// <param name="key">The entry's key</param>
        /// <returns>The archive entry, or null if not found</returns>
        IArchiveEntry GetEntry(string key);
    }
}
