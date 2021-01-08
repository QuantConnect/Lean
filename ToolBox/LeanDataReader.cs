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
using Ionic.Zip;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// This class reads data directly from disk and returns the data without the data
    /// entering the Lean data enumeration stack
    /// </summary>
    public class LeanDataReader
    {
        private readonly DateTime _date;
        private readonly string _zipPath;
        private readonly string _zipentry;
        private readonly SubscriptionDataConfig _config;
        
        /// <summary>
        /// The LeanDataReader constructor
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="symbol">The <see cref="Symbol"/> that will be read</param>
        /// <param name="resolution">The <see cref="Resolution"/> that will be read</param>
        /// <param name="date">The <see cref="DateTime"/> that will be read</param>
        /// <param name="dataFolder">The root data folder</param>
        public LeanDataReader(SubscriptionDataConfig config, Symbol symbol, Resolution resolution, DateTime date, string dataFolder)
        {
            _date = date;
            _zipPath = LeanData.GenerateZipFilePath(dataFolder, symbol, date,  resolution, config.TickType);
            _zipentry = LeanData.GenerateZipEntryName(symbol, date, resolution, config.TickType);
            _config = config;
        }

        /// <summary>
        /// Initialize a instance of LeanDataReader from a path to a zipped data file.
        /// It also supports declaring the zip entry CSV file for options and futures.  
        /// </summary>
        /// <param name="filepath">Absolute or relative path to a zipped data file, optionally the zip entry file can be declared by using '#' as separator.</param>
        /// <example>
        /// var dataReader = LeanDataReader("../relative/path/to/file.zip")
        /// var dataReader = LeanDataReader("absolute/path/to/file.zip#zipEntry.csv")
        /// </example>
        public LeanDataReader(string filepath)
        {
            Symbol symbol;
            DateTime date;
            Resolution resolution;
            string zipEntry = null;

            var isFutureOrOption = filepath.Contains("#");

            if (isFutureOrOption)
            {
                zipEntry = filepath.Split('#')[1];
                filepath = filepath.Split('#')[0];
            }
            
            var fileInfo = new FileInfo(filepath);
            if (!LeanData.TryParsePath(fileInfo.FullName, out symbol, out date, out resolution))
            {
                throw new ArgumentException($"File {filepath} cannot be parsed.");
            }

            if (isFutureOrOption)
            {
                symbol = LeanData.ReadSymbolFromZipEntry(symbol, resolution, zipEntry);
            }

            var marketHoursDataBase = MarketHoursDatabase.FromDataFolder();
            var dataTimeZone = marketHoursDataBase.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);
            var exchangeTimeZone = marketHoursDataBase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;

            var tickType = LeanData.GetCommonTickType(symbol.SecurityType);
            var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            if (fileName.Contains("_"))
            {
                tickType = (TickType)Enum.Parse(typeof(TickType), fileName.Split('_')[1], true);
            }

            var dataType = LeanData.GetDataType(resolution, tickType);
            var config = new SubscriptionDataConfig(dataType, symbol, resolution,
                                                    dataTimeZone, exchangeTimeZone, tickType: tickType,
                                                    fillForward: false, extendedHours: true, isInternalFeed: true);

            _date = date;
            _zipPath = fileInfo.FullName;
            _zipentry = zipEntry;
            _config = config;
        }

        /// <summary>
        /// Enumerate over the tick zip file and return a list of BaseData.
        /// </summary>
        /// <returns>IEnumerable of ticks</returns>
        public IEnumerable<BaseData> Parse()
        {
            var factory = (BaseData) ObjectActivator.GetActivator(_config.Type).Invoke(new object[0]);

            // for futures and options if no entry was provided we just read all
            if (_zipentry == null && (_config.SecurityType == SecurityType.Future || _config.SecurityType == SecurityType.Option || _config.SecurityType == SecurityType.FutureOption))
            {
                foreach (var entries in Compression.Unzip(_zipPath))
                {
                    // we get the contract symbol from the zip entry
                    var symbol = LeanData.ReadSymbolFromZipEntry(_config.Symbol, _config.Resolution, entries.Key);
                    foreach (var line in entries.Value)
                    {
                        var dataPoint = factory.Reader(_config, line, _date, false);
                        dataPoint.Symbol = symbol;
                        yield return dataPoint;
                    }
                }
            }
            else
            {
                ZipFile zipFile;
                using (var unzipped = Compression.Unzip(_zipPath, _zipentry, out zipFile))
                {
                    if (unzipped == null)
                        yield break;
                    string line;
                    while ((line = unzipped.ReadLine()) != null)
                    {
                        yield return factory.Reader(_config, line, _date, false);
                    }
                }
                zipFile.Dispose();
            }
        }

        /// <summary>
        /// Returns the data time zone
        /// </summary>
        /// <returns><see cref="NodaTime.DateTimeZone"/> representing the data timezone</returns>
        public DateTimeZone GetDataTimeZone()
        {
            return _config.DataTimeZone;
        }

        /// <summary>
        /// Returns the Exchange time zone
        /// </summary>
        /// <returns><see cref="NodaTime.DateTimeZone"/> representing the exchange timezone</returns>
        public DateTimeZone GetExchangeTimeZone()
        {
            return _config.ExchangeTimeZone;
        }
    }
}
