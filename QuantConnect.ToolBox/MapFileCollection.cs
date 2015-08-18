using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Provides a means of mapping a symbol at a point in time to the map file
    /// containing that share class's mapping information
    /// </summary>
    public class MapFileCollection
    {
        private readonly Dictionary<string, SortedList<DateTime, MapFileRowEntry>> _bySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFileCollection"/> by reading
        /// in all files in the specified directory.
        /// </summary>
        /// <param name="mapFileFolder">The directory containing the map files</param>
        public MapFileCollection(string mapFileFolder)
        {
            _bySymbol = new Dictionary<string, SortedList<DateTime, MapFileRowEntry>>();

            // build a symbol map and a map file map
            foreach (var mapFile in Directory.EnumerateFiles(mapFileFolder))
            {
                // many of the files with hashes seems to be corrupted/incomplete, log them
                IEnumerable<MapFileRow> rows;
                try
                {
                    rows = MapFileRow.Read(mapFile).ToList();
                }
                catch (Exception err)
                {
                    Log.Error("MapFileCollection.ctor(): " + mapFile + " \tError: " + err.Message);
                    continue;
                }

                foreach (var row in rows)
                {
                    SortedList<DateTime, MapFileRowEntry> entries;
                    var mapFileRowEntry = new MapFileRowEntry(mapFile, row);

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
        /// Resolves the map file containing the mapping information for the symbol defined at <paramref name="date"/>
        /// </summary>
        /// <param name="symbol">The symbol as of <paramref name="date"/> to be mapped</param>
        /// <param name="date">The date associated with the <paramref name="symbol"/></param>
        /// <returns>The map file responsible for mapping the symbol, if no map file is found, null is returned</returns>
        public string ResolveMapFile(string symbol, DateTime date)
        {
            // lookup the symbol's history
            SortedList<DateTime, MapFileRowEntry> entries;
            if (!_bySymbol.TryGetValue(symbol, out entries))
            {
                // no mappings
                return null;
            }

            // figure out what map file maps the specified symbol on the from date
            var mapFile = string.Empty;
            foreach (var kvp in entries)
            {
                mapFile = kvp.Value.MapFile;
                if (kvp.Key > date)
                {
                    break;
                }
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
