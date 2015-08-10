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

namespace QuantConnect.Lean.Engine.DataFeeds.Auxiliary
{
    /// <summary>
    /// Represents an entire map file for a specified symbol
    /// </summary>
    public class MapFile
    {
        private readonly SortedDictionary<DateTime, MapFileRow> _data;

        /// <summary>
        /// Gets the entity's unique symbol, i.e OIH.1
        /// </summary>
        public string EntitySymbol { get; private set; }

        /// <summary>
        /// Gets the last date in the map file which is indicative of a delisting event
        /// </summary>
        public DateTime DelistingDate
        {
            get { return _data.Keys.Count == 0 ? Time.EndOfTime : _data.Keys.Last(); }
        }

        /// <summary>
        /// Gets the first date in this map file
        /// </summary>
        public DateTime FirstDate
        {
            get { return _data.Keys.Count == 0 ? new DateTime(1000, 01, 01) : _data.Keys.First(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFile"/> class.
        /// </summary>
        public MapFile(string entitySymbol, IEnumerable<MapFileRow> data)
        {
            EntitySymbol = entitySymbol;
            _data = new SortedDictionary<DateTime, MapFileRow>(data.ToDictionary(x => x.Date));
        }

        /// <summary>
        /// Memory overload search method for finding the mapped symbol for this date.
        /// </summary>
        /// <param name="searchDate">date for symbol we need to find.</param>
        /// <returns>Symbol on this date.</returns>
        public string GetMappedSymbol(DateTime searchDate)
        {
            var mappedSymbol = "";
            //Iterate backwards to find the most recent factor:
            foreach (var splitDate in _data.Keys)
            {
                if (splitDate < searchDate) continue;
                mappedSymbol = _data[splitDate].MappedSymbol;
                break;
            }
            return mappedSymbol;
        }

        /// <summary>
        /// Determines if there's data for the requested date
        /// </summary>
        public bool HasData(DateTime date)
        {
            // handle the case where we don't have any data
            if (_data.Count == 0)
            {
                return true;
            }

            if (date < _data.Keys.First() || date > _data.Keys.Last())
            {
                // don't even bother checking the disk if the map files state we don't have ze dataz
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads in an entire map file for the requested symbol from the DataFolder
        /// </summary>
        public static MapFile Read(string symbol, string market)
        {
            return new MapFile(symbol, MapFileRow.Read(symbol, market));
        }

        /// <summary>
        /// Resolves the effective map file at the specified date. If no date is specified, then
        /// the most recent map file will be used
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="market">The market the symbol belongs to</param>
        /// <param name="date">The date used to resolve, null to use the latest</param>
        /// <returns>The map file at the requested date</returns>
        public static MapFile ResolveMapFile(string symbol, string market, DateTime? date)
        {
            if (!date.HasValue || IsExactSymbolMapping(symbol))
            {
                // don't worry about resolving the correct file if we weren't given a date or if
                // the symbol is already mapped, that is, is of the form {symbol}.{#}
                return Read(symbol, market);
            }

            // if a date was specified then we need to open up the map files and find the 'effective' one on that date
            // the effective one is the one that either overlaps the date or the first one to start after the date

            MapFile map = null;

            var i = 0;
            do
            {
                var s = symbol + (i == 0 ? string.Empty : "." + i);
                if (!File.Exists(GetMapFilePath(s, market)))
                {
                    break;
                }

                // open up the map file and check the dates
                map = Read(s, market);
                i++;
            }
            while (map.FirstDate > date);

            return map ?? new MapFile(symbol, new List<MapFileRow>());
        }

        /// <summary>
        /// Constructs the map file path for the specified market and symbol
        /// </summary>
        /// <param name="symbol">The symbol as on disk, OIH or OIH.1</param>
        /// <param name="market">The market this symbol belongs to</param>
        /// <returns>The file path to the requested map file</returns>
        public static string GetMapFilePath(string symbol, string market)
        {
            return Path.Combine(Constants.DataFolder, "equity", market, "map_files", symbol.ToLower() + ".csv");
        }

        private static bool IsExactSymbolMapping(string symbol)
        {
            // check for a '.'
            var dot = symbol.LastIndexOf('.');
            if (dot == -1)
            {
                return false;
            }

            // check for a number behind the dot
            int i;
            var postDot = symbol.Substring(dot + 1);
            return int.TryParse(postDot, out i);
        }
    }
}