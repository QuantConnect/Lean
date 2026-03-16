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

using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class BacktestAnalysisResultJsonConverterTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private static string Serialize(BacktestAnalysisResult result)
            => JsonConvert.SerializeObject(result);

        private static BacktestAnalysisResult Deserialize(string json)
            => JsonConvert.DeserializeObject<BacktestAnalysisResult>(json);

        // ── Context type detection ─────────────────────────────────────────────

        [Test]
        public void DeserializesPlainContextAsBacktestAnalysisContext()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": { ""Sample"": ""some-value"" },
                ""PotentialSolutions"": [""Fix A""]
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<BacktestAnalysisContext>(result.Context);
            Assert.AreEqual("some-value", ((BacktestAnalysisContext)result.Context).Sample.ToString());
        }

        [Test]
        public void DeserializesOccurrencesObjectAsBacktestAnalysisRepeatedContext()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": { ""Sample"": ""first"", ""Occurrences"": 42 },
                ""PotentialSolutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<BacktestAnalysisRepeatedContext>(result.Context);
            var ctx = (BacktestAnalysisRepeatedContext)result.Context;
            Assert.AreEqual("first", ctx.Sample.ToString());
            Assert.AreEqual(42, ctx.Occurrences);
        }

        [Test]
        public void DeserializesArrayContextAsBacktestAnalysisAggregateContext()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": [
                    { ""Sample"": ""a"" },
                    { ""Sample"": ""b"", ""Occurrences"": 3 }
                ],
                ""PotentialSolutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<BacktestAnalysisAggregateContext>(result.Context);
            var agg = (BacktestAnalysisAggregateContext)result.Context;
            var inner = new List<IBacktestAnalysisContext>(agg);
            Assert.AreEqual(2, inner.Count);
            Assert.IsInstanceOf<BacktestAnalysisContext>(inner[0]);
            Assert.AreEqual("a", ((BacktestAnalysisContext)inner[0]).Sample.ToString());
            Assert.IsInstanceOf<BacktestAnalysisRepeatedContext>(inner[1]);
            Assert.AreEqual("b", ((BacktestAnalysisRepeatedContext)inner[1]).Sample.ToString());
            Assert.AreEqual(3, ((BacktestAnalysisRepeatedContext)inner[1]).Occurrences);
        }

        [Test]
        public void DeserializesNullContextAsNull()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": null,
                ""PotentialSolutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsNull(result.Context);
        }

        // ── Fields ─────────────────────────────────────────────────────────────

        [Test]
        public void DeserializesNameAndPotentialSolutions()
        {
            var json = @"{
                ""Name"": ""FlatEquityCurveAnalysis"",
                ""Context"": { ""Sample"": 0 },
                ""PotentialSolutions"": [""Solution 1"", ""Solution 2""]
            }";

            var result = Deserialize(json);

            Assert.AreEqual("FlatEquityCurveAnalysis", result.Name);
            Assert.AreEqual(2, result.PotentialSolutions.Count);
            Assert.AreEqual("Solution 1", result.PotentialSolutions[0]);
            Assert.AreEqual("Solution 2", result.PotentialSolutions[1]);
        }

        [Test]
        public void MissingPotentialSolutionsDeserializesAsEmptyList()
        {
            var json = @"{ ""Name"": ""X"", ""Context"": null }";

            var result = Deserialize(json);

            Assert.IsNotNull(result.PotentialSolutions);
            Assert.AreEqual(0, result.PotentialSolutions.Count);
        }

        // ── Interface / list integration ───────────────────────────────────────

        [Test]
        public void ConverterIsUsedWhenDeserializingListOfInterface()
        {
            var json = @"[
                { ""Name"": ""A"", ""Context"": { ""Sample"": 1 }, ""PotentialSolutions"": [] },
                { ""Name"": ""B"", ""Context"": { ""Sample"": 2, ""Occurrences"": 5 }, ""PotentialSolutions"": [""Fix""] }
            ]";

            var results = JsonConvert.DeserializeObject<IReadOnlyList<BacktestAnalysisResult>>(json);

            Assert.AreEqual(2, results.Count);
            Assert.IsInstanceOf<BacktestAnalysisContext>(results[0].Context);
            Assert.IsInstanceOf<BacktestAnalysisRepeatedContext>(results[1].Context);
            Assert.AreEqual(5, ((BacktestAnalysisRepeatedContext)results[1].Context).Occurrences);
        }

        // ── Round-trip ─────────────────────────────────────────────────────────

        [Test]
        public void RoundTripWithPlainContext()
        {
            var original = new BacktestAnalysisResult(
                "SomeAnalysis",
                new BacktestAnalysisContext("sample-string"),
                ["Fix this", "Or that"]);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.AreEqual(original.Name, result.Name);
            Assert.AreEqual(2, result.PotentialSolutions.Count);
            Assert.AreEqual(original.PotentialSolutions[0], result.PotentialSolutions[0]);
            Assert.AreEqual(original.PotentialSolutions[1], result.PotentialSolutions[1]);
            Assert.IsInstanceOf<BacktestAnalysisContext>(result.Context);
            Assert.AreEqual(
                ((BacktestAnalysisContext)original.Context).Sample.ToString(),
                ((BacktestAnalysisContext)result.Context).Sample.ToString());
        }

        [Test]
        public void RoundTripWithRepeatedContext()
        {
            var original = new BacktestAnalysisResult(
                "RepeatedAnalysis",
                new BacktestAnalysisRepeatedContext(["first", "second", "third"]),
                ["Reduce frequency"]);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.IsInstanceOf<BacktestAnalysisRepeatedContext>(result.Context);
            var ctx = (BacktestAnalysisRepeatedContext)result.Context;
            Assert.AreEqual(3, ctx.Occurrences);
            Assert.AreEqual("first", ctx.Sample.ToString());
        }

        [Test]
        public void RoundTripWithAggregateContext()
        {
            var original = new BacktestAnalysisResult(
                "AggregateAnalysis",
                new BacktestAnalysisAggregateContext([
                    new BacktestAnalysisContext("ctx-a"),
                    new BacktestAnalysisRepeatedContext(["x", "y"])
                ]),
                []);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.IsInstanceOf<BacktestAnalysisAggregateContext>(result.Context);
            var inner = new List<IBacktestAnalysisContext>((BacktestAnalysisAggregateContext)result.Context);
            Assert.AreEqual(2, inner.Count);
            Assert.IsInstanceOf<BacktestAnalysisContext>(inner[0]);
            Assert.IsInstanceOf<BacktestAnalysisRepeatedContext>(inner[1]);
            Assert.AreEqual(2, ((BacktestAnalysisRepeatedContext)inner[1]).Occurrences);
        }
    }
}
