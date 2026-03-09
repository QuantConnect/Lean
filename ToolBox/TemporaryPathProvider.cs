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
using System.Threading.Tasks;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Helper method that provides and cleans given temporary paths
    /// </summary>
    public static class TemporaryPathProvider
    {
        private static readonly Queue<string> TemporaryPaths = new Queue<string>();

        // Gets a new temporary path
        public static string Get()
        {
            var newPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToStringInvariant(null));
            lock (TemporaryPaths)
            {
                TemporaryPaths.Enqueue(newPath);
            }
            return newPath;
        }

        /// <summary>
        /// Recursively deletes all the given temporary paths
        /// </summary>
        public static void Delete()
        {
            List<string> paths;
            lock (TemporaryPaths)
            {
                paths = TemporaryPaths.ToList(s => s);
                TemporaryPaths.Clear();
            }
            Parallel.ForEach(paths, path =>
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch
                {
                    // pass
                }
            });
        }
    }
}
