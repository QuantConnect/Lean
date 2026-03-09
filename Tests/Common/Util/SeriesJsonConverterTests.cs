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
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Util;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class SeriesJsonConverterTests
    {
        [TestCase(null, null, "Tooltip template")]
        [TestCase(87, null, "Tooltip template")]
        [TestCase(null, "Index Name", "Tooltip template")]
        [TestCase(87, "Index Name", "Tooltip template")]
        [TestCase(null,null, null)]
        [TestCase(87, null, null)]
        [TestCase(null, "Index Name", null)]
        [TestCase(87, "Index Name", null)]
        public void SerializeDeserializeReturnsSameSeriesValue(int? zIndex, string indexName, string toolTip)
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "%", Color.Blue, ScatterMarkerSymbol.Diamond) { ZIndex = zIndex, Index = 6, IndexName = indexName, Tooltip = toolTip };
            series.AddPoint(date, 1);
            series.AddPoint(date.AddSeconds(1), 2);

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (Series) JsonConvert.DeserializeObject(serializedSeries, typeof(Series));

            Assert.AreEqual(series.Values.Count, result.Values.Count);
            var values = series.GetValues<ChartPoint>().ToList();
            var resultValues = result.GetValues<ChartPoint>().ToList();
            for (var i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i].x, resultValues[i].x);
                Assert.AreEqual(values[i].y, resultValues[i].y);
            }
            Assert.AreEqual(series.Tooltip, result.Tooltip);
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
            Assert.AreEqual(series.ZIndex, result.ZIndex);
            Assert.AreEqual(series.Index, result.Index);
            Assert.AreEqual(series.IndexName, result.IndexName);
        }

        [Test]
        public void SerializedPieSeriesWillOnlyHaveOneValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var date2 = date.AddSeconds(1);
            var series = new Series("Pepito Grillo", SeriesType.Pie, "$", Color.Empty, ScatterMarkerSymbol.Diamond);
            series.AddPoint(date, 1);
            series.AddPoint(date2, 2);

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (Series)JsonConvert.DeserializeObject(serializedSeries, typeof(Series));

            var expectedX = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(date2)); // expect last dateTime (date2)
            Assert.AreEqual(1, result.Values.Count); // expect only one value
            Assert.AreEqual(expectedX, ((ChartPoint)result.Values[0]).x);
            Assert.AreEqual(3, ((ChartPoint)result.Values[0]).y); // expect sum of values (1 + 2)
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [TestCase(null, null, "Tooltip template")]
        [TestCase(87, null, "Tooltip template")]
        [TestCase(null, "Index Name", "Tooltip template")]
        [TestCase(87, "Index Name", "Tooltip template")]
        [TestCase(null, null, null)]
        [TestCase(87, null, null)]
        [TestCase(null, "Index Name", null)]
        [TestCase(87, "Index Name", null)]
        public void SerializeDeserializeReturnsSameCandlestickSeriesValue(int? zIndex, string indexName, string toolTip)
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new CandlestickSeries("Pepito Grillo") { ZIndex = zIndex, IndexName = indexName, Index = 7, Tooltip = toolTip };
            series.AddPoint(date, 100, 110, 80, 90);
            series.AddPoint(date.AddSeconds(1), 105, 115, 85, 95);

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (CandlestickSeries)JsonConvert.DeserializeObject(serializedSeries, typeof(CandlestickSeries));

            Assert.AreEqual(series.Values.Count, result.Values.Count);
            var values = series.GetValues<Candlestick>().ToList();
            var resultValues = result.GetValues<Candlestick>().ToList();
            for (var i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i].Time, resultValues[i].Time);
                Assert.AreEqual(values[i].Open, resultValues[i].Open);
                Assert.AreEqual(values[i].High, resultValues[i].High);
                Assert.AreEqual(values[i].Low, resultValues[i].Low);
                Assert.AreEqual(values[i].Close, resultValues[i].Close);
            }
            Assert.AreEqual(series.Tooltip, result.Tooltip);
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.ZIndex, result.ZIndex);
            Assert.AreEqual(series.Index, result.Index);
            Assert.AreEqual(series.IndexName, result.IndexName);
        }

        [Test]
        public void DeserializeChartPointObject()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var date2 = date.AddSeconds(1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "$", Color.Empty, ScatterMarkerSymbol.Diamond);
            series.AddPoint(date, 1);
            series.AddPoint(new ChartPoint(date2, null));

            var result = (Series)JsonConvert.DeserializeObject("{\"Name\":\"Pepito Grillo\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":3," +
                "\"Values\":[ {\"x\":2524611661,\"y\":1.0},{\"x\":2524611662,\"y\":null}],\"Color\":\"\",\"ScatterMarkerSymbol\":\"diamond\"}", typeof(Series));

            Assert.AreEqual(2, result.Values.Count);
            Assert.AreEqual(date, ((ChartPoint)result.Values[0]).Time);
            Assert.AreEqual(1, ((ChartPoint)result.Values[0]).y);
            Assert.AreEqual(date2, ((ChartPoint)result.Values[1]).Time);
            Assert.AreEqual(null, ((ChartPoint)result.Values[1]).y);
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [Test]
        public void DeserializeUpperCaseChartPoint()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var date2 = date.AddSeconds(1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "$", Color.Empty, ScatterMarkerSymbol.Diamond);
            series.AddPoint(date, 1);
            series.AddPoint(new ChartPoint(date2, null));

            var result = (Series)JsonConvert.DeserializeObject("{\"Name\":\"Pepito Grillo\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":3," +
                "\"Values\":[[2524611661,1.0],[2524611662,null]],\"Color\":\"\",\"ScatterMarkerSymbol\":\"diamond\"}", typeof(Series));

            Assert.AreEqual(2, result.Values.Count);
            Assert.AreEqual(date, ((ChartPoint)result.Values[0]).Time);
            Assert.AreEqual(1, ((ChartPoint)result.Values[0]).y);
            Assert.AreEqual(date2, ((ChartPoint)result.Values[1]).Time);
            Assert.AreEqual(null, ((ChartPoint)result.Values[1]).y);
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [Test]
        public void NullChartPointValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var date2 = date.AddSeconds(1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "$", Color.Empty, ScatterMarkerSymbol.Diamond);
            series.AddPoint(date, 1);
            series.AddPoint(new ChartPoint(date2, null));

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (Series)JsonConvert.DeserializeObject(serializedSeries, typeof(Series));

            Assert.AreEqual("{\"name\":\"Pepito Grillo\",\"unit\":\"$\",\"index\":0,\"seriesType\":3," +
                "\"values\":[[2524611661,1.0],[2524611662,null]],\"color\":\"\",\"scatterMarkerSymbol\":\"diamond\"}", serializedSeries);
            Assert.AreEqual(2, result.Values.Count);
            Assert.AreEqual(date, ((ChartPoint)result.Values[0]).Time);
            Assert.AreEqual(1, ((ChartPoint)result.Values[0]).y);
            Assert.AreEqual(date2, ((ChartPoint)result.Values[1]).Time);
            Assert.AreEqual(null, ((ChartPoint)result.Values[1]).y);
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [Test]
        public void DeserializeUpperCaseCandleStick()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new CandlestickSeries("Pepito Grillo");
            series.AddPoint(date, 100, 110, 80, 90);
            series.AddPoint(new Candlestick(date.AddSeconds(1), null, null, null, null));

            var result = (CandlestickSeries)JsonConvert.DeserializeObject("{\"Name\":\"Pepito Grillo\",\"Unit\":\"$\",\"Index\":0,\"SeriesType\":2," +
                "\"Values\":[[2524611661,100.0,110.0,80.0,90.0],[2524611662,null,null,null,null]]}", typeof(CandlestickSeries));

            Assert.AreEqual(series.Values.Count, result.Values.Count);
            var values = series.GetValues<Candlestick>().ToList();
            var resultValues = result.GetValues<Candlestick>().ToList();
            for (var i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i].Time, resultValues[i].Time);
                Assert.AreEqual(values[i].Open, resultValues[i].Open);
                Assert.AreEqual(values[i].High, resultValues[i].High);
                Assert.AreEqual(values[i].Low, resultValues[i].Low);
                Assert.AreEqual(values[i].Close, resultValues[i].Close);
            }
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
        }

        [Test]
        public void NullCandleStickValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new CandlestickSeries("Pepito Grillo");
            series.AddPoint(date, 100, 110, 80, 90);
            series.AddPoint(new Candlestick(date.AddSeconds(1), null, null, null, null));

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (CandlestickSeries)JsonConvert.DeserializeObject(serializedSeries, typeof(CandlestickSeries));

            Assert.AreEqual("{\"name\":\"Pepito Grillo\",\"unit\":\"$\",\"index\":0,\"seriesType\":2,\"values\":[[2524611661,100.0,110.0,80.0,90.0],[2524611662,null,null,null,null]]}", serializedSeries);
            Assert.AreEqual(series.Values.Count, result.Values.Count);
            var values = series.GetValues<Candlestick>().ToList();
            var resultValues = result.GetValues<Candlestick>().ToList();
            for (var i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i].Time, resultValues[i].Time);
                Assert.AreEqual(values[i].Open, resultValues[i].Open);
                Assert.AreEqual(values[i].High, resultValues[i].High);
                Assert.AreEqual(values[i].Low, resultValues[i].Low);
                Assert.AreEqual(values[i].Close, resultValues[i].Close);
            }
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
        }

        [Test]
        public void HandlesAnyBaseSeries()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var testSeries = new TestSeries();
            testSeries.AddPoint(new TestPoint { Time = date, Property = "Pepe" });

            var serializedSeries = JsonConvert.SerializeObject(testSeries);

            Assert.AreEqual("{\"name\":null,\"unit\":\"$\",\"index\":0,\"seriesType\":0,\"values\":[{\"time\":\"2050-01-01T01:01:01\",\"property\":\"Pepe\"}]}", serializedSeries);
        }

        private class TestSeries : BaseSeries
        {
            public override BaseSeries Clone(bool empty = false)
            {
                var series = new TestSeries();
                if (!empty)
                {
                    series.Values = CloneValues();
                }
                return series;
            }

            public override ISeriesPoint ConsolidateChartPoints()
            {
                throw new NotImplementedException();
            }
            public override void AddPoint(DateTime time, List<decimal> values)
            {
                throw new NotImplementedException();
            }
        }

        private class TestPoint : ISeriesPoint
        {
            [JsonProperty("time")]
            public DateTime Time { get; set; }
            [JsonProperty("property")]
            public string Property { get; set;}

            public ISeriesPoint Clone()
            {
                return new TestPoint { Property = Property, Time = Time };
            }
        }
    }
}
