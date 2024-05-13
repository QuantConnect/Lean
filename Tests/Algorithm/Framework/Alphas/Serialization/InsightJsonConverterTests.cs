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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Serialization;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas.Serialization
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InsightJsonConverterTests
    {
        private JsonSerializerSettings _serializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = true
                }
            }
        };

        [Test]
        public void DeserializesInsightWithoutScore()
        {
            var jObject = JObject.Parse(jsonNoScoreBackwardsCompatible);
            var result = JsonConvert.DeserializeObject<Insight>(jsonNoScoreBackwardsCompatible);
            Assert.AreEqual(jObject["id"].Value<string>(), result.Id.ToStringInvariant("N"));
            Assert.AreEqual(jObject["source-model"].Value<string>(), result.SourceModel);
            Assert.AreEqual(jObject["group-id"]?.Value<string>(), result.GroupId?.ToStringInvariant("N"));
            Assert.AreEqual(jObject["created-time"].Value<double>(), Time.DateTimeToUnixTimeStamp(result.GeneratedTimeUtc), 5e-4);
            Assert.AreEqual(jObject["close-time"].Value<double>(), Time.DateTimeToUnixTimeStamp(result.CloseTimeUtc), 5e-4);
            Assert.AreEqual(jObject["symbol"].Value<string>(), result.Symbol.ID.ToString());
            Assert.AreEqual(jObject["ticker"].Value<string>(), result.Symbol.Value);
            Assert.AreEqual(jObject["type"].Value<string>(), result.Type.ToLower());
            Assert.AreEqual(jObject["reference"].Value<decimal>(), result.ReferenceValue);
            Assert.AreEqual(jObject["direction"].Value<string>(), result.Direction.ToLower());
            Assert.AreEqual(jObject["period"].Value<double>(), result.Period.TotalSeconds);
            Assert.AreEqual(jObject["magnitude"].Value<double>(), result.Magnitude);
            Assert.AreEqual(null, result.Confidence);

            // default values for scores
            Assert.AreEqual(false, result.Score.IsFinalScore);
            Assert.AreEqual(0, result.ReferenceValueFinal);
            Assert.AreEqual(0, result.Score.Magnitude);
            Assert.AreEqual(0, result.Score.Direction);
        }

        [Test]
        public void DeserializesInsightWithScore()
        {
            var jObject = JObject.Parse(jsonWithScoreBackwardsCompatible);
            var result = JsonConvert.DeserializeObject<Insight>(jsonWithScoreBackwardsCompatible);
            Assert.AreEqual(jObject["id"].Value<string>(), result.Id.ToStringInvariant("N"));
            Assert.AreEqual(jObject["source-model"].Value<string>(), result.SourceModel);
            Assert.AreEqual(jObject["group-id"]?.Value<string>(), result.GroupId?.ToStringInvariant("N"));
            Assert.AreEqual(jObject["created-time"].Value<double>(), Time.DateTimeToUnixTimeStamp(result.GeneratedTimeUtc), 5e-4);
            Assert.AreEqual(jObject["close-time"].Value<double>(), Time.DateTimeToUnixTimeStamp(result.CloseTimeUtc), 5e-4);
            Assert.AreEqual(jObject["symbol"].Value<string>(), result.Symbol.ID.ToString());
            Assert.AreEqual(jObject["ticker"].Value<string>(), result.Symbol.Value);
            Assert.AreEqual(jObject["type"].Value<string>(), result.Type.ToLower());
            Assert.AreEqual(jObject["reference"].Value<decimal>(), result.ReferenceValue);
            Assert.AreEqual(jObject["direction"].Value<string>(), result.Direction.ToLower());
            Assert.AreEqual(jObject["period"].Value<double>(), result.Period.TotalSeconds);
            Assert.AreEqual(jObject["magnitude"].Value<double>(), result.Magnitude);
            Assert.AreEqual(null, result.Confidence);
            Assert.AreEqual(true, result.Score.IsFinalScore);
            Assert.AreEqual(jObject["score-magnitude"].Value<double>(), result.Score.Magnitude);
            Assert.AreEqual(jObject["score-direction"].Value<double>(), result.Score.Direction);
            Assert.AreEqual(jObject["reference-final"].Value<decimal>(), result.ReferenceValueFinal);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SerializesInsightWithoutScore(bool backwardsCompatible)
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(backwardsCompatible ? jsonNoScoreBackwardsCompatible : jsonNoScore2);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None, _serializerSettings);
            Assert.AreEqual(jsonNoScore2, result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SerializesInsightWithScore(bool backwardsCompatible)
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(backwardsCompatible ? jsonWithScoreBackwardsCompatible : jsonWithScore);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None, _serializerSettings);
            Assert.AreEqual(jsonWithScore, result);
        }

        [Test]
        public void SerializesOldInsightWithMissingCreatedTime()
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(jsonWithMissingCreatedTimeBackwardsCompatible);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None, _serializerSettings);

            Assert.AreEqual(serializedInsight.CreatedTime, serializedInsight.GeneratedTime);
            Assert.AreEqual(jsonWithExpectedOutputFromMissingCreatedTimeValue, result);
        }


        [TestCase(true)]
        [TestCase(false)]
        public void SerializesInsightWithTag(bool backwardsCompatible)
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(backwardsCompatible ? jsonWithTagBackwardsCompatible : jsonWithTag);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None, _serializerSettings);
            Assert.AreEqual(jsonWithTag, result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SerializesInsightWithoutTag(bool backwardsCompatible)
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(backwardsCompatible ? jsonWithoutTagBackwardsCompatible : jsonWithoutTag);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None, _serializerSettings);
            Assert.AreEqual(jsonWithoutTag, result);
        }

        [Test]
        public void DeserializesInsightWithTag()
        {
            var jObject = JObject.Parse(jsonWithTagBackwardsCompatible);
            var result = JsonConvert.DeserializeObject<Insight>(jsonWithTagBackwardsCompatible);
            Assert.AreEqual(jObject["tag"].Value<string>(), result.Tag);
        }

        [Test]
        public void DeserializesInsightWithoutTag()
        {
            var result = JsonConvert.DeserializeObject<Insight>(jsonWithoutTagBackwardsCompatible);
            Assert.IsNull(result.Tag);
        }

        private string jsonNoScore2 = @"{""id"":""e02be50f56a8496b9ba995d19a904ada"",""groupId"":null,""sourceModel"":""mySourceModel-1"",""generatedTime"":1520711961.00055,
