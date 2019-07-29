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
    /// Financial releases for the specified company
    /// </summary>
    public class EstimizeRelease : BaseData
    {
        /// <summary>
        /// The unique identifier for the release
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// The fiscal year for the release
        /// </summary>
        [JsonProperty(PropertyName = "fiscal_year")]
        public int FiscalYear { get; set; }

        /// <summary>
        /// The fiscal quarter for the release
        /// </summary>
        [JsonProperty(PropertyName = "fiscal_quarter")]
        public int FiscalQuarter { get; set; }

        /// <summary>
        /// The date of the release
        /// </summary>
        [JsonProperty(PropertyName = "release_date")]
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// The date of the release
        /// </summary>
        public override DateTime EndTime => ReleaseDate;

        /// <summary>
        /// The earnings per share for the specified fiscal quarter
        /// </summary>
        [JsonProperty(PropertyName = "eps")]
        public decimal? Eps { get; set; }

        /// <summary>
        /// The earnings per share for the specified fiscal quarter
        /// </summary>
        public override decimal Value => Eps ?? 0m;

        /// <summary>
        /// The revenue for the specified fiscal quarter
        /// </summary>
        [JsonProperty(PropertyName = "revenue")]
        public decimal? Revenue { get; set; }

        /// <summary>
        /// The estimated EPS from Wall Street
        /// </summary>
        [JsonProperty(PropertyName = "wallstreet_eps_estimate")]
        public decimal? WallStreetEpsEstimate { get; set; }

        /// <summary>
        /// The estimated revenue from Wall Street
        /// </summary>
        [JsonProperty(PropertyName = "wallstreet_revenue_estimate")]
        public decimal? WallStreetRevenueEstimate { get; set; }

        /// <summary>
        /// The mean EPS consensus by the Estimize community
        /// </summary>
        [JsonProperty(PropertyName = "consensus_eps_estimate")]
        public decimal? ConsensusEpsEstimate { get; set; }

        /// <summary>
        /// The mean revenue consensus by the Estimize community
        /// </summary>
        [JsonProperty(PropertyName = "consensus_revenue_estimate")]
        public decimal? ConsensusRevenueEstimate { get; set; }

        /// <summary>
        /// The weighted EPS consensus by the Estimize community
        /// </summary>
        [JsonProperty(PropertyName = "consensus_weighted_eps_estimate")]
        public decimal? ConsensusWeightedEpsEstimate { get; set; }

        /// <summary>
        /// The weighted revenue consensus by the Estimize community
        /// </summary>
        [JsonProperty(PropertyName = "consensus_weighted_revenue_estimate")]
        public decimal? ConsensusWeightedRevenueEstimate { get; set; }

        /// <summary>
        /// Without a default constructor, Json.NET will call the
        /// other constructor with `null` for the string parameter
        /// </summary>
        public EstimizeRelease()
        {
        }

        /// <summary>
        /// Creates EstimizeRelease instance from a line of CSV
        /// </summary>
        /// <param name="csvLine">CSV line</param>
        public EstimizeRelease(string csvLine)
        {
            // ReleaseDate[0], Id[1], FiscalYear[2], FiscalQuarter[3], Eps[4], Revenue[5], ConsensusEpsEstimate[6], ConsensusRevenueEstimate[7], WallStreetEpsEstimate[8], WallStreetRevenueEstimate[9], ConsensusWeightedEpsEstimate[10], ConsensusWeightedRevenueEstimate[11]");
            var csv = csvLine.Split(',');

            ReleaseDate = DateTime.ParseExact(csv[0].Trim(), "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            Time = ReleaseDate;
            Id = csv[1];
            FiscalYear = Convert.ToInt32(csv[2], CultureInfo.InvariantCulture);
            FiscalQuarter = Convert.ToInt32(csv[3], CultureInfo.InvariantCulture);
            Eps = string.IsNullOrWhiteSpace(csv[4]) ? (decimal?)null : Convert.ToDecimal(csv[4], CultureInfo.InvariantCulture);
            Revenue = string.IsNullOrWhiteSpace(csv[5]) ? (decimal?)null : Convert.ToDecimal(csv[5], CultureInfo.InvariantCulture);
            ConsensusEpsEstimate = string.IsNullOrWhiteSpace(csv[6]) ? (decimal?)null : Convert.ToDecimal(csv[6], CultureInfo.InvariantCulture);
            ConsensusRevenueEstimate = string.IsNullOrWhiteSpace(csv[7]) ? (decimal?)null : Convert.ToDecimal(csv[7], CultureInfo.InvariantCulture);
            WallStreetEpsEstimate = string.IsNullOrWhiteSpace(csv[8]) ? (decimal?)null : Convert.ToDecimal(csv[8], CultureInfo.InvariantCulture);
            WallStreetRevenueEstimate = string.IsNullOrWhiteSpace(csv[9]) ? (decimal?)null : Convert.ToDecimal(csv[9], CultureInfo.InvariantCulture);
            ConsensusWeightedEpsEstimate = string.IsNullOrWhiteSpace(csv[10]) ? (decimal?)null : Convert.ToDecimal(csv[10], CultureInfo.InvariantCulture);
            ConsensusWeightedRevenueEstimate = string.IsNullOrWhiteSpace(csv[11]) ? (decimal?)null : Convert.ToDecimal(csv[11], CultureInfo.InvariantCulture);
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
            if (!config.Symbol.Value.EndsWith(".R"))
            {
                throw new ArgumentException($"EstimizeRelease.GetSource(): Invalid symbol {config.Symbol}");
            }

            var symbol = config.Symbol.Value;
            symbol = symbol.Substring(0, symbol.Length - 2);

            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "estimize",
                "release",
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
        /// Estimize Release object
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new EstimizeRelease(line);
        }

        /// <summary>
        /// Formats a string with the Estimize Release information.
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol}(Q{FiscalQuarter} {FiscalYear}) :: EPS: {Eps} Revenue: {Revenue} on {EndTime:yyyyMMdd}";
        }
    }
}