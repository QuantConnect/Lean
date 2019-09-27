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
using NUnit.Framework;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class MarketHoursDatabaseJsonConverterTests
    {
        [Test]
        public void HandlesRoundTrip()
        {
            var database = MarketHoursDatabase.FromDataFolder();
            var result = JsonConvert.SerializeObject(database, Formatting.Indented);
            var deserializedDatabase = JsonConvert.DeserializeObject<MarketHoursDatabase>(result);

            var originalListing = database.ExchangeHoursListing.ToDictionary();
            foreach (var kvp in deserializedDatabase.ExchangeHoursListing)
            {
                var original = originalListing[kvp.Key];
                Assert.AreEqual(original.DataTimeZone, kvp.Value.DataTimeZone);
                CollectionAssert.AreEqual(original.ExchangeHours.Holidays, kvp.Value.ExchangeHours.Holidays);
                foreach (var value in Enum.GetValues(typeof(DayOfWeek)))
                {
                    var day = (DayOfWeek) value;
                    var o = original.ExchangeHours.MarketHours[day];
                    var d = kvp.Value.ExchangeHours.MarketHours[day];
                    foreach (var pair in o.Segments.Zip(d.Segments, Tuple.Create))
                    {
                        Assert.AreEqual(pair.Item1.State, pair.Item2.State);
                        Assert.AreEqual(pair.Item1.Start, pair.Item2.Start);
                        Assert.AreEqual(pair.Item1.End, pair.Item2.End);
                    }
                }
            }
        }

        [Test, Ignore("This is provided to make it easier to convert your own market-hours-database.csv to the new format")]
        public void ConvertMarketHoursDatabaseCsvToJson()
        {
            var directory = Path.Combine(Globals.DataFolder, "market-hours");
            var input = Path.Combine(directory, "market-hours-database.csv");
            var output = Path.Combine(directory, Path.GetFileNameWithoutExtension(input) + ".json");
            var allHolidays = Directory.EnumerateFiles(Path.Combine(Globals.DataFolder, "market-hours"), "holidays-*.csv").Select(x =>
            {
                var dates = new HashSet<DateTime>();
                var market = Path.GetFileNameWithoutExtension(x).Replace("holidays-", string.Empty);
                foreach (var line in File.ReadAllLines(x).Skip(1).Where(l => !l.StartsWith("#")))
                {
                    var csv = line.ToCsv();
                    dates.Add(new DateTime(
                        Parse.Int(csv[0]),
                        Parse.Int(csv[1]),
                        Parse.Int(csv[2])
                    ));
                }
                return new KeyValuePair<string, IEnumerable<DateTime>>(market, dates);
            }).ToDictionary();
            var database = FromCsvFile(input, allHolidays);
            File.WriteAllText(output, JsonConvert.SerializeObject(database, Formatting.Indented));
        }

        #region These methods represent the old way of reading MarketHoursDatabase from csv and are left here to allow users to convert

        /// <summary>
        /// Creates a new instance of the <see cref="MarketHoursDatabase"/> class by reading the specified csv file
        /// </summary>
        /// <param name="file">The csv file to be read</param>
        /// <param name="holidaysByMarket">The holidays for each market in the file, if no holiday is present then none is used</param>
        /// <returns>A new instance of the <see cref="MarketHoursDatabase"/> class representing the data in the specified file</returns>
        public static MarketHoursDatabase FromCsvFile(string file, IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket)
        {
            var exchangeHours = new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>();

            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to locate market hours file: " + file);
            }

            // skip the first header line, also skip #'s as these are comment lines
            foreach (var line in File.ReadLines(file).Where(x => !x.StartsWith("#")).Skip(1))
            {
                SecurityDatabaseKey key;
                var hours = FromCsvLine(line, holidaysByMarket, out key);
                if (exchangeHours.ContainsKey(key))
                {
                    throw new Exception($"Encountered duplicate key while processing file: {file}. Key: {key}");
                }

                exchangeHours[key] = hours;
            }

            return new MarketHoursDatabase(exchangeHours);
        }

        /// <summary>
        /// Creates a new instance of <see cref="SecurityExchangeHours"/> from the specified csv line and holiday set
        /// </summary>
        /// <param name="line">The csv line to be parsed</param>
        /// <param name="holidaysByMarket">The holidays this exchange isn't open for trading by market</param>
        /// <param name="key">The key used to uniquely identify these market hours</param>
        /// <returns>A new <see cref="SecurityExchangeHours"/> for the specified csv line and holidays</returns>
        private static MarketHoursDatabase.Entry FromCsvLine(string line,
            IReadOnlyDictionary<string, IEnumerable<DateTime>> holidaysByMarket,
            out SecurityDatabaseKey key)
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
            key = new SecurityDatabaseKey(csv[2], symbol, (SecurityType)Enum.Parse(typeof(SecurityType), csv[4], true));

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

            var earlyCloses = new Dictionary<DateTime, TimeSpan>();
            var lateOpens = new Dictionary<DateTime, TimeSpan>();
            var exchangeHours = new SecurityExchangeHours(exchangeTimeZone, holidays, marketHours.ToDictionary(x => x.DayOfWeek), earlyCloses, lateOpens);
            return new MarketHoursDatabase.Entry(dataTimeZone, exchangeHours);
        }

        private static DateTimeZone ParseTimeZone(string tz)
        {
            // handle UTC directly
            if (tz == "UTC") return TimeZones.Utc;
            // if it doesn't start with UTC then it's a name, like America/New_York
            if (!tz.StartsWith("UTC")) return DateTimeZoneProviders.Tzdb[tz];

            // it must be a UTC offset, parse the offset as hours

            // define the time zone as a constant offset time zone in the form: 'UTC-3.5' or 'UTC+10'
            var millisecondsOffset = (int) TimeSpan.FromHours(
                Parse.Double(tz.Replace("UTC", string.Empty))
            ).TotalMilliseconds;

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

        private static TimeSpan ParseHoursToTimeSpan(string ex_open)
        {
            return TimeSpan.FromHours(Parse.Double(ex_open));
        }

        #endregion
    }
}
