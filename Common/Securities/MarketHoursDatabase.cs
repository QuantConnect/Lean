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
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides access to exchange hours and raw data times zones in various markets
    /// </summary>
    [JsonConverter(typeof(MarketHoursDatabaseJsonConverter))]
    public class MarketHoursDatabase
    {
        private static MarketHoursDatabase _dataFolderMarketHoursDatabase;
        private static readonly object DataFolderMarketHoursDatabaseLock = new object();

        private readonly Dictionary<SecurityDatabaseKey, Entry> _entries;

        /// <summary>
        /// Gets all the exchange hours held by this provider
        /// </summary>
        public List<KeyValuePair<SecurityDatabaseKey,Entry>> ExchangeHoursListing => _entries.ToList();

        /// <summary>
        /// Gets a <see cref="MarketHoursDatabase"/> that always returns <see cref="SecurityExchangeHours.AlwaysOpen"/>
        /// </summary>
        public static MarketHoursDatabase AlwaysOpen { get; } = new AlwaysOpenMarketHoursDatabaseImpl();

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHoursDatabase"/> class
        /// </summary>
        /// <param name="exchangeHours">The full listing of exchange hours by key</param>
        public MarketHoursDatabase(IReadOnlyDictionary<SecurityDatabaseKey, Entry> exchangeHours)
        {
            _entries = exchangeHours.ToDictionary();
        }

        /// <summary>
        /// Convenience method for retrieving exchange hours from market hours database using a subscription config
        /// </summary>
        /// <param name="configuration">The subscription data config to get exchange hours for</param>
        /// <returns>The configure exchange hours for the specified configuration</returns>
        public SecurityExchangeHours GetExchangeHours(SubscriptionDataConfig configuration)
        {
            return GetExchangeHours(configuration.Market, configuration.Symbol, configuration.SecurityType);
        }

        /// <summary>
        /// Convenience method for retrieving exchange hours from market hours database using a subscription config
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <returns>The exchange hours for the specified security</returns>
        public SecurityExchangeHours GetExchangeHours(string market, Symbol symbol, SecurityType securityType)
        {
            return GetEntry(market, symbol, securityType).ExchangeHours;
        }

        /// <summary>
        /// Performs a lookup using the specified information and returns the data's time zone if found,
        /// if an entry is not found, an exception is thrown
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <returns>The raw data time zone for the specified security</returns>
        public DateTimeZone GetDataTimeZone(string market, Symbol symbol, SecurityType securityType)
        {
            var stringSymbol = symbol == null ? string.Empty : symbol.Value;
            return GetEntry(market, stringSymbol, securityType).DataTimeZone;
        }

        /// <summary>
        /// Resets the market hours database, forcing a reload when reused.
        /// Called in tests where multiple algorithms are run sequentially,
        /// and we need to guarantee that every test starts with the same environment.
        /// </summary>
        public static void Reset()
        {
            lock (DataFolderMarketHoursDatabaseLock)
            {
                _dataFolderMarketHoursDatabase = null;
            }
        }

        /// <summary>
        /// Gets the instance of the <see cref="MarketHoursDatabase"/> class produced by reading in the market hours
        /// data found in /Data/market-hours/
        /// </summary>
        /// <returns>A <see cref="MarketHoursDatabase"/> class that represents the data in the market-hours folder</returns>
        public static MarketHoursDatabase FromDataFolder()
        {
            return FromDataFolder(Globals.DataFolder);
        }

        /// <summary>
        /// Gets the instance of the <see cref="MarketHoursDatabase"/> class produced by reading in the market hours
        /// data found in /Data/market-hours/
        /// </summary>
        /// <param name="dataFolder">Path to the data folder</param>
        /// <returns>A <see cref="MarketHoursDatabase"/> class that represents the data in the market-hours folder</returns>
        public static MarketHoursDatabase FromDataFolder(string dataFolder)
        {
            lock (DataFolderMarketHoursDatabaseLock)
            {
                if (_dataFolderMarketHoursDatabase == null)
                {
                    var path = Path.Combine(dataFolder, "market-hours", "market-hours-database.json");
                    _dataFolderMarketHoursDatabase = FromFile(path);
                }
            }
            return _dataFolderMarketHoursDatabase;
        }

        /// <summary>
        /// Reads the specified file as a market hours database instance
        /// </summary>
        /// <param name="path">The market hours database file path</param>
        /// <returns>A new instance of the <see cref="MarketHoursDatabase"/> class</returns>
        public static MarketHoursDatabase FromFile(string path)
        {
            return JsonConvert.DeserializeObject<MarketHoursDatabase>(File.ReadAllText(path));
        }

        /// <summary>
        /// Sets the entry for the specified market/symbol/security-type.
        /// This is intended to be used by custom data and other data sources that don't have explicit
        /// entries in market-hours-database.csv. At run time, the algorithm can update the market hours
        /// database via calls to AddData.
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="exchangeHours">The exchange hours for the specified symbol</param>
        /// <param name="dataTimeZone">The time zone of the symbol's raw data. Optional, defaults to the exchange time zone</param>
        /// <returns>The entry matching the specified market/symbol/security-type</returns>
        public virtual Entry SetEntry(string market, string symbol, SecurityType securityType, SecurityExchangeHours exchangeHours, DateTimeZone dataTimeZone = null)
        {
            dataTimeZone = dataTimeZone ?? exchangeHours.TimeZone;
            var key = new SecurityDatabaseKey(market, symbol, securityType);
            var entry = new Entry(dataTimeZone, exchangeHours);
            _entries[key] = entry;
            return entry;
        }

        /// <summary>
        /// Convenience method for the common custom data case.
        /// Sets the entry for the specified symbol using SecurityExchangeHours.AlwaysOpen(timeZone)
        /// This sets the data time zone equal to the exchange time zone as well.
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="timeZone">The time zone of the symbol's exchange and raw data</param>
        /// <returns>The entry matching the specified market/symbol/security-type</returns>
        public virtual Entry SetEntryAlwaysOpen(string market, string symbol, SecurityType securityType, DateTimeZone timeZone)
        {
            return SetEntry(market, symbol, securityType, SecurityExchangeHours.AlwaysOpen(timeZone));
        }

        /// <summary>
        /// Gets the entry for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <returns>The entry matching the specified market/symbol/security-type</returns>
        public virtual Entry GetEntry(string market, string symbol, SecurityType securityType)
        {
            Entry entry;
            var key = new SecurityDatabaseKey(market, symbol, securityType);
            if (!_entries.TryGetValue(key, out entry))
            {
                // now check with null symbol key
                if (!_entries.TryGetValue(new SecurityDatabaseKey(market, null, securityType), out entry))
                {
                    var keys = string.Join(", ", _entries.Keys);
                    Log.Error($"MarketHoursDatabase.GetExchangeHours(): Unable to locate exchange hours for {key}.Available keys: {keys}");

                    // there was nothing that really matched exactly... what should we do here?
                    throw new ArgumentException($"Unable to locate exchange hours for {key}");
                }
            }

            return entry;
        }

        /// <summary>
        /// Gets the entry for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded (Symbol class)</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <returns>The entry matching the specified market/symbol/security-type</returns>
        public virtual Entry GetEntry(string market, Symbol symbol, SecurityType securityType)
        {
            return GetEntry(market, GetDatabaseSymbolKey(symbol), securityType);
        }

        /// <summary>
        /// Gets the correct string symbol to use as a database key
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The symbol string used in the database ke</returns>
        public static string GetDatabaseSymbolKey(Symbol symbol)
        {
            string stringSymbol;
            if (symbol == null)
            {
                stringSymbol = string.Empty;
            }
            else
            {
                switch (symbol.ID.SecurityType)
                {
                    case SecurityType.Option:
                        stringSymbol = symbol.HasUnderlying ? symbol.Underlying.Value : string.Empty;
                        break;

                    case SecurityType.Base:
                    case SecurityType.Future:
                        stringSymbol = symbol.ID.Symbol;
                        break;

                    default:
                        stringSymbol = symbol.Value;
                        break;
                }
            }

            return stringSymbol;
        }

        /// <summary>
        /// Determines if the database contains the specified key
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>True if an entry is found, otherwise false</returns>
        protected bool ContainsKey(SecurityDatabaseKey key)
        {
            return _entries.ContainsKey(key);
        }

        /// <summary>
        /// Represents a single entry in the <see cref="MarketHoursDatabase"/>
        /// </summary>
        public class Entry
        {
            /// <summary>
            /// Gets the raw data time zone for this entry
            /// </summary>
            public readonly DateTimeZone DataTimeZone;
            /// <summary>
            /// Gets the exchange hours for this entry
            /// </summary>
            public readonly SecurityExchangeHours ExchangeHours;
            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class
            /// </summary>
            /// <param name="dataTimeZone">The raw data time zone</param>
            /// <param name="exchangeHours">The security exchange hours for this entry</param>
            public Entry(DateTimeZone dataTimeZone, SecurityExchangeHours exchangeHours)
            {
                DataTimeZone = dataTimeZone;
                ExchangeHours = exchangeHours;
            }
        }

        class AlwaysOpenMarketHoursDatabaseImpl : MarketHoursDatabase
        {
            public override Entry GetEntry(string market, string symbol, SecurityType securityType)
            {
                var key = new SecurityDatabaseKey(market, symbol, securityType);
                var tz = ContainsKey(key)
                    ? base.GetEntry(market, symbol, securityType).ExchangeHours.TimeZone
                    : DateTimeZone.Utc;

                return new Entry(tz, SecurityExchangeHours.AlwaysOpen(tz));
            }

            public AlwaysOpenMarketHoursDatabaseImpl()
                : base(FromDataFolder().ExchangeHoursListing.ToDictionary())
            {
            }
        }
    }
}
