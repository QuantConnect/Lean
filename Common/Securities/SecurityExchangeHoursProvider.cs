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
    /// Provides access to exchange hours in various markets
    /// </summary>
    public class SecurityExchangeHoursProvider
    {
        private static SecurityExchangeHoursProvider DataFolderSecurityExchangeHoursProvider;
        private static readonly object DataFolderSecurityExchangeHoursProviderLock = new object();

        private readonly IReadOnlyDictionary<Key, SecurityExchangeHours> _exchangeHours;

        /// <summary>
        /// Gets an instant of <see cref="SecurityExchangeHoursProvider"/> that will always return <see cref="SecurityExchangeHours.AlwaysOpen"/>
        /// for each call to <see cref="GetExchangeHours(string, Symbol, SecurityType,DateTimeZone)"/>
        /// </summary>
        public static SecurityExchangeHoursProvider AlwaysOpen
        {
            get { return new AlwaysOpenSecurityExchangeHoursProvider(); }
        }

        /// <summary>
        /// Gets all the exchange hours held by this provider
        /// </summary>
        public List<SecurityExchangeHours> ExchangeHoursListing
        {
            get { return _exchangeHours.Values.ToList(); }
        }

        private SecurityExchangeHoursProvider(IReadOnlyDictionary<Key, SecurityExchangeHours> exchangeHours)
        {
            _exchangeHours = exchangeHours.ToDictionary();
        }

        private SecurityExchangeHoursProvider()
        {
            // used for the always open implementation
        }

        /// <summary>
        /// Performs a lookup using the specified information and returns the exchange hours if found,
        /// if exchange hours are not found, an exception is thrown
        /// </summary>
        /// <param name="configuration">The subscription data config to get exchange hours for</param>
        public SecurityExchangeHours GetExchangeHours(SubscriptionDataConfig configuration)
        {
            return GetExchangeHours(configuration.Market, configuration.Symbol, configuration.SecurityType, configuration.TimeZone);
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
        public virtual SecurityExchangeHours GetExchangeHours(string market, Symbol symbol, SecurityType securityType, DateTimeZone overrideTimeZone = null)
        {
            SecurityExchangeHours hours;
            var key = new Key(market, symbol, securityType);
            if (!_exchangeHours.TryGetValue(key, out hours))
            {
                // now check with a null symbol
                key = new Key(market, null, securityType);
                if (!_exchangeHours.TryGetValue(key, out hours))
                {
                    if (securityType == SecurityType.Base)
                    {
                        if (overrideTimeZone == null)
                        {
                            overrideTimeZone = TimeZones.Utc;
                            Log.Trace("SecurityExchangeHoursProvider.GetExchangeHours(): Custom data no time zone specified, default to UTC. " + new Key(market, symbol, securityType));
                        }
                        // base securities are always open by default
                        return SecurityExchangeHours.AlwaysOpen(overrideTimeZone);
                    }

                    Log.Error("SecurityExchangeHoursProvider.GetExchangeHours(): Unable to locate exchange hours for " + key + "." +
                        "Available keys: " + string.Join(", ", _exchangeHours.Keys));

                    // there was nothing that really matched exactly... what should we do here?
                    throw new ArgumentException("Unable to locate exchange hours for " + key);
                }
                // perform time zone override if requested, we'll use the same exact local hours
                // and holidays, but we'll express them in a different time zone
                if (overrideTimeZone != null && !hours.TimeZone.Equals(overrideTimeZone))
                {
                    hours = new SecurityExchangeHours(overrideTimeZone, hours.Holidays, hours.MarketHours);
                }
            }

            return hours;
        }

        /// <summary>
        /// Gets the instance of the <see cref="SecurityExchangeHoursProvider"/> class produced by reading in the market hours
        /// data found in /Data/market-hours/
        /// </summary>
        /// <returns>A <see cref="SecurityExchangeHoursProvider"/> class that represents the data in the market-hours folder</returns>
        public static SecurityExchangeHoursProvider FromDataFolder()
        {
            lock (DataFolderSecurityExchangeHoursProviderLock)
            {
                if (DataFolderSecurityExchangeHoursProvider == null)
                {
                    var directory = Path.Combine(Constants.DataFolder, "market-hours");
                    var holidays = ReadHolidaysFromDirectory(directory);
                    DataFolderSecurityExchangeHoursProvider = FromCsvFile(Path.Combine(directory, "market-hours-database.csv"), holidays);
                }
            }
            return DataFolderSecurityExchangeHoursProvider;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SecurityExchangeHoursProvider"/> class by reading the specified csv file
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <param name="holidaysByMarket">The holidays for each market in the file, if no holiday is present then none is used</param>
        /// <returns>A new instance of the <see cref="SecurityExchangeHoursProvider"/> class representing the data in the specified file</returns>
        public static SecurityExchangeHoursProvider FromCsvFile(string file, IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket)
        {
            var exchangeHours = new Dictionary<Key, SecurityExchangeHours>();

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

            return new SecurityExchangeHoursProvider(exchangeHours);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SecurityExchangeHours"/> from the specified csv line and holiday set
        /// </summary>
        /// <param name="line">The csv line to be parsed</param>
        /// <param name="holidaysByMarket">The holidays this exchange isn't open for trading by market</param>
        /// <param name="key">The key used to uniquely identify these market hours</param>
        /// <returns>A new <see cref="SecurityExchangeHours"/> for the specified csv line and holidays</returns>
        private static SecurityExchangeHours FromCsvLine(string line, IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket, out Key key)
        {
            var csv = line.Split(',');
            var marketHours = new List<LocalMarketHours>(7);

            // timezones can be specified using Tzdb names (America/New_York) or they can
            // be specified using offsets, UTC-5

            DateTimeZone timeZone;
            if (!csv[0].StartsWith("UTC"))
            {
                timeZone = DateTimeZoneProviders.Tzdb[csv[0]];
            }
            else
            {
                // define the time zone as a constant offset time zone in the form: 'UTC-3.5' or 'UTC+10'
                var millisecondsOffset = (int)TimeSpan.FromHours(double.Parse(csv[0].Replace("UTC", string.Empty))).TotalMilliseconds;
                timeZone = DateTimeZone.ForOffset(Offset.FromMilliseconds(millisecondsOffset));
            }

            //var market = csv[1];
            //var symbol = csv[2];
            //var type = csv[3];
            var symbol = string.IsNullOrEmpty(csv[2]) ? null : new Symbol(csv[2]);
            key = new Key(csv[1], symbol, (SecurityType)Enum.Parse(typeof(SecurityType), csv[3], true));

            int csvLength = csv.Length;
            for (int i = 1; i < 8; i++)
            {
                if (4*i + 3 > csvLength - 1)
                {
                    break;
                }
                var hours = ReadCsvHours(csv, 4*i, (DayOfWeek) (i - 1));
                marketHours.Add(hours);
            }

            IEnumerable<DateTime> holidays;
            if (!holidaysByMarket.TryGetValue(key.Market, out holidays))
            {
                holidays = Enumerable.Empty<DateTime>();
            }

            return new SecurityExchangeHours(timeZone, holidays, marketHours.ToDictionary(x => x.DayOfWeek));
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

        class Key : IEquatable<Key>
        {
            public readonly string Market;
            public readonly Symbol Symbol;
            public readonly SecurityType SecurityType;

            public Key(string market, Symbol symbol, SecurityType securityType)
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
                return string.Format("{0}-{1}-{2}", Market ?? "[null]", Symbol == null ? "[null]" : Symbol.Permtick, SecurityType);
            }
        }

        class AlwaysOpenSecurityExchangeHoursProvider : SecurityExchangeHoursProvider
        {
            public AlwaysOpenSecurityExchangeHoursProvider()
            {
            }

            public override SecurityExchangeHours GetExchangeHours(string market, Symbol symbol, SecurityType securityType, DateTimeZone overrideTimeZone = null)
            {
                return SecurityExchangeHours.AlwaysOpen(overrideTimeZone ?? TimeZones.Utc);
            }
        }
    }
}