""createdTime"":1520711961.00055,""closeTime"":1520711961.00055,""symbol"":""BTCUSD XJ"",""ticker"":""BTCUSD"",""type"":""price"",""reference"":9143.53,""referenceValueFinal"":0.0,
""direction"":""up"",""period"":5.0,""magnitude"":""0.025"",""confidence"":null,""weight"":null,""scoreIsFinal"":false,""scoreMagnitude"":""0"",""scoreDirection"":""0"",
""estimatedValue"":""0"",""tag"":null}".ReplaceLineEndings(string.Empty);

        private const string jsonNoScoreBackwardsCompatible =
            "{" +
            "\"id\":\"e02be50f56a8496b9ba995d19a904ada\"," +
            "\"group-id\":null," +
            "\"source-model\":\"mySourceModel-1\"," +
            "\"generated-time\":1520711961.00055," +
            "\"created-time\":1520711961.00055," +
            "\"close-time\":1520711961.00055," +
            "\"symbol\":\"BTCUSD XJ\"," +
            "\"ticker\":\"BTCUSD\"," +
            "\"type\":\"price\"," +
            "\"reference\":9143.53," +
            "\"reference-final\":0.0," +
            "\"direction\":\"up\"," +
            "\"period\":5.0," +
            "\"magnitude\":\"0.025\"," +
            "\"confidence\":null," +
            "\"weight\":null," +
            "\"score-final\":false," +
            "\"score-magnitude\":\"0\"," +
            "\"score-direction\":\"0\"," +
            "\"estimated-value\":\"0\"," +
            "\"tag\":null}";

        private string jsonWithScore = @"{""id"":""e02be50f56a8496b9ba995d19a904ada"",""groupId"":""a02be50f56a8496b9ba995d19a904ada"",""sourceModel"":""mySourceModel-1"",
