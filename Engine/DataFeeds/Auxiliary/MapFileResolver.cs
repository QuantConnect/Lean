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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds.Auxiliary
{
    /// <summary>
    /// Provides a means of mapping a symbol at a point in time to the map file
    /// containing that share class's mapping information
    /// </summary>
    public class MapFileResolver
    {
        private readonly Dictionary<string, MapFile> _mapFilesByPath;
        private readonly Dictionary<string, SortedList<DateTime, MapFileRowEntry>> _bySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFileResolver"/> by reading
        /// in all files in the specified directory.
        /// </summary>
        /// <param name="mapFilesByMapFilePath">The data used to initialize this resolver. Each key 
        /// is the path to the map file and each value is the data contained in the map file</param>
        public MapFileResolver(IEnumerable<KeyValuePair<string, MapFile>> mapFilesByMapFilePath)
        {
            _mapFilesByPath = new Dictionary<string, MapFile>();
            _bySymbol = new Dictionary<string, SortedList<DateTime, MapFileRowEntry>>();

            foreach (var kvp in mapFilesByMapFilePath)
            {
                var mapFile = kvp.Value;
                var mapFilePath = Path.GetFullPath(kvp.Key);

                // add to our by path map
                _mapFilesByPath.Add(mapFilePath, mapFile);

                foreach (var row in mapFile)
                {
                    SortedList<DateTime, MapFileRowEntry> entries;
                    var mapFileRowEntry = new MapFileRowEntry(mapFilePath, row);

                    if (!_bySymbol.TryGetValue(row.MappedSymbol, out entries))
                    {
                        entries = new SortedList<DateTime, MapFileRowEntry>();
                        _bySymbol[row.MappedSymbol] = entries;
                    }

                    if (entries.ContainsKey(mapFileRowEntry.MapFileRow.Date))
                    {
                        // check to verify it' the same data
                        if (!entries[mapFileRowEntry.MapFileRow.Date].Equals(mapFileRowEntry))
                        {
                            throw new Exception("Attempted to assign different history for symbol.");
                        }
                    }
                    else
                    {
                        entries.Add(mapFileRowEntry.MapFileRow.Date, mapFileRowEntry);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MapFileResolver"/> class by reading all map files
        /// for the specified market into memory
        /// </summary>
        /// <param name="dataDirectory">The root data directory</param>
        /// <param name="market">The equity market to produce a map file collection for</param>
        /// <returns>The collection of map files capable of mapping equity symbols within the specified market</returns>
        public static MapFileResolver Create(string dataDirectory, string market)
        {
            var path = Path.Combine(dataDirectory, "equity", market.ToLower(), "map_files");

            var files = from file in Directory.EnumerateFiles(path)
                        where file.EndsWith(".csv")
                        let entitySymbol = Path.GetFileNameWithoutExtension(file)
                        let fileRead = SafeMapFileRowRead(file) ?? new List<MapFileRow>()
                        let mapFileByPath = new KeyValuePair<string, MapFile>(file, new MapFile(entitySymbol, fileRead))
                        where mapFileByPath.Value != null
                        select mapFileByPath;

            return new MapFileResolver(files);
        }

        /// <summary>
        /// Resolves the map file path containing the mapping information for the symbol defined at <paramref name="date"/>
        /// </summary>
        /// <param name="symbol">The symbol as of <paramref name="date"/> to be mapped</param>
        /// <param name="date">The date associated with the <paramref name="symbol"/></param>
        /// <returns>The map file path responsible for mapping the symbol, if no map file is found, null is returned</returns>
        public string ResolveMapFilePath(string symbol, DateTime date)
        {
            // lookup the symbol's history
            SortedList<DateTime, MapFileRowEntry> entries;
            if (!_bySymbol.TryGetValue(symbol.ToUpper(), out entries))
            {
                // secondary search for exact mapping, find path than ends with symbol.csv
                var entry = _mapFilesByPath.FirstOrDefault(x => x.Key.EndsWith(symbol + ".csv", StringComparison.InvariantCultureIgnoreCase));
                // if found, return path, if not found return null
                return entry.Key;
            }

            // figure out what map file maps the specified symbol on the from date
            string mapFile = null;
            foreach (var kvp in entries)
            {
                mapFile = kvp.Value.MapFile;
                if (kvp.Key >= date)
                {
                    break;
                }
            }

            return mapFile;
        }

        /// <summary>
        /// Resolves the map file path containing the mapping information for the symbol defined at <paramref name="date"/>
        /// </summary>
        /// <param name="symbol">The symbol as of <paramref name="date"/> to be mapped</param>
        /// <param name="date">The date associated with the <paramref name="symbol"/></param>
        /// <returns>The map file responsible for mapping the symbol, if no map file is found, null is returned</returns>
        public MapFile ResolveMapFile(string symbol, DateTime date)
        {
            var path = ResolveMapFilePath(symbol, date);

            MapFile mapFile;
            if (path == null || !_mapFilesByPath.TryGetValue(path, out mapFile))
            {
                // return empty default instances if unable to resolve
                return new MapFile(symbol, new List<MapFileRow>());
            }
            return mapFile;
        }

        /// <summary>
        /// Reads in the map file at the specified path, returning null if any exceptions are encountered
        /// </summary>
        private static List<MapFileRow> SafeMapFileRowRead(string file)
        {
            try
            {
                return MapFileRow.Read(file).ToList();
            }
            catch (Exception err)
            {
                Log.Error("MapFileResover.Create(): " + file + " \tError: " + err.Message);
                return null;
            }
        }

        /// <summary>
        /// Combines the map file row with the map file path that produced the row
        /// </summary>
        class MapFileRowEntry : IEquatable<MapFileRowEntry>
        {
            /// <summary>
            /// Gets the map file row
            /// </summary>
            public MapFileRow MapFileRow { get; private set; }

            /// <summary>
            /// Gets the full path to the map file that produced this row
            /// </summary>
            public string MapFile { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MapFileRowEntry"/> class
            /// </summary>
            /// <param name="mapFile">The map file that produced this row</param>
            /// <param name="mapFileRow">The map file row data</param>
            public MapFileRowEntry(string mapFile, MapFileRow mapFileRow)
            {
                MapFileRow = mapFileRow;
                MapFile = Path.GetFullPath(mapFile);
            }

            /// <summary>
            /// Indicates whether the current object is equal to another object of the same type.
            /// </summary>
            /// <returns>
            /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
            /// </returns>
            /// <param name="other">An object to compare with this object.</param>
            public bool Equals(MapFileRowEntry other)
            {
                if (other == null) return false;
                return other.MapFileRow.Date == MapFileRow.Date
                    && other.MapFileRow.MappedSymbol == MapFileRow.MappedSymbol;
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public override string ToString()
            {
                return MapFileRow.Date + ": " + MapFileRow.MappedSymbol + ": " + MapFile;
            }
        }
    }
}