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
using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Data.Custom.Benzinga
{
    /// <summary>
    /// News data powered by Benzinga - https://docs.benzinga.io/benzinga/newsfeed-v2.html
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class BenzingaNews : IndexedBaseData
    {
        /// <summary>
        /// Unique ID assigned to the article by Benzinga
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Author of the article
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; }

        /// <summary>
        /// Date the article was published
        /// </summary>
        [JsonProperty("created")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date that the article was revised on
        /// </summary>
        [JsonProperty("updated")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Title of the article published
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Summary of the article's contents
        /// </summary>
        [JsonProperty("teaser")]
        public string Teaser { get; set; }

        /// <summary>
        /// Contents of the article
        /// </summary>
        [JsonProperty("body")]
        public string Contents { get; set; }

        /// <summary>
        /// Categories that relate to the article
        /// </summary>
        [JsonProperty("channels")]
        public List<string> Categories { get; set; }

        /// <summary>
        /// Symbols that this news article mentions
        /// </summary>
        [JsonProperty("stocks")]
        public List<Symbol> Symbols { get; set; }

        /// <summary>
        /// Additional tags that are not channels/categories, but are reoccuring
        /// themes including, but not limited to; analyst names, bills being talked
        /// about in Congress (Dodd-Frank), specific products (iPhone), politicians,
        /// celebrities, stock movements (i.e. 'Mid-day Losers' &amp; 'Mid-day Gainers').
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        /// <summary>
        /// Determines the actual source from an index contained within a ticker folder
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="date">Date</param>
        /// <param name="index">File to load data from</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>SubscriptionDataSource pointing to the article</returns>
        public override SubscriptionDataSource GetSourceForAnIndex(SubscriptionDataConfig config, DateTime date, string index, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "benzinga",
                    "content",
                    $"{date.ToStringInvariant(DateFormat.EightCharacter)}.zip#{index}"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Gets the source of the index file
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>SubscriptionDataSource indicating where data is located and how it's stored</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                throw new NotImplementedException("BenzingaNews currently does not support live trading mode.");
            }

            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "benzinga",
                    config.Symbol.Value.ToLowerInvariant(),
                    $"{date.ToStringInvariant(DateFormat.EightCharacter)}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Index
            );
        }

        /// <summary>
        /// Creates an instance from a line of JSON containing article information read from the `content` directory
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">Line of data</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>New instance of <see cref="BenzingaNews"/></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return JsonConvert.DeserializeObject<BenzingaNews>(line, new BenzingaNewsJsonConverter(config.Symbol));
        }

        /// <summary>
        /// Indicates whether the data source is sparse.
        /// If false, it will disable missing file logging.
        /// </summary>
        /// <returns>true</returns>
        public override bool IsSparseData()
        {
            return true;
        }

        /// <summary>
        /// Indicates whether the data source can undergo
        /// rename events/is tied to equities.
        /// </summary>
        /// <returns>true</returns>
        public override bool RequiresMapping()
        {
            return true;
        }

        /// <summary>
        /// Set the data time zone to UTC
        /// </summary>
        /// <returns>Time zone as UTC</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }

        /// <summary>
        /// Sets the default resolution to Second
        /// </summary>
        /// <returns>Resolution.Second</returns>
        public override Resolution DefaultResolution()
        {
            return Resolution.Second;
        }

        /// <summary>
        /// Gets a list of all the supported Resolutions
        /// </summary>
        /// <returns>All resolutions</returns>
        public override List<Resolution> SupportedResolutions()
        {
            return AllResolutions;
        }

        /// <summary>
        /// Creates a clone of the instance
        /// </summary>
        /// <returns>A clone of the instance</returns>
        public override BaseData Clone()
        {
            var newCategories = new List<string>(Categories);
            var newTags = new List<string>(Tags);

            return new BenzingaNews
            {
                Id = Id,
                Author = Author,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Title = Title,
                Teaser = Teaser,
                Contents = Contents,
                Categories = newCategories,
                Symbols = Symbols,
                Tags = newTags,

                Symbol = Symbol,
                EndTime = EndTime
            };
        }

        /// <summary>
        /// Converts the instance to string
        /// </summary>
        /// <returns>Article title and contents</returns>
        public override string ToString()
        {
            return $"{EndTime} {Symbol} - Article title: {Title}\nArticle contents:\n{Contents}";
        }
    }
}
