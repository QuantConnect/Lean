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
using System.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using NodaTime;
using ProtoBuf;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.Quiver
{
    /// <summary>
    /// Political beta for the specified company
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class QuiverEventsBeta : BaseData
    {

        /// <summary>
        /// The date of the events beta calculation
        /// </summary>
        [ProtoMember(10)]
        [JsonProperty(PropertyName = "Date")]
        [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Event name (e.g. PresidentialElection2020)
        /// </summary>
        [ProtoMember(11)]
        [JsonProperty(PropertyName = "EventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Name for first outcome (e.g. TrumpVictory)
        /// </summary>
        [ProtoMember(12)]
        [JsonProperty(PropertyName = "FirstEventName")]
        public string FirstEventName { get; set; }

        /// <summary>
        /// Name for second outcome (e.g. BidenVictory)
        /// </summary>
        [ProtoMember(13)]
        [JsonProperty(PropertyName = "SecondEventName")]
        public string SecondEventName { get; set; }

        /// <summary>
        /// Correlation between daily excess returns and daily changes in first event odds
        /// </summary>
        [ProtoMember(14)]
        [JsonProperty(PropertyName = "FirstEventBeta")]
        public decimal FirstEventBeta { get; set; }

        /// <summary>
        /// Odds of the first event happening, based on betting markets
        /// </summary>
        [ProtoMember(15)]
        [JsonProperty(PropertyName = "FirstEventOdds")]
        public decimal FirstEventOdds { get; set; }

        /// <summary>
        /// Correlation between daily excess returns and daily changes in second event odds
        /// </summary>
        [ProtoMember(16)]
        [JsonProperty(PropertyName = "SecondEventBeta")]
        public decimal SecondEventBeta { get; set; }

        /// <summary>
        /// Odds of the second event happening, based on betting markets
        /// </summary>
        [ProtoMember(17)]
        [JsonProperty(PropertyName = "SecondEventOdds")]
        public decimal SecondEventOdds { get; set; }

        /// <summary>
        /// Required for successful Json.NET deserialization
        /// </summary>
        public QuiverEventsBeta()
        {
        }

        /// <summary>
        /// Creates a new instance of QuiverPoliticalBeta from a CSV line
        /// </summary>
        /// <param name="csvLine">CSV line</param>
        public QuiverEventsBeta(string csvLine)
        {
            // Date[0], EventName[1], FirstEventName[2], SecondEventName[3], FirstEventBeta[4], SecondEventBeta[5], FirstEventOdds[6], SecondEventOdds[7]
            var csv = csvLine.Split(',');
            Date = Parse.DateTimeExact(csv[0], "yyyyMMdd");
            EventName = csv[1];
            FirstEventName = csv[2];
            SecondEventName = csv[3];
            FirstEventBeta = csv[4].IfNotNullOrEmpty<decimal>(s => Parse.Decimal(s));
            SecondEventBeta = csv[5].IfNotNullOrEmpty<decimal>(s => Parse.Decimal(s));
            FirstEventOdds = csv[6].IfNotNullOrEmpty<decimal>(s => Parse.Decimal(s));
            SecondEventOdds = csv[7].IfNotNullOrEmpty<decimal>(s => Parse.Decimal(s));
            Time = Date;
        }

        /// <summary>
        /// Return the Subscription Data Source gained from the URL
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>Subscription Data Source.</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                throw new InvalidOperationException($"{nameof(QuiverEventsBeta)} data source is currently not supported in live trading");
            }

            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "quiver",
                "eventsbeta",
                $"{config.Symbol.Value.ToLowerInvariant()}.csv"
            );
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Csv);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="line">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        /// Quiver Political Beta object
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {

            return new QuiverEventsBeta(line)
            {
                Symbol = config.Symbol
            };
        }

        /// <summary>
        /// Formats a string with the Quiver Events Beta information.
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Symbol}({Date}) :: ") +
                   Invariant($"Event Name: {EventName} ") +
                   Invariant($"Outcome #1: {FirstEventName}") +
                   Invariant($"Outcome #2: {SecondEventName}") +
                   Invariant($"First Outcome Beta: {FirstEventBeta}") +
                   Invariant($"Second Outcome Beta: {SecondEventBeta}") +
                   Invariant($"First Outcome Odds: {FirstEventOdds}") +
                   Invariant($"Second Outcome Odds: {SecondEventOdds}");
        }

        /// <summary>
        /// Indicates if there is support for mapping
        /// </summary>
        /// <returns>True indicates mapping should be used</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Specifies the data time zone for this data type. This is useful for custom data types
        /// </summary>
        /// <returns>The <see cref="DateTimeZone"/> of this data type</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }
    }
}
