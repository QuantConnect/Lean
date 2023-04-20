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
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Helper class for handling mapfile zip files
    /// </summary>
    public static class MapFileZipHelper
    {
        /// <summary>
        /// Gets the mapfile zip filename for the specified date
        /// </summary>
        public static string GetMapFileZipFileName(string market, DateTime date, SecurityType securityType)
        {
            return Path.Combine(Globals.DataFolder, MapFile.GetRelativeMapFilePath(market, securityType), $"map_files_{date:yyyyMMdd}.zip");
        }

        /// <summary>
        /// Reads the zip bytes as text and parses as MapFileRows to create MapFiles
        /// </summary>
        public static IEnumerable<MapFile> ReadMapFileZip(Stream file, string market, SecurityType securityType)
        {
            if (file == null || file.Length == 0)
            {
                return Enumerable.Empty<MapFile>();
            }

            var result = from kvp in Compression.Unzip(file)
                   let filename = kvp.Key
                   where filename.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase)
                   let lines = kvp.Value.Where(line => !string.IsNullOrEmpty(line))
                   let mapFile = SafeRead(filename, lines, market, securityType)
                   select mapFile;
            return result;
        }

        /// <summary>
        /// Parses the contents as a MapFile, if error returns a new empty map file
        /// </summary>
        private static MapFile SafeRead(string filename, IEnumerable<string> contents, string market, SecurityType securityType)
        {
            var permtick = Path.GetFileNameWithoutExtension(filename);
            try
            {
                return new MapFile(permtick, contents.Select(s => MapFileRow.Parse(s, market, securityType)));
            }
            catch
            {
                return new MapFile(permtick, Enumerable.Empty<MapFileRow>());
            }
        }
    }
}
