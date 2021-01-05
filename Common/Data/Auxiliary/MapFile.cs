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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire map file for a specified symbol
    /// </summary>
    public class MapFile : IEnumerable<MapFileRow>
    {
        private readonly SortedDictionary<DateTime, MapFileRow> _data;

        /// <summary>
        /// Gets the entity's unique symbol, i.e OIH.1
        /// </summary>
        public string Permtick { get; }

        /// <summary>
        /// Gets the last date in the map file which is indicative of a delisting event
        /// </summary>
        public DateTime DelistingDate { get; }

        /// <summary>
        /// Gets the first date in this map file
        /// </summary>
        public DateTime FirstDate { get; }

        /// <summary>
        /// Gets the first ticker for the security represented by this map file
        /// </summary>
        public string FirstTicker { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFile"/> class.
        /// </summary>
        public MapFile(string permtick, IEnumerable<MapFileRow> data)
        {
            Permtick = permtick.LazyToUpper();
            _data = new SortedDictionary<DateTime, MapFileRow>(data.Distinct().ToDictionary(x => x.Date));

            // for performance we set first and last date on ctr
            if (_data.Keys.Count == 0)
            {
                FirstDate = Time.BeginningOfTime;
                DelistingDate = Time.EndOfTime;
            }
            else
            {
                FirstDate = _data.Keys.First();
                DelistingDate = _data.Keys.Last();
            }

            var firstTicker = GetMappedSymbol(FirstDate, Permtick);
            if (char.IsDigit(firstTicker.Last()))
            {
                var dotIndex = firstTicker.LastIndexOf(".", StringComparison.Ordinal);
                if (dotIndex > 0)
                {
                    int value;
                    var number = firstTicker.Substring(dotIndex + 1);
                    if (int.TryParse(number, out value))
                    {
                        firstTicker = firstTicker.Substring(0, dotIndex);
                    }
                }
            }

            FirstTicker = firstTicker;
        }

        /// <summary>
        /// Memory overload search method for finding the mapped symbol for this date.
        /// </summary>
        /// <param name="searchDate">date for symbol we need to find.</param>
        /// <param name="defaultReturnValue">Default return value if search was got no result.</param>
        /// <returns>Symbol on this date.</returns>
        public string GetMappedSymbol(DateTime searchDate, string defaultReturnValue = "")
        {
            var mappedSymbol = defaultReturnValue;
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

            if (date < FirstDate || date > DelistingDate)
            {
                // don't even bother checking the disk if the map files state we don't have the data
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reads and writes each <see cref="MapFileRow"/>
        /// </summary>
        /// <returns>Enumerable of csv lines</returns>
        public IEnumerable<string> ToCsvLines()
        {
            foreach (var mapRow in _data.Values)
            {
                yield return mapRow.ToCsv();
            }
        }

        /// <summary>
        /// Reads in an entire map file for the requested symbol from the DataFolder
        /// </summary>
        public static MapFile Read(string permtick, string market)
        {
            return new MapFile(permtick, MapFileRow.Read(permtick, market));
        }

        /// <summary>
        /// Writes the map file to a CSV file
        /// </summary>
        /// <param name="market">The market to save the MapFile to</param>
        public void WriteToCsv(string market)
        {
            var filePath = GetMapFilePath(Permtick, market);
            var fileDir = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
                Log.Trace($"Created directory for map file: {fileDir}");
            }

            File.WriteAllLines(filePath, ToCsvLines());
        }

        /// <summary>
        /// Constructs the map file path for the specified market and symbol
        /// </summary>
        /// <param name="permtick">The symbol as on disk, OIH or OIH.1</param>
        /// <param name="market">The market this symbol belongs to</param>
        /// <returns>The file path to the requested map file</returns>
        public static string GetMapFilePath(string permtick, string market)
        {
            return Path.Combine(Globals.CacheDataFolder, "equity", market, "map_files", permtick.ToLowerInvariant() + ".csv");
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<MapFileRow> GetEnumerator()
        {
            return _data.Values.GetEnumerator();
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

        /// <summary>
        /// Reads all the map files in the specified directory
        /// </summary>
        /// <param name="mapFileDirectory">The map file directory path</param>
        /// <returns>An enumerable of all map files</returns>
        public static IEnumerable<MapFile> GetMapFiles(string mapFileDirectory)
        {
            var mapFiles = new ConcurrentBag<MapFile>();
            Parallel.ForEach(Directory.EnumerateFiles(mapFileDirectory), file =>
            {
                if (file.EndsWith(".csv"))
                {
                    var permtick = Path.GetFileNameWithoutExtension(file);
                    var fileRead = SafeMapFileRowRead(file);
                    mapFiles.Add(new MapFile(permtick, fileRead));
                }
            });
            return mapFiles;
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
                Log.Error(err, $"File: {file}");
                return new List<MapFileRow>();
            }
        }
    }
}