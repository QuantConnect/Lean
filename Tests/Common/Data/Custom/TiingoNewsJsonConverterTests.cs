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
using System.Globalization;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Data.Custom.Tiingo;
using QuantConnect.ToolBox.TiingoNewsConverter;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class TiingoNewsJsonConverterTests
    {
        [Test]
        public void DeserializeCorrectly()
        {
            var content = @"[{
    ""source"":""source"",
    ""crawlDate"":""2019-01-29T22:20:01.696871Z"",
    ""description"":""description"",
    ""url"":""url"",
    ""publishedDate"":""2019-01-29T22:17:00Z"",
    ""tags"":[ ""tag1"", ""tag2""],
    ""tickers"":[""aapl""],
    ""id"":1,
    ""title"":""title"",
    ""time"":""2019-01-29T23:17:00Z"",
},
{
    ""source"":""source"",
    ""crawlDate"":""2019-01-29T22:20:01.696871Z"",
    ""publishedDate"":""2019-01-29T22:20:01.696871Z"",
    ""tickers"":[],
    ""id"":2,
    ""title"":""title"",
    ""time"":""2019-01-29T23:20:01.696871Z""
}]";
            var result = JsonConvert.DeserializeObject<List<TiingoNews>>(content,
                new TiingoNewsJsonConverter(Symbols.SPY));

            Assert.AreEqual("2", result[0].ArticleID);
            Assert.AreEqual(
                DateTime.Parse("2019-01-29T22:20:01.696871", CultureInfo.InvariantCulture),
                result[0].CrawlDate);
            Assert.AreEqual(
                DateTime.Parse("2019-01-29T22:20:01.696871", CultureInfo.InvariantCulture),
                result[0].PublishedDate);
            Assert.AreEqual("title", result[0].Title);
            Assert.AreEqual(new List<Symbol>(), result[0].Symbols);
            Assert.AreEqual(new List<string>(), result[0].Tags);
            Assert.AreEqual(result[0].PublishedDate.Add(TiingoNewsConverter.HistoricalCrawlOffset), result[0].Time);
            Assert.AreEqual(result[0].PublishedDate.Add(TiingoNewsConverter.HistoricalCrawlOffset), result[0].EndTime);

            Assert.AreEqual(2, result.Count);

            Assert.AreEqual("1", result[1].ArticleID);
            Assert.AreEqual(
                DateTime.Parse("2019-01-29T22:20:01.696871", CultureInfo.InvariantCulture),
                result[1].CrawlDate);
            Assert.AreEqual(
                DateTime.Parse("2019-01-29T22:17:00", CultureInfo.InvariantCulture),
                result[1].PublishedDate);
            Assert.AreEqual("description", result[1].Description);
            Assert.AreEqual("source", result[1].Source);
            Assert.AreEqual(new List<string> { "tag1", "tag2" }, result[1].Tags);
            Assert.AreEqual(new List<Symbol> { Symbols.AAPL }, result[1].Symbols);
            Assert.AreEqual("title", result[1].Title);
            Assert.AreEqual("url", result[1].Url);
            Assert.AreEqual(Symbols.SPY, result[1].Symbol);
            Assert.AreEqual(result[1].PublishedDate.Add(TiingoNewsConverter.HistoricalCrawlOffset), result[1].Time);
            Assert.AreEqual(result[1].PublishedDate.Add(TiingoNewsConverter.HistoricalCrawlOffset), result[1].EndTime);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RespectsHistoricalCrawlOffset(bool liveMode)
        {
            var content = @"[{
    ""source"":""source"",
    ""crawlDate"":""2019-01-29T22:20:01.696871Z"",
    ""description"":""description"",
    ""url"":""url"",
    ""publishedDate"":""2018-01-29T22:17:00Z"",
    ""tags"":[ ""tag1"", ""tag2""],
    ""tickers"":[""aapl""],
    ""id"":1,
    ""title"":""title"",";
            if (!liveMode)
            {
                // live mode does not have 'time', this is added by the converter
                content += @" ""time"":""2018-01-29T23:17:00Z""";
            }
            content += @"}]";

            var result = JsonConvert.DeserializeObject<List<TiingoNews>>(content,
                new TiingoNewsJsonConverter(Symbols.SPY));

            if (liveMode)
            {
                Assert.AreEqual(result[0].CrawlDate, result[0].Time);
            }
            else
            {
                Assert.AreEqual(result[0].PublishedDate.Add(TiingoNewsConverter.HistoricalCrawlOffset), result[0].Time);
            }
            Assert.AreEqual(result[0].EndTime, result[0].Time);
        }

        [Test]
        public void SerializeRoundTrip()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var crawlDate = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(TiingoNews), underlyingSymbol, QuantConnect.Market.USA);
            var symbolList = new List<Symbol> { underlyingSymbol };
            var tags = new List<string> { "Stock", "Technology" };

            var item = new TiingoNews
            {
                ArticleID = "123456",
                Symbol = symbol,
                Symbols = symbolList,
                Tags = tags,
                Title = "title",
                CrawlDate = crawlDate,
                Time = crawlDate
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<TiingoNews>(serialized, settings);

            Assert.AreEqual("123456", deserialized.ArticleID);
            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(symbolList, deserialized.Symbols);
            Assert.AreEqual(tags, deserialized.Tags);
            Assert.AreEqual("title", deserialized.Title);
            Assert.AreEqual(crawlDate, deserialized.CrawlDate);
            Assert.AreEqual(crawlDate, deserialized.Time);
            Assert.AreEqual(crawlDate, deserialized.EndTime);
        }

        [Test]
        public void DoesNotFailIfTickerContainsSpace()
        {
            var content = @"[{
    ""source"":""source"",
    ""crawlDate"":""2019-01-29T22:20:01.696871Z"",
    ""description"":""description"",
    ""url"":""url"",
    ""publishedDate"":""2018-01-29T22:17:00Z"",
    ""tags"":[ ""tag1"", ""tag2""],
    ""tickers"":[""aapl"", ""iff 6"", ""abc|1"", ""zxc | 1""],
    ""id"":1,
    ""title"":""title""
}]";

            Assert.DoesNotThrow(()=>JsonConvert.DeserializeObject<List<TiingoNews>>(content,
                new TiingoNewsJsonConverter(Symbols.SPY)));

        }
    }
}
