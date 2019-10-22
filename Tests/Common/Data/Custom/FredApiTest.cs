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

using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom.Fred;
using QuantConnect.Data.UniverseSelection;
using System;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class FredApiTest
    {
        [Test]
        public void FredApiParsesDataCorrectly()
        {
            // Arrange
            var fileInfo = new FileInfo("./TestData/FredVixData.json");
            var content = File.ReadAllText(fileInfo.FullName);

            var sid = SecurityIdentifier.GenerateBase(typeof(BaseData), Fred.CBOE.VIX, QuantConnect.Market.USA, false);
            var symbol = new Symbol(sid, Fred.CBOE.VIX);
            var subscriptionDataConfig = new SubscriptionDataConfig(typeof(BaseData), symbol, Resolution.Daily,
                DateTimeZone.Utc, DateTimeZone.Utc, false, false, false, true);

            var fredApi = new FredApi();
            // Act
            var data = (BaseDataCollection)fredApi.Reader(subscriptionDataConfig, content, new DateTime(2019, 01, 04), false);
            // Assert
            Assert.AreEqual(18, data.Data.Count);
            Assert.AreEqual(23.22, data.Data.First().Value);
            Assert.AreEqual(18.87, data.Data.Last().Value);
            Assert.AreEqual(357.66m, data.Data.Sum(d => d.Value));
        }
    }
}