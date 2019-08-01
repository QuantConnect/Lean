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
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Data.Custom.Estimize
{
    /// <summary>
    /// Consensus of the specified release
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class EstimizeConsensus : BaseData
    {
        /// <summary>
        /// The unique identifier for the estimate
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Consensus source (Wall Street or Estimize)
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public Source? Source { get; set; }

        /// <summary>
        /// Type of Consensus (EPS or Revenue)
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public Type? Type { get; set; }

        /// <summary>
        /// The mean of the distribution of estimates (the "consensus")
        /// </summary>
        [JsonProperty(PropertyName = "mean")]
        public decimal? Mean { get; set; }

        /// <summary>
        /// The mean of the distribution of estimates (the "consensus")
        /// </summary>
        public override decimal Value => Mean ?? 0m;

        /// <summary>
        /// The highest estimate in the distribution
        /// </summary>
        [JsonProperty(PropertyName = "high")]
        public decimal? High { get; set; }

        /// <summary>
        /// The lowest estimate in the distribution
        /// </summary>
        [JsonProperty(PropertyName = "low")]
        public decimal? Low { get; set; }

        /// <summary>
        /// The standard deviation of the distribution
        /// </summary>
        [JsonProperty(PropertyName = "standard_deviation")]
        public decimal? StandardDeviation { get; set; }

        /// <summary>
        /// The number of estimates in the distribution
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int? Count { get; set; }

        /// <summary>
        /// The timestamp of this consensus (UTC)
        /// </summary>
        [JsonProperty(PropertyName = "updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// The fiscal year for the release
        /// </summary>
        [JsonProperty(PropertyName = "fiscal_year")]
        public int? FiscalYear { get; set; }

        /// <summary>
        /// The fiscal quarter for the release
        /// </summary>
        [JsonProperty(PropertyName = "fiscal_quarter")]
        public int? FiscalQuarter { get; set; }

        /// <summary>
        /// The timestamp of this consensus (UTC)
        /// </summary>
        public override DateTime EndTime => UpdatedAt;

        /// <summary>
        /// Empty constructor required for successful Json.NET deserialization
        /// </summary>
        public EstimizeConsensus()
        {
        }

        /// <summary>
        /// Creates an instance from CSV lines
        /// </summary>
        /// <param name="csvLine">CSV file</param>
        public EstimizeConsensus(string csvLine)
        {
            // UpdatedAt[0], Id[1], Source[2], Type[3], Mean[4], High[5], Low[6], StandardDeviation[7], FiscalYear[8], FiscalQuarter[9], Count[10]
            var csv = csvLine.Split(',');

            UpdatedAt = DateTime.ParseExact(csv[0], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            Time = UpdatedAt;
            Id = csv[1];
            Source = (Source)Enum.Parse(typeof(Source), csv[2]);
            Type = string.IsNullOrWhiteSpace(csv[3]) ? (Type?)null : (Type)Enum.Parse(typeof(Type), csv[3]);
            Mean = string.IsNullOrWhiteSpace(csv[4]) ? (decimal?) null : Convert.ToDecimal(csv[4], CultureInfo.InvariantCulture);
            High = string.IsNullOrWhiteSpace(csv[5]) ? (decimal?) null : Convert.ToDecimal(csv[5], CultureInfo.InvariantCulture);
            Low = string.IsNullOrWhiteSpace(csv[6]) ? (decimal?) null : Convert.ToDecimal(csv[6], CultureInfo.InvariantCulture);
            StandardDeviation = string.IsNullOrWhiteSpace(csv[7]) ? (decimal?) null : Convert.ToDecimal(csv[7], CultureInfo.InvariantCulture);
            FiscalYear =  string.IsNullOrWhiteSpace(csv[8]) ? (int?) null : Convert.ToInt32(csv[8], CultureInfo.InvariantCulture);
            FiscalQuarter = string.IsNullOrWhiteSpace(csv[9]) ? (int?) null : Convert.ToInt32(csv[9], CultureInfo.InvariantCulture);
            Count = string.IsNullOrWhiteSpace(csv[10]) ? (int?) null : Convert.ToInt32(csv[10], CultureInfo.InvariantCulture);
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
            if (!config.Symbol.Value.EndsWith(".C"))
            {
                throw new ArgumentException($"EstimizeConsensus.GetSource(): Invalid symbol {config.Symbol}");
            }

            var symbol = config.Symbol.Value;
            symbol = symbol.Substring(0, symbol.Length - 2);

            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "estimize",
                "consensus",
                $"{symbol.ToLower()}.csv"
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
        /// Estimize consensus object
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new EstimizeConsensus(line);
        }

        /// <summary>
        /// Formats a string with the Estimize Estimate information.
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol}(Q{FiscalQuarter} {FiscalYear}) :: {Type} - Mean: {Mean} High: {High} Low: {Low} STD: {StandardDeviation} Count: {Count} on {EndTime:yyyyMMdd} by {Source}";
        }
    }

    /// <summary>
    /// Source of the Consensus
    /// </summary>
    public enum Source
    {
        /// <summary>
        /// Consensus from Wall Street
        /// </summary>
        [JsonProperty(PropertyName = "wallstreet")]
        WallStreet,

        /// <summary>
        /// Consensus from Estimize
        /// </summary>
        [JsonProperty(PropertyName = "estimize")]
        Estimize
    }

    /// <summary>
    /// Type of the consensus
    /// </summary>
    public enum Type
    {
        /// <summary>
        /// Consensus on earnings per share value
        /// </summary>
        [JsonProperty(PropertyName = "eps")] Eps,

        /// <summary>
        /// Consensus on revenue value
        /// </summary>
        [JsonProperty(PropertyName = "revenue")]
        Revenue
    }
}