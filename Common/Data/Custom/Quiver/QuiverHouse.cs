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
    /// Twitter follower counts for the specified company
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class QuiverHouse : BaseData
    {

        /// <summary>
        /// The date of the follower count
        /// </summary>
        [ProtoMember(10)]
        [JsonProperty(PropertyName = "Date")]
        private string dateJson { get; set; }
        [JsonIgnore]
        public DateTime Date
        {
            get
            {
                return DateTime.ParseExact(dateJson, "yyyy-MM-dd", null);
            }
            set
            {
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
        /// The Representative making the transaction
        /// </summary>
        [ProtoMember(12)]
        [JsonProperty(PropertyName = "Representative")]
        public string Representative { get; set; }

        /// <summary>
        /// The type of transaction
        /// </summary>
        [ProtoMember(11)]
        [JsonProperty(PropertyName = "Transaction")]
        public string Transaction { get; set; }

        /// <summary>
        /// The amount of the transaction
        /// </summary>
        [ProtoMember(11)]
        [JsonProperty(PropertyName = "Amount")]
        public decimal? Amount { get; set; }





        /// <summary>
        /// Required for successful Json.NET deserialization
        /// </summary>
        public QuiverHouse()
        {
        }

        /// <summary>
        /// Creates a new instance of QuiverTwitter from a CSV line
        /// </summary>
        /// <param name="csvLine">CSV line</param>
        public QuiverHouse(string csvLine)
        {
            // Date[0], Ticker[1], Followers[2], Pct_Change_Week[3], Pct_Change_Month[4]
            var csv = csvLine.Split(',');
            Date = Parse.DateTimeExact(csv[0], "M/d/yyyy h:mm:ss tt");
            Ticker = csv[1];
            Representative = csv[2];
            Transaction = csv[3];
            Amount = csv[4].IfNotNullOrEmpty<decimal?>(s => Parse.Decimal(s));
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
                "housetrading",
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
        /// Quiver Twitter object
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new QuiverHouse(line)
            {
                Symbol = config.Symbol
            };
        }

        /// <summary>
        /// Formats a string with the Quiver Twitter information.
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Ticker}({Date}) :: ") +
                   Invariant($"Representative: {Representative} ") +
                   Invariant($"Transaction: {Transaction}") +
                   Invariant($"Amount: {Amount}");
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