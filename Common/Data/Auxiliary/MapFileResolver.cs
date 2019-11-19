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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using QuantConnect.Util;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides a means of mapping a symbol at a point in time to the map file
    /// containing that share class's mapping information
    /// </summary>
    public class MapFileResolver : IEnumerable<MapFile>
    {
        private readonly Dictionary<string, MapFile> _mapFilesByPermtick;
        private readonly Dictionary<string, SortedList<DateTime, MapFileRowEntry>> _bySymbol;

        /// <summary>
        /// Gets an empty <see cref="MapFileResolver"/>, that is an instance that contains
        /// zero mappings
        /// </summary>
        public static readonly MapFileResolver Empty = new MapFileResolver(Enumerable.Empty<MapFile>());

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFileResolver"/> by reading
        /// in all files in the specified directory.
        /// </summary>
        /// <param name="mapFiles">The data used to initialize this resolver.</param>
        public MapFileResolver(IEnumerable<MapFile> mapFiles)
        {
            _mapFilesByPermtick = new Dictionary<string, MapFile>(StringComparer.InvariantCultureIgnoreCase);
            _bySymbol = new Dictionary<string, SortedList<DateTime, MapFileRowEntry>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var mapFile in mapFiles)
            {
                // add to our by path map
                _mapFilesByPermtick.Add(mapFile.Permtick, mapFile);

                foreach (var row in mapFile)
                {
                    SortedList<DateTime, MapFileRowEntry> entries;
                    var mapFileRowEntry = new MapFileRowEntry(mapFile.Permtick, row);

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
                            throw new DuplicateNameException("Attempted to assign different history for symbol.");
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
            return Create(Path.Combine(dataDirectory, "equity", market.ToLowerInvariant(), "map_files"));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MapFileResolver"/> class by reading all map files
        /// for the specified market into memory
        /// </summary>
        /// <param name="mapFileDirectory">The directory containing the map files</param>
        /// <returns>The collection of map files capable of mapping equity symbols within the specified market</returns>
        public static MapFileResolver Create(string mapFileDirectory)
        {
            return new MapFileResolver(MapFile.GetMapFiles(mapFileDirectory));
        }

        /// <summary>
        /// Gets the map file matching the specified permtick
        /// </summary>
        /// <param name="permtick">The permtick to match on</param>
        /// <returns>The map file matching the permtick, or null if not found</returns>
        public MapFile GetByPermtick(string permtick)
        {
            MapFile mapFile;
            _mapFilesByPermtick.TryGetValue(permtick.LazyToUpper(), out mapFile);
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
            // lookup the symbol's history
            SortedList<DateTime, MapFileRowEntry> entries;
            if (_bySymbol.TryGetValue(symbol, out entries))
            {
                if (entries.Count == 0)
                {
                    return new MapFile(symbol, new List<MapFileRow>());
                }

                // Return value of BinarySearch (from MSDN):
                // The zero-based index of item in the sorted List<T>, if item is found;
                // otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item
                // or, if there is no larger element, the bitwise complement of Count.
                var indexOf = entries.Keys.BinarySearch(date);
                if (indexOf >= 0)
                {
                    symbol = entries.Values[indexOf].EntitySymbol;
                }
                else
                {
                    if (indexOf == ~entries.Keys.Count)
                    {
                        // the searched date is greater than the last date in the list, return the last entry
                        indexOf = entries.Keys.Count - 1;
                    }
                    else
                    {
                        // if negative, it's the bitwise complement of where it should be
                        indexOf = ~indexOf;
                    }

                    symbol = entries.Values[indexOf].EntitySymbol;
                }
            }
            // secondary search for exact mapping, find path than ends with symbol.csv
            MapFile mapFile;
            if (!_mapFilesByPermtick.TryGetValue(symbol, out mapFile)
                || mapFile.FirstDate > date)
            {
                return new MapFile(symbol, new List<MapFileRow>());
            }
            return mapFile;
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
            public string EntitySymbol { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MapFileRowEntry"/> class
            /// </summary>
            /// <param name="entitySymbol">The map file that produced this row</param>
            /// <param name="mapFileRow">The map file row data</param>
            public MapFileRowEntry(string entitySymbol, MapFileRow mapFileRow)
            {
                MapFileRow = mapFileRow;
                EntitySymbol = entitySymbol;
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
                return MapFileRow.Date + ": " + MapFileRow.MappedSymbol + ": " + EntitySymbol;
            }
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<MapFile> GetEnumerator()
        {
            return _mapFilesByPermtick.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}