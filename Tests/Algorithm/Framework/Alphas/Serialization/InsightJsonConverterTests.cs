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
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Alphas.Serialization;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas.Serialization
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class InsightJsonConverterTests
    {
        [Test]
        public void DeserializesInsightWithoutScore()
        {
            var jObject = JObject.Parse(jsonNoScore);
            var result = JsonConvert.DeserializeObject<Insight>(jsonNoScore);
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
            var jObject = JObject.Parse(jsonWithScore);
            var result = JsonConvert.DeserializeObject<Insight>(jsonWithScore);
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

        [Test]
        public void SerializesInsightWithoutScore()
        {
            var jObject = JObject.Parse(jsonNoScore);
            var insight = Insight.FromSerializedInsight(new SerializedInsight
            {
                Id = jObject["id"].Value<string>(),
                SourceModel = jObject["source-model"].Value<string>(),
                GroupId = jObject["group-id"]?.Value<string>(),
                CreatedTime = jObject["created-time"].Value<double>(),
                CloseTime = jObject["close-time"].Value<double>(),
                Symbol = jObject["symbol"].Value<string>(),
                Ticker = jObject["ticker"].Value<string>(),
                Type = (InsightType)Enum.Parse(typeof(InsightType), jObject["type"].Value<string>(), true),
                ReferenceValue = jObject["reference"].Value<decimal>(),
                Direction = (InsightDirection)Enum.Parse(typeof(InsightDirection), jObject["direction"].Value<string>(), true),
                Period = jObject["period"].Value<double>(),
                Magnitude = jObject["magnitude"].Value<double>()
            });
            var result = JsonConvert.SerializeObject(insight, Formatting.None);
            Assert.AreEqual(jsonNoScore, result);
        }

        [Test]
        public void SerializesInsightWithScore()
        {
            var jObject = JObject.Parse(jsonWithScore);
            var insight = Insight.FromSerializedInsight(new SerializedInsight
            {
                Id = jObject["id"].Value<string>(),
                SourceModel = jObject["source-model"].Value<string>(),
                GroupId = jObject["group-id"]?.Value<string>(),
                CreatedTime = jObject["created-time"].Value<double>(),
                CloseTime = jObject["close-time"].Value<double>(),
                Symbol = jObject["symbol"].Value<string>(),
                Ticker = jObject["ticker"].Value<string>(),
                Type = (InsightType)Enum.Parse(typeof(InsightType), jObject["type"].Value<string>(), true),
                ReferenceValue = jObject["reference"].Value<decimal>(),
                Direction = (InsightDirection)Enum.Parse(typeof(InsightDirection), jObject["direction"].Value<string>(), true),
                Period = jObject["period"].Value<double>(),
                Magnitude = jObject["magnitude"].Value<double>(),
                ScoreIsFinal = jObject["score-final"].Value<bool>(),
                ScoreMagnitude = jObject["score-magnitude"].Value<double>(),
                ScoreDirection = jObject["score-direction"].Value<double>(),
                EstimatedValue = jObject["estimated-value"].Value<decimal>(),
                ReferenceValueFinal = jObject["reference-final"].Value<decimal>()
            });
            var result = JsonConvert.SerializeObject(insight, Formatting.None);
            Assert.AreEqual(jsonWithScore, result);
        }

        [Test]
        public void SerializesOldInsightWithMissingCreatedTime()
        {
            var serializedInsight = JsonConvert.DeserializeObject<SerializedInsight>(jsonWithMissingCreatedTime);
            var insight = Insight.FromSerializedInsight(serializedInsight);
            var result = JsonConvert.SerializeObject(insight, Formatting.None);

            Assert.AreEqual(serializedInsight.CreatedTime, serializedInsight.GeneratedTime);
            Assert.AreEqual(jsonWithExpectedOutputFromMissingCreatedTimeValue, result);
        }

        private const string jsonNoScore =
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
            "\"estimated-value\":\"0\"}";

        private const string jsonWithScore =
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
            "\"estimated-value\":\"1113.2484\"}";

        private const string jsonWithMissingCreatedTime =
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
            "\"estimated-value\":\"1113.2484\"}";

        private const string jsonWithExpectedOutputFromMissingCreatedTimeValue =
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
            "\"estimated-value\":\"1113.2484\"}";
    }

}
