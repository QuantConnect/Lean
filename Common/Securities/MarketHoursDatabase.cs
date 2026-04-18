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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Securities.Future;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides access to exchange hours and raw data times zones in various markets
    /// </summary>
    [JsonConverter(typeof(MarketHoursDatabaseJsonConverter))]
    public class MarketHoursDatabase : BaseSecurityDatabase<MarketHoursDatabase, MarketHoursDatabase.Entry>
    {
        private readonly bool _forceExchangeAlwaysOpen = Config.GetBool("force-exchange-always-open");

        private static MarketHoursDatabase _alwaysOpenMarketHoursDatabase;

        /// <summary>
        /// Gets all the exchange hours held by this provider
        /// </summary>
        public List<KeyValuePair<SecurityDatabaseKey, Entry>> ExchangeHoursListing => Entries.ToList();

        /// <summary>
        /// Gets a <see cref="MarketHoursDatabase"/> that always returns <see cref="SecurityExchangeHours.AlwaysOpen"/>
        /// </summary>
        public static MarketHoursDatabase AlwaysOpen
        {
            get
            {
                if (_alwaysOpenMarketHoursDatabase == null)
                {
                    _alwaysOpenMarketHoursDatabase = new AlwaysOpenMarketHoursDatabaseImpl();
                }

                return _alwaysOpenMarketHoursDatabase;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHoursDatabase"/> class
        /// </summary>
        private MarketHoursDatabase()
            : this(new())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketHoursDatabase"/> class
        /// </summary>
        /// <param name="exchangeHours">The full listing of exchange hours by key</param>
        public MarketHoursDatabase(Dictionary<SecurityDatabaseKey, Entry> exchangeHours)
            : base(exchangeHours, FromDataFolder, (entry, other) => entry.Update(other))
        {
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
            return GetEntry(market, GetDatabaseSymbolKey(symbol), securityType).DataTimeZone;
        }

        /// <summary>
        /// Gets the instance of the <see cref="MarketHoursDatabase"/> class produced by reading in the market hours
        /// data found in /Data/market-hours/
        /// </summary>
        /// <returns>A <see cref="MarketHoursDatabase"/> class that represents the data in the market-hours folder</returns>
        public static MarketHoursDatabase FromDataFolder()
        {
            if (DataFolderDatabase == null)
            {
                lock (DataFolderDatabaseLock)
                {
                    if (DataFolderDatabase == null)
                    {
                        var path = Path.Combine(Globals.GetDataFolderPath("market-hours"), "market-hours-database.json");
                        DataFolderDatabase = FromFile(path);
                    }
                }
            }

            return DataFolderDatabase;
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
            lock (DataFolderDatabaseLock)
            {
                Entries[key] = entry;
                CustomEntries.Add(key);
            }
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
            // Fall back on the Futures MHDB entry if the FOP lookup failed.
            // Some FOPs have the same symbol properties as their futures counterparts.
            // So, to save ourselves some space, we can fall back on the existing entries
            // so that we don't duplicate the information.
            if (!TryGetEntry(market, symbol, securityType, out entry))
            {
                var key = new SecurityDatabaseKey(market, symbol, securityType);
                Log.Error($"MarketHoursDatabase.GetExchangeHours(): {Messages.MarketHoursDatabase.ExchangeHoursNotFound(key, Entries.Keys)}");

                if (securityType == SecurityType.Future && market == Market.USA)
                {
                    var exception = Messages.MarketHoursDatabase.FutureUsaMarketTypeNoLongerSupported;
                    if (SymbolPropertiesDatabase.FromDataFolder().TryGetMarket(symbol, SecurityType.Future, out market))
                    {
                        // let's suggest a market
                        exception += " " + Messages.MarketHoursDatabase.SuggestedMarketBasedOnTicker(market);
                    }

                    throw new ArgumentException(exception);
                }
                // there was nothing that really matched exactly
                throw new ArgumentException(Messages.MarketHoursDatabase.ExchangeHoursNotFound(key));
            }

            return entry;
        }

        /// <summary>
        /// Tries to get the entry for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="entry">The entry found if any</param>
        /// <returns>True if the entry was present, else false</returns>
        public bool TryGetEntry(string market, Symbol symbol, SecurityType securityType, out Entry entry)
        {
            return TryGetEntry(market, GetDatabaseSymbolKey(symbol), securityType, out entry);
        }

        /// <summary>
        /// Tries to get the entry for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        /// <param name="entry">The entry found if any</param>
        /// <returns>True if the entry was present, else false</returns>
        public virtual bool TryGetEntry(string market, string symbol, SecurityType securityType, out Entry entry)
        {
            if (_forceExchangeAlwaysOpen)
            {
                return AlwaysOpen.TryGetEntry(market, symbol, securityType, out entry);
            }

            return TryGetEntryImpl(market, symbol, securityType, out entry);
        }

        private bool TryGetEntryImpl(string market, string symbol, SecurityType securityType, out Entry entry)
        {
            var symbolKey = new SecurityDatabaseKey(market, symbol, securityType);
            return Entries.TryGetValue(symbolKey, out entry)
                // now check with null symbol key
                || Entries.TryGetValue(symbolKey.CreateCommonKey(), out entry)
                // if FOP check for future
                || securityType == SecurityType.FutureOption && TryGetEntry(market,
                    FuturesOptionsSymbolMappings.MapFromOption(symbol), SecurityType.Future, out entry)
                // if custom data type check for type specific entry
                || (securityType == SecurityType.Base && SecurityIdentifier.TryGetCustomDataType(symbol, out var customType)
                    && Entries.TryGetValue(new SecurityDatabaseKey(market, $"TYPE.{customType}", securityType), out entry));
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
        /// Represents a single entry in the <see cref="MarketHoursDatabase"/>
        /// </summary>
        public class Entry
        {
            /// <summary>
            /// Gets the raw data time zone for this entry
            /// </summary>
            public DateTimeZone DataTimeZone { get; private set; }
            /// <summary>
            /// Gets the exchange hours for this entry
            /// </summary>
            public SecurityExchangeHours ExchangeHours { get; init; }
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

            internal void Update(Entry other)
            {
                DataTimeZone = other.DataTimeZone;
                ExchangeHours.Update(other.ExchangeHours);
            }
        }

        class AlwaysOpenMarketHoursDatabaseImpl : MarketHoursDatabase
        {
            public override bool TryGetEntry(string market, string symbol, SecurityType securityType, out Entry entry)
            {
                DateTimeZone dataTimeZone;
                DateTimeZone exchangeTimeZone;
                if (TryGetEntryImpl(market, symbol, securityType, out entry))
                {
                    dataTimeZone = entry.DataTimeZone;
                    exchangeTimeZone = entry.ExchangeHours.TimeZone;
                }
                else
                {
                    dataTimeZone = exchangeTimeZone = TimeZones.Utc;
                }

                entry = new Entry(dataTimeZone, SecurityExchangeHours.AlwaysOpen(exchangeTimeZone));
                return true;
            }

            public AlwaysOpenMarketHoursDatabaseImpl()
                : base(FromDataFolder().ExchangeHoursListing.ToDictionary())
            {
            }
        }
    }
}
