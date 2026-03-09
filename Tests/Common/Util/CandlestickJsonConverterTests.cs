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

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class CandlestickJsonConverterTests
    {
        [Test]
        public void SerializeDeserializeReturnsSameValue()
        {
            var dateTime = new DateTime(2023, 08, 01, 12, 11, 10);
            var candlestick = new Candlestick(dateTime, 100, 110, 80, 90);

            var serializedCandlestick = JsonConvert.SerializeObject(candlestick);
            var result = (Candlestick)JsonConvert.DeserializeObject(serializedCandlestick, typeof(Candlestick));

            Assert.AreEqual(candlestick.Time, result.Time);
            Assert.AreEqual(candlestick.Open, result.Open);
            Assert.AreEqual(candlestick.High, result.High);
            Assert.AreEqual(candlestick.Low, result.Low);
            Assert.AreEqual(candlestick.Close, result.Close);
        }

        [Test]
        public void BackwardsCompatility()
        {
            var dateTime = new DateTime(2023, 08, 01, 12, 11, 10);
            var chartPoint = new ChartPoint(dateTime, 100);

            var serializedChartPoint = JsonConvert.SerializeObject(chartPoint);
            var result = (Candlestick)JsonConvert.DeserializeObject(serializedChartPoint, typeof(Candlestick));

            Assert.AreEqual(chartPoint.Time, result.Time);
            Assert.AreEqual(chartPoint.y, result.Open);
            Assert.AreEqual(chartPoint.y, result.High);
            Assert.AreEqual(chartPoint.y, result.Low);
            Assert.AreEqual(chartPoint.y, result.Close);
        }
    }
}
