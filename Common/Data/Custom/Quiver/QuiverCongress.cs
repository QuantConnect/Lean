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

using Newtonsoft.Json;
using System;
using System.IO;
using NodaTime;
using ProtoBuf;
using static QuantConnect.StringExtensions;
using QuantConnect.Util;
using Newtonsoft.Json.Converters;
using QuantConnect.Orders;

namespace QuantConnect.Data.Custom.Quiver
{
    /// <summary>
    /// Personal stock transactions by U.S. Representatives
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public class QuiverCongress : BaseData
    {
        /// <summary>
        /// The date the transaction was reported
        /// </summary>
        [ProtoMember(10)]
        [JsonProperty(PropertyName = "ReportDate")]
        [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
        public DateTime ReportDate { get; set; }

        /// <summary>
        /// The date the transaction took place
        /// </summary>
        [ProtoMember(11)]
        [JsonProperty(PropertyName = "TransactionDate")]
        [JsonConverter(typeof(DateTimeJsonConverter), "yyyy-MM-dd")]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// The Representative making the transaction
        /// </summary>
        [ProtoMember(12)]
        [JsonProperty(PropertyName = "Representative")]
        public string Representative { get; set; }

        /// <summary>
        /// The type of transaction
        /// </summary>
        [ProtoMember(13)]
        [JsonProperty(PropertyName = "Transaction")]
        [JsonConverter(typeof(TransactionDirectionJsonConverter))]
        public OrderDirection Transaction { get; set; }

        /// <summary>
        /// The amount of the transaction (in USD)
        /// </summary>
        [ProtoMember(14)]
        [JsonProperty(PropertyName = "Amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// The House of Congress that the trader belongs to
        /// </summary>
        [ProtoMember(15)]
        [JsonProperty(PropertyName = "House")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Congress House { get; set; }

        /// <summary>
        /// Required for successful Json.NET deserialization
        /// </summary>
        public QuiverCongress()
        {
        }

        /// <summary>
        /// Creates a new instance of QuiverCongress from a CSV line
        /// </summary>
        /// <param name="csvLine">CSV line</param>
        public QuiverCongress(string csvLine)
        {
            // ReportDate[0], TransactionDate[1], Representative[2], Transaction[3], Amount[4],House[5]
            var csv = csvLine.Split(',');
            ReportDate = Parse.DateTimeExact(csv[0], "yyyyMMdd");
            TransactionDate = Parse.DateTimeExact(csv[1], "yyyyMMdd");
            Representative = csv[2];
            var transaction = (TransactionDirection)Enum.Parse(typeof(TransactionDirection), csv[3], true);
            Transaction = transaction == TransactionDirection.Purchase ? OrderDirection.Buy : OrderDirection.Sell;
            Amount = csv[4].IfNotNullOrEmpty<decimal?>(s => Parse.Decimal(s));
            House = (Congress)Enum.Parse(typeof(Congress), csv[5], true);
            Time = ReportDate;
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
                throw new InvalidOperationException($"{nameof(QuiverCongress)} data source is currently not supported in live trading");
            }

            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "quiver",
                "congresstrading",
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
        /// Quiver Congress object
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new QuiverCongress(line)
            {
                Symbol = config.Symbol
            };
        }

        /// <summary>
        /// Formats a string with the Quiver Congress information.
        /// </summary>
        public override string ToString()
        {
            return Invariant($"{Symbol}({ReportDate}) :: ") +
                   Invariant($"Transaction Date: {TransactionDate} ") +
                   Invariant($"Representative: {Representative} ") +
                   Invariant($"House: {House} ") +
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
