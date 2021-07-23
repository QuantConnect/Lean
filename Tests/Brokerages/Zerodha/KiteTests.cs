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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Brokerages.Zerodha.Messages;

namespace QuantConnect.Tests.Brokerages.Zerodha
{
    [TestFixture]
    public class KiteTests
    {
        [Test]
        public void HistoricalCandleData()
        {
            var timeStamp = new DateTimeOffset(2021, 06, 11, 9, 15, 0, new TimeSpan(5, 30, 0));
            List<object> param = new List<object>(){ timeStamp, "1575", "1610.5", "1572", "1608.75", "2179" };
            var expectedCandleData = new Historical(param);
            
            Assert.AreEqual(new DateTime(2021, 06, 11, 3, 45, 0), expectedCandleData.TimeStamp);
            Assert.AreEqual(1575, expectedCandleData.Open);
            Assert.AreEqual(1610.5, expectedCandleData.High);
            Assert.AreEqual(1572, expectedCandleData.Low);
            Assert.AreEqual(1608.75, expectedCandleData.Close);
            Assert.AreEqual(2179, expectedCandleData.Volume);
        }
    }
}
