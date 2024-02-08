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
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class ChartTests
    {
        [Test]
        public void AddingScatterPoint()
        {
            var chart = new Chart("ChartName");
            var series = new Series("Test1", SeriesType.Scatter);
            series.AddPoint(new DateTime(2023, 03, 03), 100);
            chart.AddSeries(series);

            var serialized = JsonConvert.SerializeObject(chart);
            var result = JsonConvert.DeserializeObject<Chart>(serialized);

            Assert.AreEqual(result.Name, chart.Name);
            Assert.AreEqual(result.Symbol, chart.Symbol);
            Assert.AreEqual(result.LegendDisabled, chart.LegendDisabled);
            Assert.AreEqual(result.Series.Count, chart.Series.Count);
            Assert.AreEqual(result.Series["Test1"].Tooltip, chart.Series["Test1"].Tooltip);
            CollectionAssert.AreEqual(result.Series.Select(x => $"{x.Key}:{string.Join(',', x.Value.Values)}"),
                chart.Series.OrderBy(x => x.Value.Values.Count).Select(x => $"{x.Key}:{string.Join(',', x.Value.Values)}"));
        }

        [TestCase(false, false, 0, "Tooltip template")]
        [TestCase(false, true, 0, "Tooltip template")]
        [TestCase(true, false, 0, "Tooltip template")]
        [TestCase(true, true, 0, "Tooltip template")]
        [TestCase(false, false, 1, "Tooltip template")]
        [TestCase(false, true, 1, "Tooltip template")]
        [TestCase(true, false, 1, "Tooltip template")]
        [TestCase(true, true, 1, "Tooltip template")]
        [TestCase(false, false, 0, null)]
        [TestCase(false, true, 0, null)]
        [TestCase(true, false, 0, null)]
        [TestCase(true, true, 0, null)]
        [TestCase(false, false, 1, null)]
        [TestCase(false, true, 1, null)]
        [TestCase(true, false, 1, null)]
        [TestCase(true, true, 1, null)]
        public void SerializeDeserializeReturnsSameSeriesValue(bool setSymbol, bool legendDisabled, int index, string toolTip)
        {
            var chart = new Chart("ChartName") { LegendDisabled = legendDisabled, Symbol = setSymbol ? Symbols.IBM : null };
            var series1 = new Series("Test1") { Index = index, Tooltip = toolTip };
            series1.AddPoint(new DateTime(2023, 03, 03), 100);
            series1.AddPoint(new DateTime(2023, 04, 03), 200);
            chart.AddSeries(series1);

            var series2 = new Series("Test2");
            series2.AddPoint(new DateTime(2023, 05, 03), 100);
            chart.AddSeries(series2);

            var serialized = JsonConvert.SerializeObject(chart);
            var result = JsonConvert.DeserializeObject<Chart>(serialized);

            Assert.AreEqual(result.Name, chart.Name);
            Assert.AreEqual(result.Symbol, chart.Symbol);
            Assert.AreEqual(result.LegendDisabled, chart.LegendDisabled);
            Assert.AreEqual(result.Series.Count, chart.Series.Count);
            Assert.AreEqual(result.Series["Test1"].Tooltip, chart.Series["Test1"].Tooltip);
            CollectionAssert.AreEqual(result.Series.Select(x => $"{x.Key}:{string.Join(',', x.Value.Values)}"),
                chart.Series.OrderBy(x => x.Value.Values.Count).Select(x => $"{x.Key}:{string.Join(',', x.Value.Values)}"));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Clone(bool setSymbol, bool legendDisabled)
        {
            var chart = new Chart("ChartName") { LegendDisabled = legendDisabled, Symbol = setSymbol ? Symbols.IBM : null };
            var result = chart.Clone();

            Assert.AreEqual(result.Name, chart.Name);
            Assert.AreEqual(result.Symbol, chart.Symbol);
            Assert.AreEqual(result.LegendDisabled, chart.LegendDisabled);
        }
    }
}
