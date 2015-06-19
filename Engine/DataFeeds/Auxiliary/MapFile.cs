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
        /// Gets the requested symbol [symbol today]
        /// </summary>
        public string RequestedSymbol { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFile"/> class.
        /// </summary>
        public MapFile(string requestedSymbol, IEnumerable<MapFileRow> data)
        {
            RequestedSymbol = requestedSymbol;
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
    }
}