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

        private readonly IReadOnlyDictionary<SecurityDatabaseKey, Entry> _entries;

        /// <summary>
        /// Gets an instant of <see cref="MarketHoursDatabase"/> that will always return <see cref="SecurityExchangeHours.AlwaysOpen"/>
        /// for each call to <see cref="GetExchangeHours(string, Symbol, SecurityType,DateTimeZone)"/>
        /// </summary>
        public static MarketHoursDatabase AlwaysOpen
        {
            get { return new AlwaysOpenMarketHoursDatabase(); }
        }

        /// <summary>
        /// Gets all the exchange hours held by this provider
        /// </summary>
        public List<KeyValuePair<SecurityDatabaseKey,Entry>> ExchangeHoursListing
        {
            get { return _entries.ToList(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHoursDatabase"/> class
        /// </summary>
        /// <param name="exchangeHours">The full listing of exchange hours by key</param>
        public MarketHoursDatabase(IReadOnlyDictionary<SecurityDatabaseKey, Entry> exchangeHours)
        {
            _entries = exchangeHours.ToDictionary();
        }

        private MarketHoursDatabase()
        {
            // used for the always open implementation
        }

        /// <summary>
        /// Performs a lookup using the specified information and returns the exchange hours if found,
        /// if exchange hours are not found, an exception is thrown
        /// </summary>
        /// <param name="configuration">The subscription data config to get exchange hours for</param>
        /// <param name="overrideTimeZone">Specify this time zone to override the resolved time zone from the market hours database.
        /// This value will also be used as the time zone for SecurityType.Base with no market hours database entry.
        /// If null is specified, no override will be performed. If null is specified, and it's SecurityType.Base, then Utc will be used.</param>
        public SecurityExchangeHours GetExchangeHours(SubscriptionDataConfig configuration, DateTimeZone overrideTimeZone = null)
        {
            // we don't expect base security types to be in the market-hours-database, so set overrideTimeZone
            if (configuration.SecurityType == SecurityType.Base && overrideTimeZone == null) overrideTimeZone = configuration.ExchangeTimeZone;
            return GetExchangeHours(configuration.Market, configuration.Symbol, configuration.SecurityType, overrideTimeZone);
        }

        /// <summary>
        /// Performs a lookup using the specified information and returns the exchange hours if found,
        /// if exchange hours are not found, an exception is thrown
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="overrideTimeZone">Specify this time zone to override the resolved time zone from the market hours database.
        /// This value will also be used as the time zone for SecurityType.Base with no market hours database entry.
        /// If null is specified, no override will be performed. If null is specified, and it's SecurityType.Base, then Utc will be used.</param>
        /// <returns>The exchange hours for the specified security</returns>
        public SecurityExchangeHours GetExchangeHours(string market, Symbol symbol, SecurityType securityType, DateTimeZone overrideTimeZone = null)
        {
            var stringSymbol = symbol == null ? string.Empty : symbol.Value;
            return GetEntry(market, stringSymbol, securityType, overrideTimeZone).ExchangeHours;
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
        /// Gets the instance of the <see cref="MarketHoursDatabase"/> class produced by reading in the market hours
        /// data found in /Data/market-hours/
        /// </summary>
        /// <returns>A <see cref="MarketHoursDatabase"/> class that represents the data in the market-hours folder</returns>
        public static MarketHoursDatabase FromDataFolder()
        {
            lock (DataFolderMarketHoursDatabaseLock)
            {
                if (_dataFolderMarketHoursDatabase == null)
                {
                    var path = Path.Combine(Globals.DataFolder, "market-hours", "market-hours-database.json");
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
        /// Gets the entry for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="overrideTimeZone">Specify this time zone to override the resolved time zone from the market hours database.
        /// This value will also be used as the time zone for SecurityType.Base with no market hours database entry.
        /// If null is specified, no override will be performed. If null is specified, and it's SecurityType.Base, then Utc will be used.</param>
        /// <returns>The entry matching the specified market/symbol/security-type</returns>
        public virtual Entry GetEntry(string market, string symbol, SecurityType securityType, DateTimeZone overrideTimeZone = null)
        {
            Entry entry;
            var key = new SecurityDatabaseKey(market, symbol, securityType);
            if (!_entries.TryGetValue(key, out entry))
            {
                // now check with null symbol key
                if (!_entries.TryGetValue(new SecurityDatabaseKey(market, null, securityType), out entry))
                {
                    if (securityType == SecurityType.Base)
                    {
                        if (overrideTimeZone == null)
                        {
                            overrideTimeZone = TimeZones.Utc;
                            Log.Error("MarketHoursDatabase.GetExchangeHours(): Custom data no time zone specified, default to UTC. " + key);
                        }
                        // base securities are always open by default and have equal data time zone and exchange time zones
                        return new Entry(overrideTimeZone, SecurityExchangeHours.AlwaysOpen(overrideTimeZone));
                    }

                    Log.Error(string.Format("MarketHoursDatabase.GetExchangeHours(): Unable to locate exchange hours for {0}." + "Available keys: {1}", key, string.Join(", ", _entries.Keys)));

                    // there was nothing that really matched exactly... what should we do here?
                    throw new ArgumentException("Unable to locate exchange hours for " + key);
                }

                // perform time zone override if requested, we'll use the same exact local hours
                // and holidays, but we'll express them in a different time zone
                if (overrideTimeZone != null && !entry.ExchangeHours.TimeZone.Equals(overrideTimeZone))
                {
                    return new Entry(overrideTimeZone, new SecurityExchangeHours(overrideTimeZone, entry.ExchangeHours.Holidays, entry.ExchangeHours.MarketHours));
                }
            }

            return entry;
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

        class AlwaysOpenMarketHoursDatabase : MarketHoursDatabase
        {
            public override Entry GetEntry(string market, string symbol, SecurityType securityType, DateTimeZone overrideTimeZone = null)
            {
                var tz = overrideTimeZone ?? TimeZones.Utc;
                return new Entry(tz, SecurityExchangeHours.AlwaysOpen(tz));
            }
        }
    }
}
