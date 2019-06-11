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
                $"{symbol.ToLower()}.zip"
            );
            return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile, FileFormat.Collection);
        }

        /// <summary>
        /// Reader converts each line of the data source into BaseData objects.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        /// Collection of USEnergyInformation objects
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var objectList = JsonConvert.DeserializeObject<List<EstimizeRelease>>(content);

            foreach (var obj in objectList)
            {
                obj.Symbol = config.Symbol;
                obj.Time = new DateTime(obj.FiscalYear, obj.FiscalQuarter * 3 - 2, 1);
            }

            return new BaseDataCollection(date, config.Symbol, objectList.OrderBy(x => x.EndTime));
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