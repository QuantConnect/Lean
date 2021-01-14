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
using QuantConnect.Interfaces;

namespace QuantConnect.Data.Shortable
{
    /// <summary>
    /// Sources easy-to-borrow (ETB) data from the local disk for the given brokerage
    /// </summary>
    public class LocalDiskShortableProvider : IShortableProvider
    {
        private readonly DirectoryInfo _shortableDataDirectory;

        /// <summary>
        /// Creates an instance of the class. Establishes the directory to read from.
        /// </summary>
        /// <param name="securityType">SecurityType to read data</param>
        /// <param name="brokerage">Brokerage to read ETB data</param>
        /// <param name="market">Market to read ETB data</param>
        public LocalDiskShortableProvider(SecurityType securityType, string brokerage, string market)
        {
            var shortableDataDirectory = Path.Combine(Globals.DataFolder, securityType.SecurityTypeToLower(), market, "shortable", brokerage.ToLowerInvariant());
            _shortableDataDirectory = Directory.Exists(shortableDataDirectory) ? new DirectoryInfo(shortableDataDirectory) : null;
        }

        /// <summary>
        /// Gets a list of all shortable Symbols, including the quantity shortable as a Dictionary.
        /// </summary>
        /// <param name="localTime">The algorithm's local time</param>
        /// <returns>Symbol/quantity shortable as a Dictionary. Returns null if no entry data exists for this date or brokerage</returns>
        public Dictionary<Symbol, long> AllShortableSymbols(DateTime localTime)
        {
            var allSymbols = new Dictionary<Symbol, long>();
            if (_shortableDataDirectory == null)
            {
                return allSymbols;
            }

            FileInfo shortableListFile = null;
            // Check backwards up to one week to see if we can source a previous file.
            // If not, then we return a list of all Symbols with quantity set to zero.
            var i = 0;
            var shortableListFileExists = false;
            while (i <= 7)
            {
                shortableListFile = new FileInfo(Path.Combine(_shortableDataDirectory.FullName, "dates", $"{localTime.AddDays(-i):yyyyMMdd}.csv"));
                if (shortableListFile.Exists)
                {
                    shortableListFileExists = true;
                    break;
                }

                i++;
            }

            if (!shortableListFileExists)
            {
                // Empty case, we'll know to consider all quantities zero.
                return allSymbols;
            }

            using (var fileStream = shortableListFile.OpenRead())
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var csv = line.Split(',');
                    var ticker = csv[0];

                    var symbol = new Symbol(SecurityIdentifier.GenerateEquity(ticker, QuantConnect.Market.USA, mappingResolveDate: localTime), ticker);
                    var quantity = Parse.Long(csv[1]);

                    allSymbols[symbol] = quantity;
                }
            }

            return allSymbols;
        }

        /// <summary>
        /// Gets the quantity shortable for the Symbol at the given date.
        /// </summary>
        /// <param name="symbol">Symbol to lookup shortable quantity</param>
        /// <param name="localTime">Time of the algorithm</param>
        /// <returns>Quantity shortable. Null if the data for the brokerage/date does not exist.</returns>
        public long? ShortableQuantity(Symbol symbol, DateTime localTime)
        {
            if (_shortableDataDirectory == null)
            {
                return 0;
            }

            // Implicitly trusts that Symbol.Value has been mapped and updated to the latest ticker
            var shortableSymbolFile = new FileInfo(Path.Combine(_shortableDataDirectory.FullName, "symbols", $"{symbol.Value.ToLowerInvariant()}.csv"));
            if (!shortableSymbolFile.Exists)
            {
                // Don't allow shorting if data is missing for the provided Symbol.
                return 0;
            }

            var localDate = localTime.Date;

            using (var fileStream = shortableSymbolFile.OpenRead())
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var csv = line.Split(',');
                    var date = Parse.DateTimeExact(csv[0], "yyyyMMdd");
                    var quantity = Parse.Long(csv[1]);

                    if (localDate == date)
                    {
                        return quantity;
                    }
                }
            }

            // Any missing entry will be considered to be unshortable.
            return 0;
        }
    }
}
