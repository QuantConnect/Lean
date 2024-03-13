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

using QuantConnect.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace QuantConnect
{
    /// <summary>
    /// Helper methods for file management
    /// </summary>
    public static class FileExtension
    {
        public static readonly string ReservedWordsPrefix = Config.Get("reserved-words-prefix", "@");
        private static readonly Regex ToValidWindowsPathRegex = new Regex("((?<=(\\\\|/|^))(CON|PRN|AUX|NUL|(COM[0-9])|(LPT[0-9]))(?=(\\.|\\\\|/|$)))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly string _fixPathRegex = ReservedWordsPrefix + "$&"; // The string "$&" gets the matched word
        private static readonly bool _isWindows = OS.IsWindows;

        /// <summary>
        /// Takes a given path and (if applicable) returns a modified path accepted by
        /// Windows OS
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToNormalizedPath(string path)
        {
            return _isWindows ? ToValidWindowsPathRegex.Replace(path, _fixPathRegex) : path;
        }

        /// <summary>
        /// Takes a modified path (see <see cref="ToNormalizedPath(string)"/>) and (if applicable)
        /// returns the original path proposed by LEAN
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FromNormalizedPath(string path)
        {
            return _isWindows ? path.Replace(ReservedWordsPrefix, string.Empty) : path;
        }
    }
}
