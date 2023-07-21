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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire map file for a specified symbol
    /// </summary>
    public class MapFile : IEnumerable<MapFileRow>
    {
        private readonly List<MapFileRow> _data;

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
        /// Allows the consumer to specify a desired mapping mode
        /// </summary>
        public DataMappingMode? DataMappingMode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFile"/> class.
        /// </summary>
        public MapFile(string permtick, IEnumerable<MapFileRow> data)
        {
            if (string.IsNullOrEmpty(permtick))
            {
                throw new ArgumentNullException(nameof(permtick), "Provided ticker is null or empty");
            }

            Permtick = permtick.LazyToUpper();
            _data = data.Distinct().OrderBy(row => row.Date).ToList();

            // for performance we set first and last date on ctr
            if (_data.Count == 0)
            {
                FirstDate = Time.BeginningOfTime;
                DelistingDate = Time.EndOfTime;
            }
            else
            {
                FirstDate = _data[0].Date;
                DelistingDate = _data[_data.Count - 1].Date;
            }

            var firstTicker = GetMappedSymbol(FirstDate, Permtick);
            if (char.IsDigit(firstTicker.Last()))
            {
                var dotIndex = firstTicker.LastIndexOf(".", StringComparison.Ordinal);
                if (dotIndex > 0)
                {
                    int value;
                    var number = firstTicker.AsSpan(dotIndex + 1);
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
            for (var i = 0; i < _data.Count; i++)
            {
                var row = _data[i];
                if (row.Date < searchDate || row.DataMappingMode.HasValue && row.DataMappingMode != DataMappingMode)
                {
                    continue;
                }
                mappedSymbol = row.MappedSymbol;
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
            return _data.Select(mapRow => mapRow.ToCsv());
        }

        /// <summary>
        /// Writes the map file to a CSV file
        /// </summary>
        /// <param name="market">The market to save the MapFile to</param>
        /// <param name="securityType">The map file security type</param>
        public void WriteToCsv(string market, SecurityType securityType)
        {
            var filePath = Path.Combine(Globals.DataFolder, GetRelativeMapFilePath(market, securityType), Permtick.ToLowerInvariant() + ".csv");
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
        /// <param name="market">The market this symbol belongs to</param>
        /// <param name="securityType">The map file security type</param>
        /// <returns>The file path to the requested map file</returns>
        public static string GetRelativeMapFilePath(string market, SecurityType securityType)
        {
            return Invariant($"{securityType.SecurityTypeToLower()}/{market}/map_files");
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
            return _data.GetEnumerator();
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
        /// <param name="market">The map file market</param>
        /// <param name="securityType">The map file security type</param>
        /// <param name="dataProvider">The data provider instance to use</param>
        /// <returns>An enumerable of all map files</returns>
        public static IEnumerable<MapFile> GetMapFiles(string mapFileDirectory, string market, SecurityType securityType, IDataProvider dataProvider)
        {
            var mapFiles = new List<MapFile>();
            Parallel.ForEach(Directory.EnumerateFiles(mapFileDirectory), file =>
            {
                if (file.EndsWith(".csv"))
                {
                    var permtick = Path.GetFileNameWithoutExtension(file);
                    var fileRead = SafeMapFileRowRead(file, market, securityType, dataProvider);
                    var mapFile = new MapFile(permtick, fileRead);
                    lock (mapFiles)
                    {
                        // just use a list + lock, not concurrent bag, avoid garbage it creates for features we don't need here. See https://github.com/dotnet/runtime/issues/23103
                        mapFiles.Add(mapFile);
                    }
                }
            });
            return mapFiles;
        }

        /// <summary>
        /// Reads in the map file at the specified path, returning null if any exceptions are encountered
        /// </summary>
        private static List<MapFileRow> SafeMapFileRowRead(string file, string market, SecurityType securityType, IDataProvider dataProvider)
        {
            try
            {
                return MapFileRow.Read(file, market, securityType, dataProvider).ToList();
            }
            catch (Exception err)
            {
                Log.Error(err, $"File: {file}");
                return new List<MapFileRow>();
            }
        }
    }
}
