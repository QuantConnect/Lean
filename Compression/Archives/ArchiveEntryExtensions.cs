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
using System.Text;

namespace QuantConnect.Archives
{
    /// <summary>
    /// Provides extension methods for <see cref="IArchiveEntry"/>
    /// </summary>
    public static class ArchiveEntryExtensions
    {
        /// <summary>
        /// Reads the entire archive entry stream as a string
        /// </summary>
        /// <param name="entry">The entry to read</param>
        /// <param name="encoding">The encoding to use, specify null to use <see cref="Encoding.Default"/></param>
        /// <returns>The string contents of the archive entry</returns>
        public static string ReadAsString(this IArchiveEntry entry, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.Default;

            var memoryStream = new MemoryStream();
            using (var entryStream = entry.Read())
            {
                entryStream.CopyTo(memoryStream);
            }

            return encoding.GetString(memoryStream.ToArray());
        }

        /// <summary>
        /// Writes the specified string contents to the archive entry
        /// </summary>
        /// <param name="entry">The archive entry to write to</param>
        /// <param name="contents">The contents to write</param>
        /// <param name="encoding">The encoding to use, specify null to use <see cref="Encoding.Default"/></param>
        public static void WriteString(this IArchiveEntry entry, string contents, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.Default;

            var memoryStream = new MemoryStream(encoding.GetBytes(contents));
            entry.Write(memoryStream);
        }
    }
}