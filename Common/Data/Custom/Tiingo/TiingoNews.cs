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
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using ProtoBuf;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.Tiingo
{
    /// <summary>
    /// Tiingo news data
    /// https://api.tiingo.com/documentation/news
    /// </summary>
    /// <remarks>Requires setting <see cref="Tiingo.AuthCode"/></remarks>
    [ProtoContract(SkipConstructor = true)]
    public class TiingoNews : IndexedBaseData
    {
        private List<string> _tags;
        private List<Symbol> _symbols;

        /// <summary>
        /// The domain the news source is from.
        /// </summary>
        [ProtoMember(10)]
        public string Source { get; set; }

        /// <summary>
        /// The datetime the news story was added to Tiingos database in UTC.
        /// This is always recorded by Tiingo and the news source has no input on this date.
        /// </summary>
        [ProtoMember(11)]
        public DateTime CrawlDate { get; set; }

        /// <summary>
        /// URL of the news article.
        /// </summary>
        [ProtoMember(12)]
        public string Url { get; set; }

        /// <summary>
        /// The datetime the news story was published in UTC. This is usually reported by the news source and not by Tiingo.
        /// If the news source does not declare a published date, Tiingo will use the time the news story was discovered by our crawler farm.
        /// </summary>
        [ProtoMember(13)]
        public DateTime PublishedDate { get; set; }

        /// <summary>
        /// Tags that are mapped and discovered by Tiingo.
        /// </summary>
        [ProtoMember(14)]
        public List<string> Tags
        {
            get
            {
                if (_tags == null)
                {
                    _tags = new List<string>();
                }
                
                return _tags;
            }
            set
            {
                _tags = value;
            }
        }

        /// <summary>
        /// Long-form description of the news story.
        /// </summary>
        [ProtoMember(15)]
        public string Description { get; set; }

        /// <summary>
        /// Title of the news article.
        /// </summary>
        [ProtoMember(16)]
        public string Title { get; set; }

        /// <summary>
        /// Unique identifier specific to the news article.
        /// </summary>
        [ProtoMember(17)]
        public string ArticleID { get; set; }

        /// <summary>
        /// What symbols are mentioned in the news story.
        /// </summary>
        [ProtoMember(18)]
        public List<Symbol> Symbols
        {
            get
            {
                if (_symbols == null)
                {
                    _symbols = new List<Symbol>();
                }
                
                return _symbols;
            } 
            set
            {
                _symbols = value;
            }
        }

        /// <summary>
        /// Returns the source for a given index value
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="index">The index value for which we want to fetch the source</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>The <see cref="SubscriptionDataSource"/> instance to use</returns>
        public override SubscriptionDataSource GetSourceForAnIndex(SubscriptionDataConfig config, DateTime date, string index, bool isLiveMode)
        {
            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "tiingo",
                "content",
                $"{date.ToStringInvariant(DateFormat.EightCharacter)}.zip#{index}"
            );
            return new SubscriptionDataSource(source,
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv);
        }

        /// <summary>
        /// For backtesting returns the index source for a date
        /// For live trading will return the source url to use, not using the index mechanism
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>The <see cref="SubscriptionDataSource"/> instance to use</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            if (isLiveMode)
            {
                if (!Tiingo.IsAuthCodeSet)
                {
                    throw new InvalidOperationException("TiingoNews API token has to be set using Tiingo.SetAuthCode(). See https://api.tiingo.com/about/pricing");
                }

                var tiingoTicker = TiingoSymbolMapper.GetTiingoTicker(config.Symbol);
                var url = Invariant($"https://api.tiingo.com/tiingo/news?tickers={tiingoTicker}&startDate={date:yyyy-MM-dd}&token={Tiingo.AuthCode}&sortBy=crawlDate");

                return new SubscriptionDataSource(url,
                    SubscriptionTransportMedium.Rest,
                    FileFormat.Collection);
            }

            var source = Path.Combine(
                Globals.DataFolder,
                "alternative",
                "tiingo",
                $"{config.MappedSymbol.ToLowerInvariant()}",
                $"{date.ToStringInvariant(DateFormat.EightCharacter)}.csv"
            );
            return new SubscriptionDataSource(source,
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Index);
        }

        /// <summary>
        ///     Reader converts each line of the data source into BaseData objects. Each data type creates its own factory method,
        ///     and returns a new instance of the object
        ///     each time it is called. The returned object is assumed to be time stamped in the config.ExchangeTimeZone.
        /// </summary>
        /// <param name="config">Subscription data config setup object</param>
        /// <param name="content">Content of the source document</param>
        /// <param name="date">Date of the requested data</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        ///     Instance of the T:BaseData object generated by this line of the CSV
        /// </returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var data = JsonConvert.DeserializeObject<List<TiingoNews>>(content, new TiingoNewsJsonConverter(config.Symbol));

            if (isLiveMode)
            {
                // use the last news time, that's the most recent, as the collection time
                var newest = data.LastOrDefault();
                return new BaseDataCollection(newest?.Time ?? date, config.Symbol, data);
            }
            // we expect a single piece of news for backtesting
            var single = data.Single();
            return single;
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
