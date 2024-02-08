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
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class ScatterChartPointTests
    {
        [Test]
        public void JsonRoundTrip()
        {
            var chart = new Chart(Symbols.SPY);
            var series = new Series("SeriesName", SeriesType.Scatter);
            var point = new ScatterChartPoint() { y = 99, Time = new DateTime(2024, 01, 01), Tooltip = "Filled @ 88 tooltip test" };
            series.AddPoint(point);
            chart.AddSeries(series);

            var serialized = JsonConvert.SerializeObject(chart);
            var deserialized = JsonConvert.DeserializeObject<Chart>(serialized);

            Assert.AreEqual(1, deserialized.Series.Count);
            var deserializedSeries = deserialized.Series[series.Name];

            Assert.AreEqual(1, deserializedSeries.Values.Count);

            var assetPlotSeriesPoint = deserializedSeries.Values[0] as ScatterChartPoint;

            Assert.AreEqual(point.Time, assetPlotSeriesPoint.Time);
            Assert.AreEqual(point.Y, assetPlotSeriesPoint.Y);
            Assert.AreEqual(point.Tooltip, assetPlotSeriesPoint.Tooltip);
        }

        [Test]
        public void Clone()
        {
            var point = new ScatterChartPoint() { y = 99, Time = new DateTime(2024, 01, 01), Tooltip = "Filled @ 88 tooltip test" };
            var clone = (ScatterChartPoint)point.Clone();

            Assert.AreEqual(point.Time, clone.Time);
            Assert.AreEqual(point.Y, clone.Y);
            Assert.AreEqual(point.Tooltip, clone.Tooltip);
        }

        [TestCase("[890370000,1.0]", 1, null)]
        [TestCase("{ \"x\": 890370000, \"y\": 1.0}", 1, null)]
        [TestCase("{ \"x\": 890370000, \"y\": null}", null, null)]
        [TestCase("{ \"x\": 890370000, \"y\": null, \"tooltip\": \"a Test\"}", null, "a Test")]
        public void Deserialize(string serialized, decimal? expected, string toolTip)
        {
            var deserialized = JsonConvert.DeserializeObject<ScatterChartPoint>(serialized);

            var time = new DateTime(1998, 3, 20, 5, 0, 0);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(890370000, deserialized.X);
            Assert.AreEqual(expected, deserialized.Y);
            Assert.AreEqual(toolTip, deserialized.Tooltip);
        }
    }
}
