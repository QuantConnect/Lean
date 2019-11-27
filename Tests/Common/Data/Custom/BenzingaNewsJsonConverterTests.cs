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
using QuantConnect.Data.Custom.Benzinga;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class BenzingaNewsJsonConverterTests
    {
        [Test]
        public void DeserializesCorrectly()
        {
            var content = @"{
                ""id"": 1,
                ""author"": ""Gerardo"",
                ""created"": ""2018-01-25T12:00:00Z"",
                ""updated"": ""2018-01-26T12:00:00Z"",
                ""title"": ""Unit Test Beats Expectations"",
                ""teaser"": ""The unit test beat reviewer's expectations, reporters say"",
                ""body"": ""<p>The unit test beat reviewer's expectations, reporters say - 'This is the best test I've ever seen' says Martin</p>"",
                ""channels"": [
                    {
                        ""name"": ""earnings""
                    }
                ],
                ""stocks"": [
                    {
                        ""name"": ""AAPL""
                    },
                ],
                ""tags"": [
                    {
                        ""name"": ""unit test""
                    },
                    {
                        ""name"": ""testing""
                    }
                ]
            }";

            // Put in a single line to avoid potential failure due to platform-specific behavior (\r\n vs. \n)
            var expectedSerialized = @"{""id"":1,""author"":""Gerardo"",""created"":""2018-01-25T12:00:00Z"",""updated"":""2018-01-26T12:00:00Z"",""title"":""Unit Test Beats Expectations"",""teaser"":""The unit test beat reviewer's expectations, reporters say"",""body"":"" The unit test beat reviewer's expectations, reporters say - 'This is the best test I've ever seen' says Martin "",""channels"":[{""name"":""earnings""}],""stocks"":[{""name"":""AAPL""}],""tags"":[{""name"":""unit test""},{""name"":""testing""}]}";
            var expectedSymbol = new Symbol(
                SecurityIdentifier.GenerateEquity(
                    "AAPL",
                    QuantConnect.Market.USA,
                    true,
                    null,
                    new DateTime(2018, 1, 25)
                ),
                "AAPL"
            );
            var expectedBaseSymbol = new Symbol(
                SecurityIdentifier.GenerateBase(
                    typeof(BenzingaNews),
                    "AAPL",
                    QuantConnect.Market.USA,
                    mapSymbol: true,
                    date: new DateTime(2018, 1, 25)
                ),
                "AAPL"
            );

            var result = JsonConvert.DeserializeObject<BenzingaNews>(content, new BenzingaNewsJsonConverter(symbol: expectedBaseSymbol, liveMode: false));
            var serializedResult = JsonConvert.SerializeObject(result, Formatting.None, new BenzingaNewsJsonConverter(symbol: expectedBaseSymbol, liveMode: false));
            var resultFromSerialized = JsonConvert.DeserializeObject<BenzingaNews>(serializedResult, new BenzingaNewsJsonConverter(symbol: expectedBaseSymbol, liveMode: false));

            Assert.AreEqual(expectedSerialized, serializedResult);

            Assert.AreEqual(1, result.Id);
            Assert.AreEqual(
                Parse.DateTimeExact("2018-01-25T12:00:00Z", "yyyy-MM-ddTHH:mm:ssZ", DateTimeStyles.AdjustToUniversal),
                result.CreatedAt);
            Assert.AreEqual(
                Parse.DateTimeExact("2018-01-26T12:00:00Z", "yyyy-MM-ddTHH:mm:ssZ", DateTimeStyles.AdjustToUniversal),
                result.UpdatedAt);

            Assert.AreEqual(result.UpdatedAt, result.EndTime);

            Assert.AreEqual("Gerardo", result.Author);
            Assert.AreEqual("Unit Test Beats Expectations", result.Title);
            Assert.AreEqual("The unit test beat reviewer's expectations, reporters say", result.Teaser);
            Assert.AreEqual(" The unit test beat reviewer's expectations, reporters say - 'This is the best test I've ever seen' says Martin ", result.Contents);

            Assert.AreEqual(new List<string> { "earnings" }, result.Categories);
            Assert.AreEqual(new List<string> { "unit test", "testing" }, result.Tags);
            Assert.AreEqual(new List<Symbol> { expectedSymbol }, result.Symbols);

            // Now begin comparing the resultFromSerialized and result instances
            Assert.AreEqual(result.Id, resultFromSerialized.Id);
            Assert.AreEqual(result.Author, resultFromSerialized.Author);
            Assert.AreEqual(result.CreatedAt, resultFromSerialized.CreatedAt);
            Assert.AreEqual(result.UpdatedAt, resultFromSerialized.UpdatedAt);
            Assert.AreEqual(result.Title, resultFromSerialized.Title);
            Assert.AreEqual(result.Teaser, resultFromSerialized.Teaser);
            Assert.AreEqual(result.Contents, resultFromSerialized.Contents);
            Assert.AreEqual(result.Categories, resultFromSerialized.Categories);
            Assert.AreEqual(result.Symbols, resultFromSerialized.Symbols);
            Assert.AreEqual(result.Tags, resultFromSerialized.Tags);
        }
    }
}
