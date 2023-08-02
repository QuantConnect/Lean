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

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class SeriesJsonConverterTests
    {
        [Test]
        public void SerializeDeserializeReturnsSameSeriesValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "%", Color.Blue, ScatterMarkerSymbol.Diamond);
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
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
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

            var expectedX = Convert.ToInt64(Time.DateTimeToUnixTimeStamp(date2.ToUniversalTime())); // expect last dateTime (date2)
            Assert.AreEqual(1, result.Values.Count); // expect only one value
            Assert.AreEqual(expectedX, ((ChartPoint)result.Values[0]).x);
            Assert.AreEqual(3, ((ChartPoint)result.Values[0]).y); // expect sum of values (1 + 2)
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [Test]
        public void SerializeDeserializeReturnsSameCandlestickSeriesValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new CandlestickSeries("Pepito Grillo");
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
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
        }
    }
}
