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
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides json conversion for the <see cref="MarketHoursDatabase"/> class
    /// </summary>
    public class MarketHoursDatabaseJsonConverter : TypeChangeJsonConverter<MarketHoursDatabase, MarketHoursDatabaseJsonConverter.MarketHoursDatabaseJson>
    {
        /// <summary>
        /// Convert the input value to a value to be serialzied
        /// </summary>
        /// <param name="value">The input value to be converted before serialziation</param>
        /// <returns>A new instance of TResult that is to be serialzied</returns>
        protected override MarketHoursDatabaseJson Convert(MarketHoursDatabase value)
        {
            return new MarketHoursDatabaseJson(value);
        }

        /// <summary>
        /// Converts the input value to be deserialized
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to T</param>
        /// <returns>The converted value</returns>
        protected override MarketHoursDatabase Convert(MarketHoursDatabaseJson value)
        {
            return value.Convert();
        }

        /// <summary>
        /// Creates an instance of the un-projected type to be deserialized
        /// </summary>
        /// <param name="type">The input object type, this is the data held in the token</param>
        /// <param name="token">The input data to be converted into a T</param>
        /// <returns>A new instance of T that is to be serialized using default rules</returns>
        protected override MarketHoursDatabase Create(Type type, JToken token)
        {
            var jobject = (JObject) token;
            var instance = jobject.ToObject<MarketHoursDatabaseJson>();
            return Convert(instance);
        }

        /// <summary>
        /// Defines the json structure of the market-hours-database.json file
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class MarketHoursDatabaseJson
        {
            /// <summary>
            /// The entries in the market hours database, keyed by <see cref="SecurityDatabaseKey"/>
            /// </summary>
            [JsonProperty("entries")]
            public Dictionary<string, MarketHoursDatabaseEntryJson> Entries;

            /// <summary>
            /// Initializes a new instance of the <see cref="MarketHoursDatabaseJson"/> class
            /// </summary>
            /// <param name="database">The database instance to copy</param>
            public MarketHoursDatabaseJson(MarketHoursDatabase database)
            {
                if (database == null) return;
                Entries = new Dictionary<string, MarketHoursDatabaseEntryJson>();
                foreach (var kvp in database.ExchangeHoursListing)
                {
                    var key = kvp.Key;
                    var entry = kvp.Value;
                    Entries[key.ToString()] = new MarketHoursDatabaseEntryJson(entry);
                }
            }

            /// <summary>
            /// Converts this json representation to the <see cref="MarketHoursDatabase"/> type
            /// </summary>
            /// <returns>A new instance of the <see cref="MarketHoursDatabase"/> class</returns>
            public MarketHoursDatabase Convert()
            {
                // first we parse the entries keys so that later we can sort by security type
                var entries = new Dictionary<SecurityDatabaseKey, MarketHoursDatabaseEntryJson>(Entries.Count);
                foreach (var entry in Entries)
                {
                    try
                    {
                        var key = SecurityDatabaseKey.Parse(entry.Key);
                        if (key != null)
                        {
                            entries[key] = entry.Value;
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                    }
                }

                var result = new Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry>(Entries.Count);
                // we sort so we process generic entries and non options first
                foreach (var entry in entries.OrderBy(kvp => kvp.Key.Symbol != null ? 1 : 0).ThenBy(kvp => kvp.Key.SecurityType.IsOption() ? 1 : 0))
                {
                    try
                    {
                        result.TryGetValue(entry.Key.CreateCommonKey(), out var marketEntry);
                        var underlyingEntry = GetUnderlyingEntry(entry.Key, result);
                        result[entry.Key] = entry.Value.Convert(underlyingEntry, marketEntry);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                    }
                }
                return new MarketHoursDatabase(result);
            }

            /// <summary>
            /// Helper method to get the already processed underlying entry for options
            /// </summary>
            private static MarketHoursDatabase.Entry GetUnderlyingEntry(SecurityDatabaseKey key, Dictionary<SecurityDatabaseKey, MarketHoursDatabase.Entry> result)
            {
                MarketHoursDatabase.Entry underlyingEntry = null;
                if (key.SecurityType.IsOption())
                {
                    // if option, let's get the underlyings entry
                    var underlyingSecurityType = Symbol.GetUnderlyingFromOptionType(key.SecurityType);
                    var underlying = OptionSymbol.MapToUnderlying(key.Symbol, key.SecurityType);
                    var underlyingKey = new SecurityDatabaseKey(key.Market, underlying, underlyingSecurityType);

                    if (!result.TryGetValue(underlyingKey, out underlyingEntry)
                        // let's retry with the wildcard
                        && underlying != SecurityDatabaseKey.Wildcard)
                    {
                        var underlyingKeyWildCard = new SecurityDatabaseKey(key.Market, SecurityDatabaseKey.Wildcard, underlyingSecurityType);
                        result.TryGetValue(underlyingKeyWildCard, out underlyingEntry);
                    }
                }
                return underlyingEntry;
            }
        }

        /// <summary>
        /// Defines the json structure of a single entry in the market-hours-database.json file
        /// </summary>
        [JsonObject(MemberSerialization.OptIn)]
        public class MarketHoursDatabaseEntryJson
        {
            /// <summary>
            /// The data's raw time zone
            /// </summary>
            [JsonProperty("dataTimeZone")]
            public string DataTimeZone;

            /// <summary>
            /// The exchange's time zone id from the tzdb
            /// </summary>
            [JsonProperty("exchangeTimeZone")]
            public string ExchangeTimeZone;

            /// <summary>
            /// Sunday market hours segments
            /// </summary>
            [JsonProperty("sunday")]
            public List<MarketHoursSegment> Sunday;

            /// <summary>
            /// Monday market hours segments
            /// </summary>
            [JsonProperty("monday")]
            public List<MarketHoursSegment> Monday;

            /// <summary>
            /// Tuesday market hours segments
            /// </summary>
            [JsonProperty("tuesday")]
            public List<MarketHoursSegment> Tuesday;

            /// <summary>
            /// Wednesday market hours segments
            /// </summary>
            [JsonProperty("wednesday")]
            public List<MarketHoursSegment> Wednesday;

            /// <summary>
            /// Thursday market hours segments
            /// </summary>
            [JsonProperty("thursday")]
            public List<MarketHoursSegment> Thursday;

            /// <summary>
            /// Friday market hours segments
            /// </summary>
            [JsonProperty("friday")]
            public List<MarketHoursSegment> Friday;

            /// <summary>
            /// Saturday market hours segments
            /// </summary>
            [JsonProperty("saturday")]
            public List<MarketHoursSegment> Saturday;

            /// <summary>
            /// Holiday date strings
            /// </summary>
            [JsonProperty("holidays")]
            public List<string> Holidays = new();

            /// <summary>
            /// Early closes by date
            /// </summary>
            [JsonProperty("earlyCloses")]
            public Dictionary<string, TimeSpan> EarlyCloses = new Dictionary<string, TimeSpan>();

            /// <summary>
            /// Late opens by date
            /// </summary>
            [JsonProperty("lateOpens")]
            public Dictionary<string, TimeSpan> LateOpens = new Dictionary<string, TimeSpan>();

            /// <summary>
            /// Initializes a new instance of the <see cref="MarketHoursDatabaseEntryJson"/> class
            /// </summary>
            /// <param name="entry">The entry instance to copy</param>
            public MarketHoursDatabaseEntryJson(MarketHoursDatabase.Entry entry)
            {
                if (entry == null) return;
                DataTimeZone = entry.DataTimeZone.Id;
                var hours = entry.ExchangeHours;
                ExchangeTimeZone = hours.TimeZone.Id;
                SetSegmentsForDay(hours, DayOfWeek.Sunday, out Sunday);
                SetSegmentsForDay(hours, DayOfWeek.Monday, out Monday);
                SetSegmentsForDay(hours, DayOfWeek.Tuesday, out Tuesday);
                SetSegmentsForDay(hours, DayOfWeek.Wednesday, out Wednesday);
                SetSegmentsForDay(hours, DayOfWeek.Thursday, out Thursday);
                SetSegmentsForDay(hours, DayOfWeek.Friday, out Friday);
                SetSegmentsForDay(hours, DayOfWeek.Saturday, out Saturday);
                Holidays = hours.Holidays.Select(x => x.ToString("M/d/yyyy", CultureInfo.InvariantCulture)).ToList();
                EarlyCloses = entry.ExchangeHours.EarlyCloses.ToDictionary(pair => pair.Key.ToString("M/d/yyyy", CultureInfo.InvariantCulture), pair => pair.Value);
                LateOpens = entry.ExchangeHours.LateOpens.ToDictionary(pair => pair.Key.ToString("M/d/yyyy", CultureInfo.InvariantCulture), pair => pair.Value);
            }

            /// <summary>
            /// Converts this json representation to the <see cref="MarketHoursDatabase.Entry"/> type
            /// </summary>
            /// <returns>A new instance of the <see cref="MarketHoursDatabase.Entry"/> class</returns>
            public MarketHoursDatabase.Entry Convert(MarketHoursDatabase.Entry underlyingEntry, MarketHoursDatabase.Entry marketEntry)
            {
                var hours = new Dictionary<DayOfWeek, LocalMarketHours>
                {
                    { DayOfWeek.Sunday, new LocalMarketHours(DayOfWeek.Sunday, Sunday) },
                    { DayOfWeek.Monday, new LocalMarketHours(DayOfWeek.Monday, Monday) },
                    { DayOfWeek.Tuesday, new LocalMarketHours(DayOfWeek.Tuesday, Tuesday) },
                    { DayOfWeek.Wednesday, new LocalMarketHours(DayOfWeek.Wednesday, Wednesday) },
                    { DayOfWeek.Thursday, new LocalMarketHours(DayOfWeek.Thursday, Thursday) },
                    { DayOfWeek.Friday, new LocalMarketHours(DayOfWeek.Friday, Friday) },
                    { DayOfWeek.Saturday, new LocalMarketHours(DayOfWeek.Saturday, Saturday) }
                };
                var holidayDates = Holidays.Select(x => DateTime.ParseExact(x, "M/d/yyyy", CultureInfo.InvariantCulture)).ToHashSet();
                IReadOnlyDictionary<DateTime, TimeSpan> earlyCloses = EarlyCloses.ToDictionary(x => DateTime.ParseExact(x.Key, "M/d/yyyy", CultureInfo.InvariantCulture), x => x.Value);
                IReadOnlyDictionary<DateTime, TimeSpan> lateOpens = LateOpens.ToDictionary(x => DateTime.ParseExact(x.Key, "M/d/yyyy", CultureInfo.InvariantCulture), x => x.Value);

                if(underlyingEntry != null)
                {
                    // If we have no entries but the underlying does, let's use the underlyings
                    if (holidayDates.Count == 0)
                    {
                        holidayDates = underlyingEntry.ExchangeHours.Holidays;
                    }
                    if (earlyCloses.Count == 0)
                    {
                        earlyCloses = underlyingEntry.ExchangeHours.EarlyCloses;
                    }
                    if (lateOpens.Count == 0)
                    {
                        lateOpens = underlyingEntry.ExchangeHours.LateOpens;
                    }
                }

                if(marketEntry != null)
                {
                    if (marketEntry.ExchangeHours.Holidays.Count > 0)
                    {
                        holidayDates.UnionWith(marketEntry.ExchangeHours.Holidays);
                    }

                    if (marketEntry.ExchangeHours.EarlyCloses.Count > 0 )
                    {
                        earlyCloses = MergeLateOpensAndEarlyCloses(marketEntry.ExchangeHours.EarlyCloses, earlyCloses);
                    }

                    if (marketEntry.ExchangeHours.LateOpens.Count > 0)
                    {
                        lateOpens = MergeLateOpensAndEarlyCloses(marketEntry.ExchangeHours.LateOpens, lateOpens);
                    }
                }

                var exchangeHours = new SecurityExchangeHours(DateTimeZoneProviders.Tzdb[ExchangeTimeZone], holidayDates, hours, earlyCloses, lateOpens);
                return new MarketHoursDatabase.Entry(DateTimeZoneProviders.Tzdb[DataTimeZone], exchangeHours);
            }

            private void SetSegmentsForDay(SecurityExchangeHours hours, DayOfWeek day, out List<MarketHoursSegment> segments)
            {
                LocalMarketHours local;
                if (hours.MarketHours.TryGetValue(day, out local))
                {
                    segments = local.Segments.ToList();
                }
                else
                {
                    segments = new List<MarketHoursSegment>();
                }
            }

            /// <summary>
            /// Merges the late opens or early closes from the common entry (with wildcards) with the specific entry
            /// (e.g. Indices-usa-[*] with Indices-usa-VIX).
            /// The specific entry takes precedence.
            /// </summary>
            private static Dictionary<DateTime, TimeSpan> MergeLateOpensAndEarlyCloses(IReadOnlyDictionary<DateTime, TimeSpan> common,
                IReadOnlyDictionary<DateTime, TimeSpan> specific)
            {
                var result = common.ToDictionary();
                foreach (var (key, value) in specific)
                {
                    result[key] = value;
                }

                return result;
            }
        }
    }
}
