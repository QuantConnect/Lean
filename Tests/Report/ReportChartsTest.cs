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

using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Packets;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using QuantConnect.Report;
using Newtonsoft.Json;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ReportChartTests
    {
        [Test]
        public void RunsAllReportCartTests()
        {
            using (Py.GIL())
            {
                var code = File.ReadAllText("../../../Report/ReportChartTests.py");
                using var scope = Py.CreateScope();
                Assert.DoesNotThrow(() => scope.Exec(code));
            }
        }

        [Test]
        public void ExposureReportWorksForEverySecurityType()
        {
            var backtestSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new NullResultValueTypeJsonConverter<BacktestResult>() },
                FloatParseHandling = FloatParseHandling.Decimal
            };
            var backtest = JsonConvert.DeserializeObject<BacktestResult>(
                File.ReadAllText(Path.Combine("TestData", "test_report_data.json")), backtestSettings);
            QuantConnect.Report.Report report = null;

            Assert.DoesNotThrow(() => report = new QuantConnect.Report.Report("Report", "Report", "v1.0.0", backtest, (LiveResult)null));
            string html = "";
            Assert.DoesNotThrow(() => report.Compile(out html, out _));
            Assert.IsNotEmpty(html);
        }
    }
}
