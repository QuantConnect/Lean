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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Storage
{
    /// <summary>
    /// Raw file handler
    /// </summary>
    /// <remarks>Useful to abstract file operations for <see cref="LocalObjectStore"/></remarks>
    public class FileHandler
    {
        /// <summary>
        /// True if the given file path exists
        /// </summary>
        public virtual bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Will delete the given file path
        /// </summary>
        public virtual void Delete(string path)
        {
            File.Delete(path);
        }

        /// <summary>
        /// Will write the given byte array at the target file path
        /// </summary>
        public virtual void WriteAllBytes(string path, byte[] data)
        {
            File.WriteAllBytes(path, data);
        }

        /// <summary>
        /// Read all bytes in the given file path
        /// </summary>
        public virtual byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Will try to fetch the given file length, will return 0 if it doesn't exist
        /// </summary>
        public virtual long TryGetFileLength(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo.Length;
            }
            return 0;
        }

        /// <summary>
        /// True if the given directory exists
        /// </summary>
        public virtual bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Create the requested directory path
        /// </summary>
        public virtual DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Enumerate the files in the target path
        /// </summary>
        public virtual IEnumerable<FileInfo> EnumerateFiles(string path, string pattern, SearchOption searchOption, out string rootfolder)
        {
            var directoryInfo = new DirectoryInfo(path);
            rootfolder = directoryInfo.FullName;
            return directoryInfo.EnumerateFiles(pattern, searchOption);
        }
    }
}