""generatedTime"":1520711961.00055,""createdTime"":1520711961.00055,""closeTime"":1520711961.00055,""symbol"":""BTCUSD XJ"",""ticker"":""BTCUSD"",""type"":""price"",
""reference"":9143.53,""referenceValueFinal"":9243.53,""direction"":""up"",""period"":5.0,""magnitude"":""0.025"",""confidence"":null,""weight"":null,
""scoreIsFinal"":true,""scoreMagnitude"":""1"",""scoreDirection"":""1"",""estimatedValue"":""1113.2484"",""tag"":null}".ReplaceLineEndings(string.Empty);
        private const string jsonWithScoreBackwardsCompatible =
            "{" +
            "\"id\":\"e02be50f56a8496b9ba995d19a904ada\"," +
            "\"group-id\":\"a02be50f56a8496b9ba995d19a904ada\"," +
            "\"source-model\":\"mySourceModel-1\"," +
            "\"generated-time\":1520711961.00055," +
            "\"created-time\":1520711961.00055," +
            "\"close-time\":1520711961.00055," +
            "\"symbol\":\"BTCUSD XJ\"," +
            "\"ticker\":\"BTCUSD\"," +
            "\"type\":\"price\"," +
            "\"reference\":9143.53," +
            "\"reference-final\":9243.53," +
            "\"direction\":\"up\"," +
            "\"period\":5.0," +
            "\"magnitude\":\"0.025\"," +
            "\"confidence\":null," +
            "\"weight\":null," +
            "\"score-final\":true," +
            "\"score-magnitude\":\"1\"," +
            "\"score-direction\":\"1\"," +
            "\"estimated-value\":\"1113.2484\"," +
            "\"tag\":null}";

        private const string jsonWithMissingCreatedTimeBackwardsCompatible =
            "{" +
            "\"id\":\"e02be50f56a8496b9ba995d19a904ada\"," +
            "\"group-id\":\"a02be50f56a8496b9ba995d19a904ada\"," +
            "\"source-model\":\"mySourceModel-1\"," +
            "\"generated-time\":1520711961.00055," +
            "\"close-time\":1520711961.00055," +
            "\"symbol\":\"BTCUSD XJ\"," +
            "\"ticker\":\"BTCUSD\"," +
            "\"type\":\"price\"," +
            "\"reference\":9143.53," +
            "\"reference-final\":9243.53," +
            "\"direction\":\"up\"," +
            "\"period\":5.0," +
            "\"magnitude\":0.025," +
            "\"confidence\":null," +
            "\"weight\":null," +
            "\"score-final\":true," +
            "\"score-magnitude\":\"1\"," +
            "\"score-direction\":\"1\"," +
            "\"estimated-value\":\"1113.2484\"," +
            "\"tag\":null}";

        private string jsonWithExpectedOutputFromMissingCreatedTimeValue = @"{""id"":""e02be50f56a8496b9ba995d19a904ada"",""groupId"":""a02be50f56a8496b9ba995d19a904ada"",
""sourceModel"":""mySourceModel-1"",""generatedTime"":1520711961.00055,""createdTime"":1520711961.00055,""closeTime"":1520711961.00055,""symbol"":""BTCUSD XJ"",""ticker"":
""BTCUSD"",""type"":""price"",""reference"":9143.53,""referenceValueFinal"":9243.53,""direction"":""up"",""period"":5.0,""magnitude"":""0.025"",""confidence"":null,
""weight"":null,""scoreIsFinal"":true,""scoreMagnitude"":""1"",""scoreDirection"":""1"",""estimatedValue"":""1113.2484"",""tag"":null}".ReplaceLineEndings(string.Empty);

        private string jsonWithTag = @"{""id"":""e02be50f56a8496b9ba995d19a904ada"",""groupId"":""a02be50f56a8496b9ba995d19a904ada"",""sourceModel"":""mySourceModel-1"",
