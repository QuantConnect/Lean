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

using QuantConnect.Util;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides access to specific properties for various symbols
    /// </summary>
    public class SymbolPropertiesDatabase : BaseSecurityDatabase<SymbolPropertiesDatabase, SymbolProperties>
    {
        private IReadOnlyDictionary<SecurityDatabaseKey, SecurityDatabaseKey> _keyBySecurityType;

        /// <summary>
        /// Initialize a new instance of <see cref="SymbolPropertiesDatabase"/> using the given file
        /// </summary>
        /// <param name="file">File to read from</param>
        protected SymbolPropertiesDatabase(string file)
            : base(null, FromDataFolder, (entry, newEntry) => entry.Update(newEntry))
        {
            var allEntries = new Dictionary<SecurityDatabaseKey, SymbolProperties>();
            var entriesBySecurityType = new Dictionary<SecurityDatabaseKey, SecurityDatabaseKey>();

            foreach (var keyValuePair in FromCsvFile(file))
            {
                if (allEntries.ContainsKey(keyValuePair.Key))
                {
                    throw new DuplicateNameException(Messages.SymbolPropertiesDatabase.DuplicateKeyInFile(file, keyValuePair.Key));
                }
                // we wildcard the market, so per security type and symbol we will keep the *first* instance
                // this allows us to fetch deterministically, in O(1), an entry without knowing the market, see 'TryGetMarket()'
                var key = new SecurityDatabaseKey(SecurityDatabaseKey.Wildcard, keyValuePair.Key.Symbol, keyValuePair.Key.SecurityType);
                if (!entriesBySecurityType.ContainsKey(key))
                {
                    entriesBySecurityType[key] = keyValuePair.Key;
                }
                allEntries[keyValuePair.Key] = keyValuePair.Value;
            }

            Entries = allEntries;
            _keyBySecurityType = entriesBySecurityType;
        }

        /// <summary>
        /// Tries to get the market for the provided symbol/security type
        /// </summary>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="market">The market the exchange resides in <see cref="Market"/></param>
        /// <returns>True if market was retrieved, false otherwise</returns>
        public bool TryGetMarket(string symbol, SecurityType securityType, out string market)
        {
            SecurityDatabaseKey result;
            var key = new SecurityDatabaseKey(SecurityDatabaseKey.Wildcard, symbol, securityType);
            if (_keyBySecurityType.TryGetValue(key, out result))
            {
                market = result.Market;
                return true;
            }

            market = null;
            return false;
        }

        /// <summary>
        /// Gets the symbol properties for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded (Symbol class)</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="defaultQuoteCurrency">Specifies the quote currency to be used when returning a default instance of an entry is not found in the database</param>
        /// <returns>The symbol properties matching the specified market/symbol/security-type or null if not found</returns>
        /// <remarks>For any derivative options asset that is not for equities, we default to the underlying symbol's properties if no entry is found in the database</remarks>
        public SymbolProperties GetSymbolProperties(string market, Symbol symbol, SecurityType securityType, string defaultQuoteCurrency)
        {
            SymbolProperties symbolProperties;
            var lookupTicker = MarketHoursDatabase.GetDatabaseSymbolKey(symbol);
            var key = new SecurityDatabaseKey(market, lookupTicker, securityType);

            if (!Entries.TryGetValue(key, out symbolProperties))
            {
                if (symbol != null && symbol.SecurityType == SecurityType.FutureOption)
                {
                    // Default to looking up the underlying symbol's properties and using those instead if there's
                    // no existing entry for the future option.
                    lookupTicker = MarketHoursDatabase.GetDatabaseSymbolKey(symbol.Underlying);
                    key = new SecurityDatabaseKey(market, lookupTicker, symbol.Underlying.SecurityType);

                    if (Entries.TryGetValue(key, out symbolProperties))
                    {
                        return symbolProperties;
                    }
                }

                // now check with null symbol key
                if (!Entries.TryGetValue(new SecurityDatabaseKey(market, null, securityType), out symbolProperties))
                {
                    // no properties found, return object with default property values
                    return SymbolProperties.GetDefault(defaultQuoteCurrency);
                }
            }

            return symbolProperties;
        }

        /// <summary>
        /// Gets a list of symbol properties for the specified market/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <returns>An IEnumerable of symbol properties matching the specified market/security-type</returns>
        public IEnumerable<KeyValuePair<SecurityDatabaseKey, SymbolProperties>> GetSymbolPropertiesList(string market, SecurityType securityType)
        {
            foreach (var entry in Entries)
            {
                var key = entry.Key;
                var symbolProperties = entry.Value;

                if (key.Market == market && key.SecurityType == securityType)
                {
                    yield return new KeyValuePair<SecurityDatabaseKey, SymbolProperties>(key, symbolProperties);
                }
            }
        }

        /// <summary>
        /// Gets a list of symbol properties for the specified market
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <returns>An IEnumerable of symbol properties matching the specified market</returns>
        public IEnumerable<KeyValuePair<SecurityDatabaseKey, SymbolProperties>> GetSymbolPropertiesList(string market)
        {
            foreach (var entry in Entries)
            {
                var key = entry.Key;
                var symbolProperties = entry.Value;

                if (key.Market == market)
                {
                    yield return new KeyValuePair<SecurityDatabaseKey, SymbolProperties>(key, symbolProperties);
                }
            }
        }

        /// <summary>
        /// Set SymbolProperties entry for a particular market, symbol and security type.
        /// </summary>
        /// <param name="market">Market of the entry</param>
        /// <param name="symbol">Symbol of the entry</param>
        /// <param name="securityType">Type of security for the entry</param>
        /// <param name="properties">The new symbol properties to store</param>
        /// <returns>True if successful</returns>
        public bool SetEntry(string market, string symbol, SecurityType securityType, SymbolProperties properties)
        {
            var key = new SecurityDatabaseKey(market, symbol, securityType);
            lock (DataFolderDatabaseLock)
            {
                Entries[key] = properties;
                CustomEntries.Add(key);
            }
            return true;
        }

        /// <summary>
        /// Gets the instance of the <see cref="SymbolPropertiesDatabase"/> class produced by reading in the symbol properties
        /// data found in /Data/symbol-properties/
        /// </summary>
        /// <returns>A <see cref="SymbolPropertiesDatabase"/> class that represents the data in the symbol-properties folder</returns>
        public static SymbolPropertiesDatabase FromDataFolder()
        {
            if (DataFolderDatabase == null)
            {
                lock (DataFolderDatabaseLock)
                {
                    if (DataFolderDatabase == null)
                    {
                        var path = Path.Combine(Globals.GetDataFolderPath("symbol-properties"), "symbol-properties-database.csv");
                        DataFolderDatabase = new SymbolPropertiesDatabase(path);
                    }
                }
            }
            return DataFolderDatabase;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SymbolPropertiesDatabase"/> class by reading the specified csv file
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <returns>A new instance of the <see cref="SymbolPropertiesDatabase"/> class representing the data in the specified file</returns>
        private static IEnumerable<KeyValuePair<SecurityDatabaseKey, SymbolProperties>> FromCsvFile(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(Messages.SymbolPropertiesDatabase.DatabaseFileNotFound(file));
            }

            // skip the first header line, also skip #'s as these are comment lines
            foreach (var line in File.ReadLines(file).Where(x => !x.StartsWith("#") && !string.IsNullOrWhiteSpace(x)).Skip(1))
            {
                SecurityDatabaseKey key;
                var entry = FromCsvLine(line, out key);
                if (key == null || entry == null)
                {
                    continue;
                }

                yield return new KeyValuePair<SecurityDatabaseKey, SymbolProperties>(key, entry);
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="SymbolProperties"/> from the specified csv line
        /// </summary>
        /// <param name="line">The csv line to be parsed</param>
        /// <param name="key">The key used to uniquely identify this security</param>
        /// <returns>A new <see cref="SymbolProperties"/> for the specified csv line</returns>
        protected static SymbolProperties FromCsvLine(string line, out SecurityDatabaseKey key)
        {
            var csv = line.Split(',');

            SecurityType securityType;
            if (!csv[2].TryParseSecurityType(out securityType))
            {
                key = null;
                return null;
            }

            key = new SecurityDatabaseKey(
                market: csv[0],
                symbol: csv[1],
                securityType: securityType);

            return new SymbolProperties(
                description: csv[3],
                quoteCurrency: csv[4],
                contractMultiplier: csv[5].ToDecimal(),
                minimumPriceVariation: csv[6].ToDecimalAllowExponent(),
                lotSize: csv[7].ToDecimal(),
                marketTicker: HasValidValue(csv, 8) ? csv[8] : string.Empty,
                minimumOrderSize: HasValidValue(csv, 9) ? csv[9].ToDecimal() : null,
                priceMagnifier: HasValidValue(csv, 10) ? csv[10].ToDecimal() : 1,
                strikeMultiplier: HasValidValue(csv, 11) ? csv[11].ToDecimal() : 1);
        }

        private static bool HasValidValue(string[] array, uint position)
        {
            return array.Length > position && !string.IsNullOrEmpty(array[position]);
        }

        internal override void Merge(SymbolPropertiesDatabase newDatabase, bool resetCustomEntries)
        {
            base.Merge(newDatabase, resetCustomEntries);
            _keyBySecurityType = newDatabase._keyBySecurityType.ToReadOnlyDictionary();
        }
    }
}
