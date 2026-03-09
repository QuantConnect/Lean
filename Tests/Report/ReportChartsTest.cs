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
using System.Linq;
using Python.Runtime;
using QuantConnect.Packets;
using QuantConnect.Report;
using Newtonsoft.Json;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class ReportChartTests
    {
        [Test]
        public void RunsAllReportChartTests()
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
            var backtestResult = GetBacktestResult();
            QuantConnect.Report.Report report = null;

            Assert.DoesNotThrow(() => report = new QuantConnect.Report.Report("Report", "Report", "v1.0.0", backtestResult, (LiveResult)null));
            string html = "";
            Assert.DoesNotThrow(() => report.Compile(out html, out _));
            Assert.IsNotEmpty(html);
        }

        [Test]
        public void ReportChartsColorMapWorksForEverySecurityType()
        {
            using (Py.GIL())
            {
                var reportChartsModule = Py.Import("ReportCharts");
                var reportChartsClass = reportChartsModule.GetAttr("ReportCharts");
                dynamic colorMap = reportChartsClass.GetAttr("color_map");
                var chartSecurities = new HashSet<string>();

                foreach (string security in colorMap.keys())
                {
                    chartSecurities.Add(security);
                }

                foreach (var security in Enum.GetValues(typeof(SecurityType)))
                {
                    if (security.ToString() != "Base" && security.ToString() != "Index")
                    {
                        Assert.IsTrue(chartSecurities.Contains(security.ToString()), $"{security} SecurityType is not present in ReportCharts.py color_map dictionary");
                    }
                }
            }
        }

        [TestCaseSource(nameof(CurrencySymbols))]
        public void EstimatedCapacityIsParsedRegardlessOfTheCurrency(string currencySymbol)
        {
            var backtestResult = new BacktestResult()
            {
                Statistics = new Dictionary<string, string>(){ { "Estimated Strategy Capacity", $"{currencySymbol}1,000,000.00" } }
            };
            QuantConnect.Report.ReportElements.EstimatedCapacityReportElement element = new("", "", backtestResult, new LiveResult());

            Assert.DoesNotThrow(() => element.Render());
        }

        [Test, Sequential]
        public void ProperlyRendersEstimatedCapacity(
            [Values(999d, 9999d, 99999d, 999999d, 9999999d, 99999999d, 999999999d, 9999999999d)] decimal capacity,
            [Values("1K", "10K", "100K", "1M", "10M", "100M", "1B", "10B")] string expectedRenderedCapacity)
        {
            var backtestResult = new BacktestResult()
            {
                Statistics = new Dictionary<string, string>() { { "Estimated Strategy Capacity", $"${capacity}" } }
            };
            QuantConnect.Report.ReportElements.EstimatedCapacityReportElement element = new("", "", backtestResult, new LiveResult());

            string renderedCapacity = element.Render();
            Assert.AreEqual(expectedRenderedCapacity, renderedCapacity);
        }

        [TestCase("€")]
        [TestCase("Fr")]
        [TestCase("ZRX")]
        public void GeneratesReportWithNonUSDCurrency(string currencySymbol)
        {
            var backtestResult = GetBacktestResult();
            var capacity = backtestResult.Statistics["Estimated Strategy Capacity"];
            backtestResult.Statistics["Estimated Strategy Capacity"] = capacity.Replace("$", currencySymbol, StringComparison.Ordinal);
            QuantConnect.Report.Report report = null;

            Assert.DoesNotThrow(() => report = new QuantConnect.Report.Report("Report", "Report", "v1.0.0", backtestResult, (LiveResult)null));
            string html = "";
            Assert.DoesNotThrow(() => report.Compile(out html, out _));
            Assert.IsNotEmpty(html);
        }

        static BacktestResult GetBacktestResult()
        {
            var backtestSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new NullResultValueTypeJsonConverter<BacktestResult>() },
                FloatParseHandling = FloatParseHandling.Decimal
            };
            var backtest = JsonConvert.DeserializeObject<BacktestResult>(
                File.ReadAllText(Path.Combine("TestData", "test_report_data.json")), backtestSettings);

            return backtest;
        }

        static IEnumerable<string> CurrencySymbols => Currencies.CurrencySymbols.Values.Distinct();
    }
}
