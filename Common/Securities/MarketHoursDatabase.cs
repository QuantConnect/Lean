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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides access to exchange hours and raw data times zones in various markets
    /// </summary>
    public class MarketHoursDatabase
    {
        private static MarketHoursDatabase _dataFolderMarketHoursDatabase;
        private static readonly object DataFolderMarketHoursDatabaseLock = new object();

        private readonly IReadOnlyDictionary<Key, Entry> _entries;

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
        public List<SecurityExchangeHours> ExchangeHoursListing
        {
            get { return _entries.Values.Select(x => x.ExchangeHours).ToList(); }
        }

        private MarketHoursDatabase(IReadOnlyDictionary<Key, Entry> exchangeHours)
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
                    var directory = Path.Combine(Constants.DataFolder, "market-hours");
                    var holidays = ReadHolidaysFromDirectory(directory);
                    _dataFolderMarketHoursDatabase = FromCsvFile(Path.Combine(directory, "market-hours-database.csv"), holidays);
                }
            }
            return _dataFolderMarketHoursDatabase;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MarketHoursDatabase"/> class by reading the specified csv file
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <param name="holidaysByMarket">The holidays for each market in the file, if no holiday is present then none is used</param>
        /// <returns>A new instance of the <see cref="MarketHoursDatabase"/> class representing the data in the specified file</returns>
        public static MarketHoursDatabase FromCsvFile(string file, IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket)
        {
            var exchangeHours = new Dictionary<Key, Entry>();

            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to locate market hours file: " + file);
            }

            // skip the first header line, also skip #'s as these are comment lines
            foreach (var line in File.ReadLines(file).Where(x => !x.StartsWith("#")).Skip(1))
            {
                Key key;
                var hours = FromCsvLine(line, holidaysByMarket, out key);
                if (exchangeHours.ContainsKey(key))
                {
                    throw new Exception("Encountered duplicate key while processing file: " + file + ". Key: " + key);
                }

                exchangeHours[key] = hours;
            }

            return new MarketHoursDatabase(exchangeHours);
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
            var key = new Key(market, symbol, securityType);
            if (!_entries.TryGetValue(key, out entry))
            {
                // now check with null symbol key
                if (!_entries.TryGetValue(new Key(market, null, securityType), out entry))
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
        /// Creates a new instance of <see cref="SecurityExchangeHours"/> from the specified csv line and holiday set
        /// </summary>
        /// <param name="line">The csv line to be parsed</param>
        /// <param name="holidaysByMarket">The holidays this exchange isn't open for trading by market</param>
        /// <param name="key">The key used to uniquely identify these market hours</param>
        /// <returns>A new <see cref="SecurityExchangeHours"/> for the specified csv line and holidays</returns>
        private static Entry FromCsvLine(string line, IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket, out Key key)
        {
            var csv = line.Split(',');
            var marketHours = new List<LocalMarketHours>(7);

            // timezones can be specified using Tzdb names (America/New_York) or they can
            // be specified using offsets, UTC-5

            var dataTimeZone = ParseTimeZone(csv[0]);
            var exchangeTimeZone = ParseTimeZone(csv[1]);

            //var market = csv[2];
            //var symbol = csv[3];
            //var type = csv[4];
            var symbol = string.IsNullOrEmpty(csv[3]) ? null : csv[3];
            key = new Key(csv[2], symbol, (SecurityType)Enum.Parse(typeof(SecurityType), csv[4], true));

            int csvLength = csv.Length;
            for (int i = 1; i < 8; i++) // 7 days, so < 8
            {
                // the 4 here is because 4 times per day, ex_open,open,close,ex_close
                if (4*i + 4 > csvLength - 1)
                {
                    break;
                }
                var hours = ReadCsvHours(csv, 4*i + 1, (DayOfWeek) (i - 1));
                marketHours.Add(hours);
            }

            IEnumerable<DateTime> holidays;
            if (!holidaysByMarket.TryGetValue(key.Market, out holidays))
            {
                holidays = Enumerable.Empty<DateTime>();
            }

            var exchangeHours = new SecurityExchangeHours(exchangeTimeZone, holidays, marketHours.ToDictionary(x => x.DayOfWeek));
            return new Entry(dataTimeZone, exchangeHours);
        }

        private static DateTimeZone ParseTimeZone(string tz)
        {
            // handle UTC directly
            if (tz == "UTC") return TimeZones.Utc;
            // if it doesn't start with UTC then it's a name, like America/New_York
            if (!tz.StartsWith("UTC")) return DateTimeZoneProviders.Tzdb[tz];

            // it must be a UTC offset, parse the offset as hours
            
            // define the time zone as a constant offset time zone in the form: 'UTC-3.5' or 'UTC+10'
            var millisecondsOffset = (int) TimeSpan.FromHours(double.Parse(tz.Replace("UTC", string.Empty))).TotalMilliseconds;
            return DateTimeZone.ForOffset(Offset.FromMilliseconds(millisecondsOffset));
        }

        private static LocalMarketHours ReadCsvHours(string[] csv, int startIndex, DayOfWeek dayOfWeek)
        {
            var ex_open = csv[startIndex];
            if (ex_open == "-")
            {
                return LocalMarketHours.ClosedAllDay(dayOfWeek);
            }
            if (ex_open == "+")
            {
                return LocalMarketHours.OpenAllDay(dayOfWeek);
            }

            var open = csv[startIndex + 1];
            var close = csv[startIndex + 2];
            var ex_close = csv[startIndex + 3];

            var ex_open_time = ParseHoursToTimeSpan(ex_open);
            var open_time = ParseHoursToTimeSpan(open);
            var close_time = ParseHoursToTimeSpan(close);
            var ex_close_time = ParseHoursToTimeSpan(ex_close);

            if (ex_open_time == TimeSpan.Zero
                && open_time == TimeSpan.Zero
                && close_time == TimeSpan.Zero
                && ex_close_time == TimeSpan.Zero)
            {
                return LocalMarketHours.ClosedAllDay(dayOfWeek);
            }

            return new LocalMarketHours(dayOfWeek, ex_open_time, open_time, close_time, ex_close_time);
        }

        /// <summary>
        /// Extracts the holiday information from the specified directory. Holiday file names are expectd to be of the
        /// following form: 'holidays-{market}.csv' and should be a csv file with year,month,day
        /// </summary>
        private static IReadOnlyDictionary<string, IEnumerable<DateTime>> ReadHolidaysFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new ArgumentException("The specified directory does not exist: " + directory);
            }

            var holidays = new Dictionary<string, IEnumerable<DateTime>>();
            foreach (var file in Directory.EnumerateFiles(directory, "holidays-*.csv"))
            {
                var dates = new List<DateTime>();
                var market = Path.GetFileNameWithoutExtension(file).Replace("holidays-", string.Empty);
                foreach (var line in File.ReadLines(file).Where(x => !x.StartsWith("#")).Skip(1))
                {
                    var csv = line.Split(',');
                    dates.Add(new DateTime(int.Parse(csv[0], CultureInfo.InvariantCulture), int.Parse(csv[1], CultureInfo.InvariantCulture), int.Parse(csv[2], CultureInfo.InvariantCulture)));
                }
                holidays[market] = dates;
            }
            return holidays;
        }

        private static TimeSpan ParseHoursToTimeSpan(string ex_open)
        {
            return TimeSpan.FromHours(double.Parse(ex_open, CultureInfo.InvariantCulture));
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

        class Key : IEquatable<Key>
        {
            public readonly string Market;
            public readonly string Symbol;
            public readonly SecurityType SecurityType;

            public Key(string market, string symbol, SecurityType securityType)
            {
                Market = market;
                SecurityType = securityType;
                Symbol = symbol;
            }

            #region Equality members

            public bool Equals(Key other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Market, other.Market) && Equals(Symbol, other.Symbol) && SecurityType == other.SecurityType;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Market != null ? Market.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)SecurityType;
                    return hashCode;
                }
            }

            public static bool operator ==(Key left, Key right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Key left, Key right)
            {
                return !Equals(left, right);
            }

            #endregion

            public override string ToString()
            {
                return string.Format("{0}-{1}-{2}", Market ?? "[null]", Symbol ?? "[null]", SecurityType);
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
