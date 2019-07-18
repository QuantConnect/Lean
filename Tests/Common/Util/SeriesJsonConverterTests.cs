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
using Newtonsoft.Json;
using NUnit.Framework;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class SeriesJsonConverterTests
    {
        [Test]
        public void SerializeDeserializeReturnsSameValue()
        {
            var date = new DateTime(2050, 1, 1, 1, 1, 1);
            var series = new Series("Pepito Grillo", SeriesType.Bar, "%", Color.Blue, ScatterMarkerSymbol.Diamond);
            series.AddPoint(date, 1);
            series.AddPoint(date.AddSeconds(1), 2);

            var serializedSeries = JsonConvert.SerializeObject(series);
            var result = (Series) JsonConvert.DeserializeObject(serializedSeries, typeof(Series));

            Assert.AreEqual(series.Values.Count, result.Values.Count);
            for (var i = 0; i < series.Values.Count; i++)
            {
                Assert.AreEqual(series.Values[i].x, result.Values[i].x);
                Assert.AreEqual(series.Values[i].y, result.Values[i].y);
            }
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }

        [Test]
        public void SerializedPieWillOnlyHaveOneValue()
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
            Assert.AreEqual(expectedX, result.Values[0].x);
            Assert.AreEqual(3, result.Values[0].y); // expect sum of values (1 + 2)
            Assert.AreEqual(series.Name, result.Name);
            Assert.AreEqual(series.Unit, result.Unit);
            Assert.AreEqual(series.SeriesType, result.SeriesType);
            Assert.AreEqual(series.Color.ToArgb(), result.Color.ToArgb());
            Assert.AreEqual(series.ScatterMarkerSymbol, result.ScatterMarkerSymbol);
        }
    }
}
