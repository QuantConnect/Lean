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
    public class AnalysisJsonConverterTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private static string Serialize(Analysis result)
            => JsonConvert.SerializeObject(result);

        private static Analysis Deserialize(string json)
            => JsonConvert.DeserializeObject<Analysis>(json);

        // ── Context type detection ─────────────────────────────────────────────

        [Test]
        public void DeserializesPlainContextAsBacktestAnalysisContext()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": { ""Sample"": ""some-value"" },
                ""Solutions"": [""Fix A""]
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<ResultsAnalysisContext>(result.Context);
            Assert.AreEqual("some-value", ((ResultsAnalysisContext)result.Context).Sample.ToString());
        }

        [Test]
        public void DeserializesOccurrencesObjectAsBacktestAnalysisRepeatedContext()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": { ""Sample"": ""first"", ""Occurrences"": 42 },
                ""Solutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<ResultsAnalysisRepeatedContext>(result.Context);
            var ctx = (ResultsAnalysisRepeatedContext)result.Context;
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
                ""Solutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsInstanceOf<ResultsAnalysisAggregateContext>(result.Context);
            var agg = (ResultsAnalysisAggregateContext)result.Context;
            var inner = new List<IResultsAnalysisContext>(agg);
            Assert.AreEqual(2, inner.Count);
            Assert.IsInstanceOf<ResultsAnalysisContext>(inner[0]);
            Assert.AreEqual("a", ((ResultsAnalysisContext)inner[0]).Sample.ToString());
            Assert.IsInstanceOf<ResultsAnalysisRepeatedContext>(inner[1]);
            Assert.AreEqual("b", ((ResultsAnalysisRepeatedContext)inner[1]).Sample.ToString());
            Assert.AreEqual(3, ((ResultsAnalysisRepeatedContext)inner[1]).Occurrences);
        }

        [Test]
        public void DeserializesNullContextAsNull()
        {
            var json = @"{
                ""Name"": ""TestAnalysis"",
                ""Context"": null,
                ""Solutions"": []
            }";

            var result = Deserialize(json);

            Assert.IsNull(result.Context);
        }

        // ── Fields ─────────────────────────────────────────────────────────────

        [Test]
        public void DeserializesNameAndSolutions()
        {
            var json = @"{
                ""Name"": ""FlatEquityCurveAnalysis"",
                ""Context"": { ""Sample"": 0 },
                ""Solutions"": [""Solution 1"", ""Solution 2""]
            }";

            var result = Deserialize(json);

            Assert.AreEqual("FlatEquityCurveAnalysis", result.Name);
            Assert.AreEqual(2, result.Solutions.Count);
            Assert.AreEqual("Solution 1", result.Solutions[0]);
            Assert.AreEqual("Solution 2", result.Solutions[1]);
        }

        [Test]
        public void MissingSolutionsDeserializesAsEmptyList()
        {
            var json = @"{ ""Name"": ""X"", ""Context"": null }";

            var result = Deserialize(json);

            Assert.IsNotNull(result.Solutions);
            Assert.AreEqual(0, result.Solutions.Count);
        }

        // ── Interface / list integration ───────────────────────────────────────

        [Test]
        public void ConverterIsUsedWhenDeserializingListOfInterface()
        {
            var json = @"[
                { ""Name"": ""A"", ""Context"": { ""Sample"": 1 }, ""Solutions"": [] },
                { ""Name"": ""B"", ""Context"": { ""Sample"": 2, ""Occurrences"": 5 }, ""Solutions"": [""Fix""] }
            ]";

            var results = JsonConvert.DeserializeObject<IReadOnlyList<Analysis>>(json);

            Assert.AreEqual(2, results.Count);
            Assert.IsInstanceOf<ResultsAnalysisContext>(results[0].Context);
            Assert.IsInstanceOf<ResultsAnalysisRepeatedContext>(results[1].Context);
            Assert.AreEqual(5, ((ResultsAnalysisRepeatedContext)results[1].Context).Occurrences);
        }

        // ── Round-trip ─────────────────────────────────────────────────────────

        [Test]
        public void RoundTripWithPlainContext()
        {
            var original = new Analysis(
                "SomeAnalysis",
                "Issue",
                new ResultsAnalysisContext("sample-string"),
                ["Fix this", "Or that"]);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.AreEqual(original.Name, result.Name);
            Assert.AreEqual(2, result.Solutions.Count);
            Assert.AreEqual(original.Solutions[0], result.Solutions[0]);
            Assert.AreEqual(original.Solutions[1], result.Solutions[1]);
            Assert.IsInstanceOf<ResultsAnalysisContext>(result.Context);
            Assert.AreEqual(
                ((ResultsAnalysisContext)original.Context).Sample.ToString(),
                ((ResultsAnalysisContext)result.Context).Sample.ToString());
        }

        [Test]
        public void RoundTripWithRepeatedContext()
        {
            var original = new Analysis(
                "RepeatedAnalysis",
                "Issue",
                new ResultsAnalysisRepeatedContext(["first", "second", "third"]),
                ["Reduce frequency"]);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.IsInstanceOf<ResultsAnalysisRepeatedContext>(result.Context);
            var ctx = (ResultsAnalysisRepeatedContext)result.Context;
            Assert.AreEqual(3, ctx.Occurrences);
            Assert.AreEqual("first", ctx.Sample.ToString());
        }

        [Test]
        public void RoundTripWithAggregateContext()
        {
            var original = new Analysis(
                "AggregateAnalysis",
                "Issue",
                new ResultsAnalysisAggregateContext([
                    new ResultsAnalysisContext("ctx-a"),
                    new ResultsAnalysisRepeatedContext(["x", "y"])
                ]),
                []);

            var json = Serialize(original);
            var result = Deserialize(json);

            Assert.IsInstanceOf<ResultsAnalysisAggregateContext>(result.Context);
            var inner = new List<IResultsAnalysisContext>((ResultsAnalysisAggregateContext)result.Context);
            Assert.AreEqual(2, inner.Count);
            Assert.IsInstanceOf<ResultsAnalysisContext>(inner[0]);
            Assert.IsInstanceOf<ResultsAnalysisRepeatedContext>(inner[1]);
            Assert.AreEqual(2, ((ResultsAnalysisRepeatedContext)inner[1]).Occurrences);
        }
    }
}
