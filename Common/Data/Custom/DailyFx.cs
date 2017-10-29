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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Custom
{
    /// <summary>
    /// Helper data type for FXCM's public macro economic sentiment API.
    /// Data source used to create: https://www.dailyfx.com/calendar
    /// </summary>
    /// <remarks>
    /// Data sourced by Thomson Reuters
    /// DailyFX provides traders with an easy to use and customizable real-time calendar that updates automatically during
    /// announcements.Keep track of significant events that traders care about.As soon as event data is released, the DailyFX
    /// calendar automatically updates to provide traders with instantaneous information that they can use to formulate their trading decisions.
    /// </remarks>
    public class DailyFx : BaseData
    {
        JsonSerializerSettings _jsonSerializerSettings;
        private string _previousContent;
        private readonly Dictionary<string, DailyFx> _previous = new Dictionary<string, DailyFx>();

        /// <summary>
        /// Title of the event.
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title;

        /// <summary>
        /// Date the event was displayed on DailyFX
        /// </summary>
        [JsonProperty(PropertyName = "displayDate")]
        public DateTimeOffset DisplayDate;

        /// <summary>
        /// Time of the day the event was displayed.
        /// </summary>
        /// <remarks>
        ///  This is dated 1970, ignore the date component.
        /// </remarks>
        [JsonProperty(PropertyName = "displayTime")]
        public DateTimeOffset DisplayTime;

        /// <summary>
        /// Date/time of the event
        /// </summary>
        public DateTimeOffset EventDateTime
        {
            get { return DisplayDate.Date.Add(DisplayTime.TimeOfDay); }
        }

        /// <summary>
        /// Importance assignment from FxDaily API.
        /// </summary>
        [JsonProperty(PropertyName = "importance")]
        public FxDailyImportance Importance;

        /// <summary>
        /// What is the perceived meaning of this announcement result?
        /// </summary>
        [JsonProperty(PropertyName = "better")]
        [JsonConverter(typeof(DailyFxMeaningEnumConverter))]
        public FxDailyMeaning Meaning;

        /// <summary>
        /// Currency for this event.
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string Currency;

        /// <summary>
        /// Realized value of the economic tracker
        /// </summary>
        [JsonProperty(PropertyName = "actual")]
        public string Actual;

        /// <summary>
        /// Forecast value of the economic tracker
        /// </summary>
        [JsonProperty(PropertyName = "forecast")]
        public string Forecast;

        /// <summary>
        /// Previous value of the economic tracker
        /// </summary>
        [JsonProperty(PropertyName = "previous")]
        public string Previous;

        /// <summary>
        /// Is this a daily event?
        /// </summary>
        [JsonProperty(PropertyName = "daily")]
        public bool DailyEvent;

        /// <summary>
        /// Description and commentary on the event.
        /// </summary>
        [JsonProperty(PropertyName = "commentary")]
        public string Commentary;

        /// <summary>
        /// Language for this event.
        /// </summary>
        [JsonProperty(PropertyName = "language")]
        public string Language;

        /// <summary>
        /// Create a new basic FxDaily object.
        /// </summary>
        public DailyFx()
        {
            _jsonSerializerSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };
        }

        /// <summary>
        /// Get the source URL for this date.
        /// </summary>
        /// <remarks>
        ///     FXCM API allows up to 3mo blocks at a time, so we'll return the same URL for each
        ///     quarter and store the results in a local cache for speed.
        /// </remarks>
        /// <param name="config">Susbcription configuration</param>
        /// <param name="date">Date we're seeking.</param>
        /// <param name="isLiveMode">Live mode flag</param>
        /// <returns>Subscription source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            // Live mode just always get today's results, backtesting get all the results for the quarter.
            var url = "https://content.dailyfx.com/getData?contenttype=calendarEvent&description=true&format=json_pretty";

            // If we're backtesting append the quarters.
            if (!isLiveMode)
            {
                url += GetQuarter(date);
            }

            return new SubscriptionDataSource(url, SubscriptionTransportMedium.Rest, FileFormat.Collection);
        }

        /// <summary>
        /// Create a new Daily FX Object
        /// </summary>
        /// <param name="config">Subscription data config which created this factory</param>
        /// <param name="content">Line from a <seealso cref="SubscriptionDataSource"/> result</param>
        /// <param name="date">Date of the request</param>
        /// <param name="isLiveMode">Live mode</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            if (_previousContent == content)
            {
                return null;
            }

            _previousContent = content;

            // clean old entries from memory
            var clearingDate = date.Date.AddDays(-2);
            var oldEntries = _previous.Where(kvp => kvp.Value.DisplayDate.UtcDateTime.Date < clearingDate).ToList();
            oldEntries.ForEach(oe => _previous.Remove(oe.Key));

            var dailyfxList = JsonConvert.DeserializeObject<List<DailyFx>>(content, _jsonSerializerSettings);

            var timestamp = DateTime.UtcNow;
            var updated = new List<DailyFx>();
            foreach (var dailyfx in dailyfxList)
            {
                DailyFx previous;
                var key = MakeKey(dailyfx);
                if (_previous.TryGetValue(key, out previous))
                {
                    // if the event hasn't been updated then don't emit it
                    if (!dailyfx.HasChangedSince(previous))
                    {
                        continue;
                    }
                }

                updated.Add(dailyfx);
                _previous[key] = dailyfx;

                dailyfx.Symbol = config.Symbol;

                if (isLiveMode)
                {
                    // Live mode set the time to now, this update just happened
                    dailyfx.Time = timestamp;
                }
                else
                {
                    // Custom data format without settings in market hours are assumed UTC.
                    dailyfx.Time = dailyfx.DisplayDate.Date.AddHours(dailyfx.DisplayTime.TimeOfDay.TotalHours);
                }

                // Assign a value to this event:
                // Fairly meaningless between unrelated events, but meaningful with the same event over time.
                dailyfx.Value = 0;
                try
                {
                    if (!string.IsNullOrEmpty(dailyfx.Actual))
                    {
                        dailyfx.Value = Convert.ToDecimal(RemoveSpecialCharacters(dailyfx.Actual));
                    }
                }
                catch
                {
                }
            }

            return new BaseDataCollection(timestamp, config.Symbol, updated);
        }

        /// <summary>
        /// Actual values from the API have lots of units, strip these to generate a "value" for the basedata.
        /// </summary>
        private static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        /// <summary>
        /// Get the date search string for the quarter.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetQuarter(DateTime date)
        {
            var start = date.ToString("yyyy", CultureInfo.InvariantCulture);
            var end = start;

            if (date.Month < 4)
            {
                start += "0101";
                end += "03312359";
            }
            else if (date.Month < 7)
            {
                start += "0401";
                end += "06302359";
            }
            else if (date.Month < 10)
            {
                start += "0701";
                end += "09302359";
            }
            else
            {
                start += "1001";
                end += "12312359";
            }
            return string.Format("&startdate={0}&enddate={1}", start, end);
        }

        /// <summary>
        /// Pretty format output string for the DailyFx.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"DailyFx: {EndTime} [{EventDateTime.ToString("u")} {Title} {Currency} {Importance} {Meaning} {Actual}]";
        }

        /// <summary>
        /// Determines whether or not the values of this event have changed since the previous
        /// </summary>
        public bool HasChangedSince(DailyFx previous)
        {
            return Importance != previous.Importance
                || Meaning != previous.Meaning
                || Actual != previous.Actual
                || Forecast != previous.Forecast
                || Previous != previous.Previous
                || Commentary != previous.Commentary
                || Value != previous.Value;
        }

        private static string MakeKey(DailyFx data)
        {
            return data.EventDateTime + data.Title;
        }
    }

    /// <summary>
    /// FXDaily Importance Assignment.
    /// </summary>
    public enum FxDailyImportance
    {
        /// <summary>
        /// Low importance
        /// </summary>
        [JsonProperty(PropertyName = "low")]
        Low,

        /// <summary>
        /// Medium importance
        /// </summary>
        [JsonProperty(PropertyName = "medium")]
        Medium,

        /// <summary>
        /// High importance
        /// </summary>
        [JsonProperty(PropertyName = "high")]
        High
    }

    /// <summary>
    /// What is the meaning of the event?
    /// </summary>
    public enum FxDailyMeaning
    {
        /// <summary>
        /// The impact is perceived to be neutral.
        /// </summary>
        [JsonProperty(PropertyName = "NONE")]
        None,

        /// <summary>
        /// The economic impact is perceived to be better.
        /// </summary>
        [JsonProperty(PropertyName = "TRUE")]
        Better,

        /// <summary>
        /// The economic impact is perceived to be worse.
        /// </summary>
        [JsonProperty(PropertyName = "FALSE")]
        Worse
    }

    /// <summary>
    /// Helper to parse the Daily Fx API.
    /// </summary>
    public class DailyFxMeaningEnumConverter : JsonConverter
    {
        /// <summary>
        /// Parse DailyFx API enum
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;
            FxDailyMeaning? meaning = null;

            switch (enumString)
            {
                case "TRUE":
                    meaning = FxDailyMeaning.Better;
                    break;
                case "FALSE":
                    meaning = FxDailyMeaning.Worse;
                    break;
                default:
                case "NONE":
                    meaning = FxDailyMeaning.None;
                    break;
            }
            return meaning;
        }

        /// <summary>
        /// Write DailyFxEnum objects to JSON
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("DailyFx Enum Converter is ReadOnly");
        }

        /// <summary>
        /// Indicate if we can convert this object.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}
