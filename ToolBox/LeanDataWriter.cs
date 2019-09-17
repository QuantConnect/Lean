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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Data writer for saving an IEnumerable of BaseData into the LEAN data directory.
    /// </summary>
    public class LeanDataWriter
    {
        private readonly Symbol _symbol;
        private readonly string _market;
        private readonly string _dataDirectory;
        private readonly TickType _dataType;
        private readonly Resolution _resolution;
        private readonly SecurityType _securityType;

        /// <summary>
        /// Create a new lean data writer to this base data directory.
        /// </summary>
        /// <param name="symbol">Symbol string</param>
        /// <param name="dataDirectory">Base data directory</param>
        /// <param name="resolution">Resolution of the desired output data</param>
        /// <param name="dataType">Write the data to trade files</param>
        public LeanDataWriter(Resolution resolution, Symbol symbol, string dataDirectory, TickType dataType = TickType.Trade)
        {
            _securityType = symbol.ID.SecurityType;
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _symbol = symbol;
            _market = symbol.ID.Market.ToLowerInvariant();
            _dataType = dataType;
            // All fx data is quote data.
            if (_securityType == SecurityType.Forex || _securityType == SecurityType.Cfd)
            {
                _dataType = TickType.Quote;
            }

            if (_securityType != SecurityType.Equity && _securityType != SecurityType.Forex && _securityType != SecurityType.Cfd && _securityType != SecurityType.Crypto && _securityType != SecurityType.Future && _securityType != SecurityType.Option)
            {
                throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }
        }

        /// <summary>
        /// Given the constructor parameters, write out the data in LEAN format.
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        public void Write(IEnumerable<BaseData> source)
        {
            switch (_resolution)
            {
                case Resolution.Daily:
                case Resolution.Hour:
                    WriteDailyOrHour(source);
                    break;

                case Resolution.Minute:
                case Resolution.Second:
                case Resolution.Tick:
                    WriteMinuteOrSecondOrTick(source);
                    break;
            }
        }

        /// <summary>
        /// Write out the data in LEAN format (minute, second or tick resolutions)
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        /// <remarks>This function overwrites existing data files</remarks>
        private void WriteMinuteOrSecondOrTick(IEnumerable<BaseData> source)
        {
            var sb = new StringBuilder();
            var lastTime = new DateTime();


            // Loop through all the data and write to file as we go
            foreach (var data in source)
            {
                // Ensure the data is sorted
                if (data.Time < lastTime) throw new Exception("The data must be pre-sorted from oldest to newest");

                // Based on the security type and resolution, write the data to the zip file
                if (lastTime != DateTime.MinValue && data.Time.Date > lastTime.Date)
                {
                    // Write and clear the file contents
                    var outputFile = GetZipOutputFileName(_dataDirectory, lastTime);
                    WriteFile(outputFile, sb.ToString(), lastTime);
                    sb.Clear();
                }

                lastTime = data.Time;

                // Build the line and append it to the file
                sb.Append(LeanData.GenerateLine(data, _securityType, _resolution) + Environment.NewLine);
            }

            // Write the last file
            if (sb.Length > 0)
            {
                var outputFile = GetZipOutputFileName(_dataDirectory, lastTime);
                WriteFile(outputFile, sb.ToString(), lastTime);
            }
        }

        /// <summary>
        /// Write out the data in LEAN format (daily or hour resolutions)
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        /// <remarks>This function performs a merge (insert/append/overwrite) with the existing Lean zip file</remarks>
        private void WriteDailyOrHour(IEnumerable<BaseData> source)
        {
            var sb = new StringBuilder();
            var lastTime = new DateTime();

            // Determine file path
            var outputFile = GetZipOutputFileName(_dataDirectory, lastTime);

            // Load new data rows into a SortedDictionary for easy merge/update
            var newRows = new SortedDictionary<DateTime, string>(source.ToDictionary(x => x.Time, x => LeanData.GenerateLine(x, _securityType, _resolution)));
            SortedDictionary<DateTime, string> rows;

            if (File.Exists(outputFile))
            {
                // If file exists, we load existing data and perform merge
                rows = LoadHourlyOrDailyFile(outputFile);
                foreach (var kvp in newRows)
                {
                    rows[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // No existing file, just use the new data
                rows = newRows;
            }

            // Loop through the SortedDictionary and write to file contents
            foreach (var kvp in rows)
            {
                // Build the line and append it to the file
                sb.Append(kvp.Value + Environment.NewLine);
            }

            // Write the file contents
            if (sb.Length > 0)
            {
                WriteFile(outputFile, sb.ToString(), lastTime);
            }
        }

        /// <summary>
        /// Loads an existing hourly or daily Lean zip file into a SortedDictionary
        /// </summary>
        private static SortedDictionary<DateTime, string> LoadHourlyOrDailyFile(string fileName)
        {
            var rows = new SortedDictionary<DateTime, string>();

            using (var zip = ZipFile.Read(fileName))
            {
                using (var stream = new MemoryStream())
                {
                    zip[0].Extract(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var time = Parse.DateTimeExact(line.Substring(0, DateFormat.TwelveCharacter.Length), DateFormat.TwelveCharacter);
                            rows[time] = line;
                        }
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// Write this file to disk.
        /// </summary>
        /// <param name="filePath">The full path to the new file</param>
        /// <param name="data">The data to write as a string</param>
        /// <param name="date">The date the data represents</param>
        private void WriteFile(string filePath, string data, DateTime date)
        {
            var tempFilePath = filePath + ".tmp";

            data = data.TrimEnd();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Log.Trace("LeanDataWriter.Write(): Existing deleted: " + filePath);
            }

            // Create the directory if it doesnt exist
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Write out this data string to a zip file
            Compression.Zip(data, tempFilePath, LeanData.GenerateZipEntryName(_symbol, date, _resolution, _dataType));

            // Move temp file to the final destination with the appropriate name
            File.Move(tempFilePath, filePath);

            Log.Trace("LeanDataWriter.Write(): Created: " + filePath);
        }

        /// <summary>
        /// Get the output zip file
        /// </summary>
        /// <param name="baseDirectory">Base output directory for the zip file</param>
        /// <param name="time">Date/time for the data we're writing</param>
        /// <returns>The full path to the output zip file</returns>
        private string GetZipOutputFileName(string baseDirectory, DateTime time)
        {
            return LeanData.GenerateZipFilePath(baseDirectory, _symbol, time, _resolution, _dataType);
        }

    }
}
