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

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Defines how to open/close a source file
    /// </summary>
    public interface IStreamProvider : IDisposable
    {
        /// <summary>
        /// Opens the specified source as read to be consumed stream
        /// </summary>
        /// <param name="source">The source file to be opened</param>
        /// <returns>The stream representing the specified source</returns>
        IEnumerable<Stream> Open(string source);

        /// <summary>
        /// Closes the specified source file stream
        /// </summary>
        /// <param name="source">The source file to be closed</param>
        void Close(string source);
    }

    /// <summary>
    /// Provides factor method for creating an <see cref="IStreamProvider"/> from a file name
    /// </summary>
    public static class StreamProvider
    {
        /// <summary>
        /// Creates a new <see cref="IStreamProvider"/> capable of reading a file with the specified extenson
        /// </summary>
        /// <param name="extension">The file extension</param>
        /// <returns>A new stream provider capable of reading files with the specified extension</returns>
        public static IStreamProvider ForExtension(string extension)
        {
            var ext = Path.GetExtension(extension);
            if (ext == ".zip")
            {
                return new ZipStreamProvider();
            }
            if (ext == ".bz2")
            {
                return new Bz2StreamProvider();
            }
            if (ext == ".gz")
            {
                return new GzipStreamProvider();
            }

            return new FileStreamProvider();
        }
    }
}