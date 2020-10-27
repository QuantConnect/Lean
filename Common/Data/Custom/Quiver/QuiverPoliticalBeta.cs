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
    public class QuiverPoliticalBeta : BaseData
    {

        /// <summary>
        /// The date of the political beta calculation
        /// </summary>
        [ProtoMember(10)]
        [JsonProperty(PropertyName = "Date")]
        private string dateJson { get; set; }
        [JsonIgnore]
        public DateTime Date {
            get {
                Console.WriteLine(dateJson);
                return DateTime.ParseExact(dateJson, "yyyy-MM-dd", null);
                    }
            set {
                dateJson = value.ToStringInvariant("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// The ticker of the company
        /// </summary>
        [ProtoMember(11)]
        [JsonProperty(PropertyName = "Ticker")]
        public string Ticker { get; set; }

        /// <summary>
        /// Correlation between daily excess returns and daily changes in Trump election odds
        /// </summary>
        [ProtoMember(12)]
        [JsonProperty(PropertyName = "TrumpBeta")]
        public decimal? TrumpBeta { get; set; }

        /// <summary>
        /// Trump's odds of winning the 2020 Presidential election, based on PredictIt betting markets
        /// </summary>
        [ProtoMember(14)]
        [JsonProperty(PropertyName = "TrumpOdds")]
        public decimal? TrumpOdds { get; set; }






        /// <summary>
        /// Required for successful Json.NET deserialization
        /// </summary>
        public QuiverPoliticalBeta()
        {
        }

        /// <summary>
        /// Creates a new instance of QuiverPoliticalBeta from a CSV line
        /// </summary>
        /// <param name="csvLine">CSV line</param>
        public QuiverPoliticalBeta(string csvLine)
        {
            // Date[0], Ticker[1], Followers[2], Pct_Change_Week[3], Pct_Change_Month[4]
            var csv = csvLine.Split(',');
            Date = Parse.DateTimeExact(csv[0], "M/d/yyyy h:mm:ss tt");
            Ticker = csv[1];
            TrumpBeta = csv[2].IfNotNullOrEmpty<decimal?>(s => Parse.Decimal(s));
            TrumpOdds = csv[3].IfNotNullOrEmpty<decimal?>(s => Parse.Decimal(s));
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
            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "quiver",
                "politicalbeta",
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
            
            return new QuiverPoliticalBeta(line)
            {
                Symbol = config.Symbol
            };
        }

        /// <summary>
        /// Formats a string with the Quiver Political Beta information.
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Ticker}({Date}) :: ") +
                   Invariant($"Trump Beta: {TrumpBeta} ") +
                   Invariant($"Trump Election Odds: {TrumpOdds}");
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