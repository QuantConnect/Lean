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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Data.Custom.Benzinga
{
    /// <summary>
    /// Helper json converter class used to convert Benzinga news data
    /// into <see cref="BenzingaNews"/>
    ///
    /// An example schema of the data in a serialized format is provided
    /// to help you better understand this converter.
    /// </summary>
    /// <example>
    /// {
    ///     "id": int,
    ///     "author": string,
    ///     "created": DateTime (yyyy-MM-ddTHH:mm:ssZ),
    ///     "updated": DateTime (yyyy-MM-ddTHH:mm:ssZ),
    ///     "title": string,
    ///     "teaser": string,
    ///     "body": string,
    ///     "channels": [
    ///         {
    ///             "name": string
    ///         },
    ///         ...
    ///     ],
    ///     "stocks": [
    ///         {
    ///             "name": string
    ///         },
    ///         ...
    ///     ],
    ///     "tags": [
    ///         {
    ///             "name": string
    ///         },
    ///         ...
    ///     ]
    /// }
    /// </example>
    public class BenzingaNewsJsonConverter : JsonConverter
    {
        private readonly Symbol _symbol;
        private readonly bool _liveMode;

        /// <summary>
        /// Sometimes "Berkshire Hathaway" is mentioned as "BRK" in the raw data, although it is
        /// separated into class A and B shares and should appear as BRK.A and BRK.B. Because our
        /// map file system does not perform the conversion from BRK -> { BRK.A, BRK.B }, we must
        /// provide them manually. Note that we don't dynamically try to locate class A and B shares
        /// because there can exist companies with the same base ticker that class A and B shares have.
        /// For example, CBS trades under "CBS" and "CBS.A", which means that if "CBS" appears, it will
        /// be automatically mapped to CBS. However, if we dynamically selected "CBS.A" - we might select
        /// a different company not associated with the ticker being referenced.
        /// </summary>
        public static readonly Dictionary<string, HashSet<string>> ShareClassMappedTickers = new Dictionary<string, HashSet<string>>
        {
            {"BRK", new HashSet<string>
                {
                    "BRK.A",
                    "BRK.B"
                }
            }
        };

        /// <summary>
        /// Creates a new instance of the json converter
        /// </summary>
        /// <param name="symbol">The <see cref="Symbol"/> instance associated with this news</param>
        /// <param name="liveMode">True if live mode, false for backtesting</param>
        public BenzingaNewsJsonConverter(Symbol symbol = null, bool liveMode = false)
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
            var article = value as BenzingaNews;

            var token = JToken.FromObject(article);
            var categoryTokens = new List<JObject>(article.Categories.Count);
            var symbolTokens = new List<JObject>(article.Symbols.Count);
            var tagTokens = new List<JObject>(article.Tags.Count);

            // In the loops below, we want to convert a List<T> to the following JSON structure:
            // ...
            // "channel/stock/tag": [
            //     {
            //         "name": string(T)
            //     }
            // ],
            // ...
            //
            // We then replace the existing entries in the current `token` to be this JArray representation
            // of the data contained within the List<T>. This is done to keep the data as close as possible
            // to its original format and so we can deserialize the raw data gathered from the API.
            foreach (var category in article.Categories)
            {
                var obj = new JObject();
                obj["name"] = new JValue(category);

                categoryTokens.Add(obj);
            }

            foreach (var symbol in article.Symbols)
            {
                var obj = new JObject();
                obj["name"] = new JValue(symbol.Value);

                symbolTokens.Add(obj);
            }

            foreach (var tag in article.Tags)
            {
                var obj = new JObject();
                obj["name"] = new JValue(tag);

                tagTokens.Add(obj);
            }

            token["channels"].Replace(JArray.FromObject(categoryTokens));
            token["stocks"].Replace(JArray.FromObject(symbolTokens));
            token["tags"].Replace(JArray.FromObject(tagTokens));

            // Sends off the JToken object to be converted into a string
            token.WriteTo(writer);
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
            var instance = DeserializeNews(data);

            instance.Symbol = _symbol;
            instance.EndTime = instance.UpdatedAt;

            return instance;
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
            return objectType == typeof(BenzingaNews);
        }

        /// <summary>
        /// Helper method to deserialize a single json Benzinga news
        /// </summary>
        /// <param name="item">The json token containing the Benzinga news to deserialize</param>
        /// <param name="enableLogging">true to enable logging (for debug purposes)</param>
        /// <returns>The deserialized <see cref="BenzingaNews"/> instance</returns>
        public static BenzingaNews DeserializeNews(JToken item, bool enableLogging = false)
        {
            var instance = new BenzingaNews
            {
                Id = item.Value<int>("id"),
                Author = item.Value<string>("author"),
                CreatedAt = item.Value<DateTime>("created").ToUniversalTime(),
                UpdatedAt = item.Value<DateTime>("updated").ToUniversalTime(),
                Title = item.Value<string>("title"),
                // Teasers are not present in the RSS data
                Teaser = item.Value<string>("teaser"),
                // Strip all HTML tags from the article, then convert HTML entities to their string representation
                // e.g. "<html><p>Apple&#39;s Earnings</p></html>" would become "Apple's Earnings"
                Contents = WebUtility.HtmlDecode(Regex.Replace(item.Value<string>("body"), @"<[^>]*>", " ")),
                Categories = new List<string>(),
                Symbols = new List<Symbol>(),
                Tags = new List<string>(),
            };

            if (item["channels"] != null)
            {
                // Get the JSON from the "channels" key and iterate on the various categories that they provide
                foreach (var category in JArray.Parse(item["channels"].ToString()))
                {
                    instance.Categories.Add(category.Value<string>("name"));
                }
            }

            if (item["tags"] != null)
            {
                // Ge tthe JSON from the "tags" key and iterate on the various categories that they provide
                foreach (var tag in JArray.Parse(item["tags"].ToString()))
                {
                    instance.Tags.Add(tag.Value<string>("name"));
                }
            }

            // Use this collection to get rid of duplicate symbols (verified manually that this occurs rarely)
            var tempSymbols = new HashSet<Symbol>();

            if (item["stocks"] != null)
            {
                // Get the JSON from the "stocks" key and iterate on the various stocks that they provide
                foreach (var ticker in JArray.Parse(item["stocks"].ToString()))
                {
                    // Tickers with dots in them like BRK.A and BRK.B appear as BRK-A and BRK-B in Benzinga data.
                    // They can also appear as BRK/B or BRK/A in some instances.
                    var symbolTicker = ticker.Value<string>("name").Trim().Replace('-', '.').Replace('/', '.');

                    // Tickers can be empty/null in Benzinga API responses.
                    // Verified by observing and processing empty ticker
                    if (string.IsNullOrWhiteSpace(symbolTicker))
                    {
                        if (enableLogging)
                        {
                            Log.Error($"BenzingaNewsJsonConverter.DeserializeNews(): Empty ticker was found in article with ID: {instance.Id}");
                        }
                        continue;
                    }

                    if (!ShareClassMappedTickers.ContainsKey(symbolTicker))
                    {
                        tempSymbols.Add(new Symbol(
                            SecurityIdentifier.GenerateEquity(symbolTicker, QuantConnect.Market.USA, mapSymbol: true, mappingResolveDate: instance.CreatedAt),
                            symbolTicker
                        ));
                    }
                    else
                    {
                        if (enableLogging)
                        {
                            Log.Trace($"BenzingaNewsJsonConverter.DeserializeNews(): Ticker {symbolTicker} will be added as: {string.Join(",", ShareClassMappedTickers[symbolTicker])}");
                        }
                        foreach (var mappedTicker in ShareClassMappedTickers[symbolTicker])
                        {
                            tempSymbols.Add(new Symbol(
                                SecurityIdentifier.GenerateEquity(mappedTicker, QuantConnect.Market.USA, mapSymbol: true, mappingResolveDate: instance.CreatedAt),
                                mappedTicker
                            ));
                        }
                    }
                }
            }

            instance.Symbols.AddRange(tempSymbols);
            return instance;
        }
    }
}
