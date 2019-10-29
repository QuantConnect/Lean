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
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.Data.Custom.Benzinga
{
    /// <summary>
    /// News data powered by Benzinga - https://benzinga.com/
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class BenzingaNews : IndexedBaseData
    {
        /// <summary>
        /// Title of the article published
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Contents of the article
        /// </summary>
        [JsonProperty("description")]
        public string Contents { get; set; }

        /// <summary>
        /// Date the article was published
        /// </summary>
        [JsonProperty("pubDate")]
        public DateTime PublicationDate { get; set; }

        /// <summary>
        /// Author of the article
        /// </summary>
        [JsonProperty("dc:creator")]
        public string Author { get; set; }

        /// <summary>
        /// Categories that the article belongs to
        /// </summary>
        [JsonProperty("category"), JsonConverter(typeof(SingleValueListConverter<BenzingaCategory>))]
        public List<BenzingaCategory> Category { get; set; }

        /// <summary>
        /// Unique ID assigned to the article by Benzinga
        /// </summary>
        [JsonProperty("bz:id")]
        public string Id { get; set; }

        /// <summary>
        /// Unique ID assigned to the article after a revision by Benzinga
        /// </summary>
        [JsonProperty("bz:revisionid")]
        public string RevisionId { get; set; }

        /// <summary>
        /// Symbols that this news article applies to
        /// </summary>
        /// <remarks>
        /// Initialize this outside of the JSON deserialization process since we're unable to
        /// choose how we want the tickers referenced in the articles as (mapped or unmapped)
        /// </remarks>
        [JsonProperty("articleSymbols")]
        public List<BenzingaSymbolData> Symbols { get; set; }

        /// <summary>
        /// Date that the article was revised on
        /// </summary>
        [JsonProperty("bz:revisiondate")]
        public DateTime RevisionDate { get; set; }

        /// <summary>
        /// Metadata associated with the article
        /// </summary>
        /// <remarks>Initialize separately, same as <see cref="Symbols" />
        [JsonProperty("articleMetadata")]
        public BenzingaMetadata Metadata { get; set; }

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
                    $"{date:yyyyMMdd}.zip#{index}"
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
        /// <returns></returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "benzinga",
                    config.Symbol.Value.ToLowerInvariant(),
                    $"{date:yyyyMMdd}.csv"
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
            var article = JsonConvert.DeserializeObject<BenzingaNews>(line);
            article.Symbol = config.Symbol;
            article.Time = article.PublicationDate;

            return article;
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
        /// Creates a clone of the instance
        /// </summary>
        /// <returns>A clone of the instance</returns>
        public override BaseData Clone()
        {
            var metadataClone = new BenzingaMetadata {
                FirstRun = Metadata.FirstRun,
                IsPro = Metadata.IsPro,
                Kind = Metadata.Kind
            };
            var symbolsClone = new List<BenzingaSymbolData>();
            foreach (var symbolData in Symbols)
            {
                symbolsClone.Add(new BenzingaSymbolData
                {
                    Exchange = symbolData.Exchange,
                    Sentiment = symbolData.Sentiment,
                    Symbol = symbolData.Symbol
                });
            }

            return new BenzingaNews
            {
                Title = Title,
                Contents = Contents,
                PublicationDate = PublicationDate,
                Author = Author,
                Category = Category,
                Id = Id,
                RevisionId = RevisionId,
                Symbols = symbolsClone,
                RevisionDate = RevisionDate,
                Metadata = metadataClone,

                Symbol = Symbol,
                Time = Time,
            };
        }

        /// <summary>
        /// Converts the instance to string
        /// </summary>
        /// <returns>Article title and contents</returns>
        public override string ToString()
        {
            return $"{Symbol} - Article title: {Title}\nArticle contents:\n{Contents}";
        }
    }
}
