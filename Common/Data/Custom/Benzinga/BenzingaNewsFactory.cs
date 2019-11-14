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
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Custom.Benzinga
{
    /// <summary>
    /// Benzinga news data deserialization utility methods - https://docs.benzinga.io/benzinga/newsfeed-v2.html
    /// </summary>
    public static class BenzingaNewsFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="BenzingaNews"/> from RSS data (historical)
        /// </summary>
        /// <param name="contents">Raw contents of the XML file</param>
        /// <param name="mapFileProvider">Map file provider</param>
        /// <param name="mapFileResolver">Map file resolver</param>
        /// <returns>BenzingaNews instance</returns>
        public static BenzingaNews CreateBenzingaNewsFromRSS(string contents, IMapFileProvider mapFileProvider, MapFileResolver mapFileResolver)
        {
            var item = JsonConvert.DeserializeObject<JObject>(contents)["rss"]["channel"]["item"];

            // Only process articles that contain tickers to disk
            if (item["bz:ticker"] == null)
            {
                return null;
            }

            var instance = new BenzingaNews
            {
                Id = Parse.Int(item.Value<string>("bz:id")),
                Author = item.Value<string>("dc:creator"),
                CreatedAt = item.Value<DateTime>("pubDate").ToUniversalTime(),
                UpdatedAt = item.Value<DateTime>("revisiondate").ToUniversalTime(),
                Title = item.Value<string>("title"),
                // Teasers are not present in the RSS data
                Teaser = string.Empty,
                // Strip all HTML tags from the article, then convert HTML entities to their string representation
                // e.g. "<html><p>Apple&#39;s Earnings</p></html>" would become "Apple's Earnings"
                Contents = WebUtility.HtmlDecode(Regex.Replace(item.Value<string>("description"), @"<[^>]*>", " ")),
                Categories = new List<string>(),
                Symbols = new List<Symbol>(),
                // We won't have any Tags since they're not present in the old data
                Tags = new List<string>()
            };

            // For instance.Categories
            foreach (var category in GetValuesFromTag(item, "category"))
            {
                if (string.IsNullOrWhiteSpace(category.Value<string>("@domain")))
                {
                    continue;
                }

                var name = WebUtility.HtmlDecode(category.Value<string>("#text"));
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                instance.Categories.Add(name);
            }

            // Use this collection to get rid of any duplicate symbols
            var tempSymbols = new HashSet<Symbol>();

            // For instance.Symbols
            foreach (var ticker in GetValuesFromTag(item, "bz:ticker"))
            {
                if (ticker["#text"] == null)
                {
                    continue;
                }

                // Tickers with dots in them like BRK.A and BRK.B appear as BRK-A and BRK-B in Benzinga data.
                var symbolTicker = ticker.Value<string>("#text").Trim().Replace('-', '.');
                var mappedSymbol = mapFileResolver.ResolveMapFile(symbolTicker, instance.CreatedAt).GetMappedSymbol(instance.CreatedAt);

                if (string.IsNullOrWhiteSpace(mappedSymbol))
                {
                    Log.Error($"BenzingaFactory.CreateBenzingaNewsFromRSS(): Failed to map old ticker {symbolTicker}. New ticker is null");
                    continue;
                }

                tempSymbols.Add(new Symbol(
                    SecurityIdentifier.GenerateEquity(symbolTicker, QuantConnect.Market.USA, mapSymbol: true, mapFileProvider: mapFileProvider, mappingResolveDate: instance.CreatedAt),
                    mappedSymbol
                ));
            }

            instance.Symbols.AddRange(tempSymbols);
            return instance;
        }

        /// <summary>
        /// Creates <see cref="BenzingaNews"/> instance from JSON (API response)
        /// </summary>
        /// <param name="contents">Contents of the JSON file (article)</param>
        /// <param name="mapFileProvider">Map file provider</param>
        /// <param name="mapFileResolver">Map file resolver</param>
        /// <returns>BenzingaNews instance</returns>
        /// <remarks>This method is provided to enable logging when deserializing Benzinga news data for debugging purposes</remarks>
        public static IEnumerable<BenzingaNews> CreateBenzingaNewsFromJSON(string contents, MapFileResolver mapFileResolver)
        {
            foreach (var item in JsonConvert.DeserializeObject<JArray>(contents))
            {
                var instance = BenzingaNewsJsonConverter.DeserializeNews(item);
                var tickers = JArray.Parse(item["stocks"].ToString());

                // Get the JSON from the "stocks" key and iterate on the various stocks that they provide.
                // This is for debugging purposes
                foreach (var ticker in tickers)
                {
                    // Tickers with dots in them like BRK.A and BRK.B appear as BRK-A and BRK-B in Benzinga data.
                    // They can also appear as BRK/B or BRK/A in some instances.
                    var symbolTicker = ticker.Value<string>("name").Trim().Replace('-', '.').Replace('/', '.');

                    // Tickers can be empty/null in Benzinga API responses.
                    // Verified by observing and processing empty ticker
                    if (string.IsNullOrWhiteSpace(symbolTicker))
                    {
                        Log.Error($"BenzingaFactory.CreateBenzingaNewsFromJSON(): Empty ticker was found in article with ID: {instance.Id}");
                        continue;
                    }

                    var mappedSymbol = mapFileResolver.ResolveMapFile(symbolTicker, instance.CreatedAt).GetMappedSymbol(instance.CreatedAt);
                    if (string.IsNullOrWhiteSpace(mappedSymbol))
                    {
                        Log.Error($"BenzingaFactory.CreateBenzingaNewsFromJSON(): Failed to map old ticker {symbolTicker}. New ticker is null");
                        continue;
                    }
                }

                // Make sure we don't yield if there are no stocks mentioned in an article.
                // Verified that this can occur by manually reviewing API responses.
                if (instance.Symbols.Count == 0)
                {
                    Log.Error($"BenzingaFactory.CreateBenzingaNewsFromJSON(): No symbols were found for article {instance.Id}");
                    continue;
                }

                yield return instance;
            }
        }

        /// <summary>
        /// Gets all value(s) associated with a tag. Since the JSON converter is dumb and
        /// doesn't know if there are supposed to be multiple entries for a certain tag,
        /// we must separately handle the Object and Array cases
        /// </summary>
        /// <param name="item">JObject to search in</param>
        /// <param name="tag">Name of the tag</param>
        /// <returns>List of JTokens</returns>
        private static List<JToken> GetValuesFromTag(JToken item, string tag)
        {
            var tickers = new List<JToken>();

            // Check for only a single ticker before we try to iterate
            if (item[tag].Type == JTokenType.Object)
            {
                tickers.Add(item[tag]);
            }
            else
            {
                tickers.AddRange(JArray.Parse(item[tag].ToString()));
            }

            return tickers;
        }
    }
}
