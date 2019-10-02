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
 *
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Data.Custom.Tiingo
{
    /// <summary>
    /// Helper json converter class used to convert a list of Tiingo news data
    /// into <see cref="List{TiingoNews}"/>
    /// </summary>
    public class TiingoNewsJsonConverter : JsonConverter
    {
        private static readonly DateTime Epoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly bool _liveMode;
        private readonly Symbol _symbol;

        /// <summary>
        /// Creates a new instance of the json converter
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/> instance associated with this news</param>
        /// <param name="liveMode">True if live mode, false for backtesting</param>
        public TiingoNewsJsonConverter(Symbol symbol = null,
            bool liveMode = false)
        {
            _symbol = symbol;
            _liveMode = liveMode;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("TiingoNewsJsonConverter.WriteJson(): is not implemented");
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JToken.Load(reader);

            var result = new List<TiingoNews>();
            if (data is JArray)
            {
                foreach (var token in data)
                {
                    var dataPoint = DeserializeNews(token);
                    SetSymbolAndTime(dataPoint);
                    result.Add(dataPoint);
                }
                // we need to reverse the news data since it has newest data first
                result.Reverse();
            }
            else
            {
                var dataPoint = DeserializeNews(data);
                SetSymbolAndTime(dataPoint);
                result.Add(dataPoint);
            }

            return result;
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<TiingoNews>);
        }

        /// <summary>
        /// Helper method to deserialize a single json Tiingo news
        /// </summary>
        /// <param name="token">The json token containing the Tiingo news to deserialize</param>
        /// <returns>The deserialized <see cref="TiingoNews"/> instance</returns>
        public static TiingoNews DeserializeNews(JToken token)
        {
            // just in case we add some default values for these
            var title = token["title"]?.ToString() ?? "";
            var source = token["source"]?.ToString() ?? "";
            var url = token["url"]?.ToString() ?? "";
            var tags = token["tags"]?.ToObject<List<string>>() ?? new List<string>();
            var description = token["description"]?.ToString() ?? "";

            var publishedDate = GetDateTime(token["publishedDate"]);
            var crawlDate = GetDateTime(token["crawlDate"]);
            var articleID = token["id"].ToString();
            var tickers = token["tickers"];

            var symbols = new List<Symbol>();
            foreach (var tiingoTicker in tickers)
            {
                var ticker = TiingoSymbolMapper.GetLeanTicker(tiingoTicker.ToString());

                var sid = SecurityIdentifier.GenerateEquity(
                    ticker,
                    // for now we suppose USA market
                    QuantConnect.Market.USA,
                    // we use the news date to resolve the map file to use
                    mappingResolveDate: publishedDate);
                symbols.Add(new Symbol(sid, ticker));
            }

            var dataPoint = new TiingoNews
            {
                ArticleID = articleID,
                CrawlDate = crawlDate,
                Description = description,
                PublishedDate = publishedDate,
                Time = publishedDate,
                Source = source,
                Tags = tags,
                Symbols = symbols,
                Url = url,
                Title = title
            };

            return dataPoint;
        }

        /// <summary>
        /// Depending on the source Tiingo provides
        /// times as a string or an int
        /// </summary>
        private static DateTime GetDateTime(JToken jToken)
        {
            if (jToken == null)
            {
                return default(DateTime);
            }
            else if (jToken.Type == JTokenType.Integer)
            {
                var value = jToken.Value<int>();
                return Epoch.AddSeconds(value);
            }
            return jToken.ToObject<DateTime>();
        }

        private void SetSymbolAndTime(TiingoNews dataPoint)
        {
            if (_symbol != null)
            {
                dataPoint.Symbol = _symbol;
                if (_liveMode)
                {
                    // for live use crawl date
                    dataPoint.Time = dataPoint.CrawlDate;
                }
                else
                {
                    if ((dataPoint.CrawlDate - dataPoint.PublishedDate) > Time.OneDay)
                    {
                        // old data (eg 2014 PublishedDate) can have newer crawl date (eg 2019)
                        // for these cases, for backtesting, use published time + 'TiingoNews.HistoricalCrawlOffset'
                        dataPoint.Time = dataPoint.PublishedDate.Add(TiingoNews.HistoricalCrawlOffset);
                    }
                    else
                    {
                        dataPoint.Time = dataPoint.CrawlDate;
                    }
                }
            }
        }
    }
}
