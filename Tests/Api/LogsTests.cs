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
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the log reading endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class LogsTests
    {
        private const string SuccessfulBacktestLogResponse = @"{
            ""logs"": [ ""2013-10-07 13:31:00 Launching analysis"", ""2013-10-07 13:32:00 Error: boom"" ],
            ""length"": 1337,
            ""success"": true
        }";

        private const string SuccessfulLiveLogResponse = @"{
            ""logs"": [ ""2024-06-07 13:31:00 Launching analysis"", ""2024-06-07 13:32:00 Error: boom"" ],
            ""length"": 1337,
            ""deploymentOffset"": 1200,
            ""success"": true
        }";

        [Test]
        public void ReadBacktestLogSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulBacktestLogResponse);
            using var api = server.CreateApi();

            api.ReadBacktestLog(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", "Error", 10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/read/log", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("26c7bb06b8487cff1c7b3c44652b30f1", request.Body["backtestId"].Value<string>());
            Assert.AreEqual("Error", request.Body["query"].Value<string>());
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
        }

        [Test]
        public void ReadLiveLogsSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulLiveLogResponse);
            using var api = server.CreateApi();

            api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43", 10, 60, "Error", deploymentLogs: true);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/logs/read", request.Path);
            Assert.AreEqual("json", request.Body["format"].Value<string>());
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("L-6e9d8a78f5af89d401f630585be90e43", request.Body["algorithmId"].Value<string>());
            Assert.AreEqual(10, request.Body["startLine"].Value<int>());
            Assert.AreEqual(60, request.Body["endLine"].Value<int>());
            Assert.AreEqual("Error", request.Body["query"].Value<string>());
            Assert.IsTrue(request.Body["deploymentLogs"].Value<bool>());
        }

        [Test]
        public void ReadLiveLogsRequestsEveryDeploymentAndEveryLineByDefault()
        {
            using var server = new StubApiServer(SuccessfulLiveLogResponse);
            using var api = server.CreateApi();

            api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43");

            var body = server.GetSingleRequest().Body;
            Assert.IsFalse(body["deploymentLogs"].Value<bool>());
            Assert.AreEqual(JTokenType.Null, body["query"].Type);
        }

        [TestCase(0, 200)]
        [TestCase(500, 700)]
        public void ReadBacktestLogDefaultsTheEndLineToAFullWindow(int start, int expectedEnd)
        {
            using var server = new StubApiServer(SuccessfulBacktestLogResponse);
            using var api = server.CreateApi();

            api.ReadBacktestLog(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", start: start);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(start, body["start"].Value<int>());
            Assert.AreEqual(expectedEnd, body["end"].Value<int>());
        }

        [TestCase(0, 250)]
        [TestCase(500, 750)]
        public void ReadLiveLogsDefaultsTheEndLineToAFullWindow(int startLine, int expectedEndLine)
        {
            using var server = new StubApiServer(SuccessfulLiveLogResponse);
            using var api = server.CreateApi();

            api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43", startLine);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual(startLine, body["startLine"].Value<int>());
            Assert.AreEqual(expectedEndLine, body["endLine"].Value<int>());
        }

        [Test]
        public void ReadBacktestLogRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadBacktestLog(23456789, "26c7bb06b8487cff1c7b3c44652b30f1", start: 0, end: 201));
        }

        [Test]
        public void ReadLiveLogsRejectsAWindowWiderThanTheDocumentedMaximum()
        {
            using var api = new Api.Api();
            api.Initialize(0, "token", Globals.DataFolder);

            Assert.Throws<ArgumentException>(() => api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43", 0, 251));
        }

        [Test]
        public void ReadBacktestLogExposesTheTotalLogLineCount()
        {
            using var server = new StubApiServer(SuccessfulBacktestLogResponse);
            using var api = server.CreateApi();

            var response = api.ReadBacktestLog(23456789, "26c7bb06b8487cff1c7b3c44652b30f1");

            Assert.IsTrue(response.Success);
            Assert.AreEqual(1337, response.Length);
            Assert.AreEqual(2, response.Logs.Count);
            Assert.AreEqual("2013-10-07 13:32:00 Error: boom", response.Logs[1]);
        }

        [Test]
        public void ReadLiveLogsExposesTheTotalLogLineCountAndTheDeploymentOffset()
        {
            using var server = new StubApiServer(SuccessfulLiveLogResponse);
            using var api = server.CreateApi();

            var response = api.ReadLiveLogs(23456789, "L-6e9d8a78f5af89d401f630585be90e43");

            Assert.IsTrue(response.Success);
            Assert.AreEqual(1337, response.Length);
            Assert.AreEqual(1200, response.DeploymentOffset);
            Assert.AreEqual(2, response.Logs.Count);
        }

        [Test]
        public void ReadBacktestLogReportsTheApiErrors()
        {
            using var server = new StubApiServer(@"{ ""success"": false, ""errors"": [ ""Backtest not found"" ] }");
            using var api = server.CreateApi();

            var response = api.ReadBacktestLog(23456789, "26c7bb06b8487cff1c7b3c44652b30f1");

            Assert.IsFalse(response.Success);
            CollectionAssert.AreEqual(new[] { "Backtest not found" }, response.Errors);
        }
    }
}