""generatedTime"":1520711961.00055,""createdTime"":1520711961.00055,""closeTime"":1520711961.00055,""symbol"":""BTCUSD XJ"",""ticker"":""BTCUSD"",""type"":
""price"",""reference"":9143.53,""referenceValueFinal"":9243.53,""direction"":""up"",""period"":5.0,""magnitude"":null,""confidence"":null,""weight"":null,
""scoreIsFinal"":true,""scoreMagnitude"":""1"",""scoreDirection"":""1"",""estimatedValue"":""1113.2484"",""tag"":""additional information""}".ReplaceLineEndings(string.Empty);
        private const string jsonWithTagBackwardsCompatible =
            "{" +
            "\"id\":\"e02be50f56a8496b9ba995d19a904ada\"," +
            "\"group-id\":\"a02be50f56a8496b9ba995d19a904ada\"," +
            "\"source-model\":\"mySourceModel-1\"," +
            "\"generated-time\":1520711961.00055," +
            "\"created-time\":1520711961.00055," +
            "\"close-time\":1520711961.00055," +
            "\"symbol\":\"BTCUSD XJ\"," +
            "\"ticker\":\"BTCUSD\"," +
            "\"type\":\"price\"," +
            "\"reference\":9143.53," +
            "\"reference-final\":9243.53," +
            "\"direction\":\"up\"," +
            "\"period\":5.0," +
            "\"magnitude\":null," +
            "\"confidence\":null," +
            "\"weight\":null," +
            "\"score-final\":true," +
            "\"score-magnitude\":\"1\"," +
            "\"score-direction\":\"1\"," +
            "\"estimated-value\":\"1113.2484\"," +
            "\"tag\":\"additional information\"}";

        private string jsonWithoutTag = @"{""id"":""e02be50f56a8496b9ba995d19a904ada"",""groupId"":""a02be50f56a8496b9ba995d19a904ada"",
""sourceModel"":""mySourceModel-1"",""generatedTime"":1520711961.00055,""createdTime"":1520711961.00055,""closeTime"":1520711961.00055,""symbol"":""BTCUSD XJ"",
""ticker"":""BTCUSD"",""type"":""price"",""reference"":9143.53,""referenceValueFinal"":9243.53,""direction"":""up"",""period"":5.0,""magnitude"":null,
""confidence"":null,""weight"":null,""scoreIsFinal"":true,""scoreMagnitude"":""1"",""scoreDirection"":""1"",""estimatedValue"":""1113.2484"",""tag"":null}".ReplaceLineEndings(string.Empty);

        private const string jsonWithoutTagBackwardsCompatible =
            "{" +
            "\"id\":\"e02be50f56a8496b9ba995d19a904ada\"," +
            "\"group-id\":\"a02be50f56a8496b9ba995d19a904ada\"," +
            "\"source-model\":\"mySourceModel-1\"," +
            "\"generated-time\":1520711961.00055," +
            "\"created-time\":1520711961.00055," +
            "\"close-time\":1520711961.00055," +
            "\"symbol\":\"BTCUSD XJ\"," +
            "\"ticker\":\"BTCUSD\"," +
            "\"type\":\"price\"," +
            "\"reference\":9143.53," +
            "\"reference-final\":9243.53," +
            "\"direction\":\"up\"," +
            "\"period\":5.0," +
            "\"magnitude\":null," +
            "\"confidence\":null," +
            "\"weight\":null," +
            "\"score-final\":true," +
            "\"score-magnitude\":\"1\"," +
            "\"score-direction\":\"1\"," +
            "\"estimated-value\":\"1113.2484\"," +
            "\"tag\":null}";
    }

}
