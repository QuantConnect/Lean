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
using QuantConnect.Data.Custom.PsychSignal;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class PsychSignalTests
    {
        [Test]
        public void SerializeRoundTrip()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(PsychSignalSentiment), underlyingSymbol, QuantConnect.Market.USA);

            var item = new PsychSignalSentiment
            {
                Symbol = symbol,
                Time = time,
                BullIntensity = 1,
                BearIntensity = 2,
                BullMinusBear = 3,
                BullScoredMessages = 4,
                BearScoredMessages = 5,
                BullBearMessageRatio = 6,
                TotalScoredMessages = 7
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<PsychSignalSentiment>(serialized, settings);

            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(1, deserialized.BullIntensity);
            Assert.AreEqual(2, deserialized.BearIntensity);
            Assert.AreEqual(3, deserialized.BullMinusBear);
            Assert.AreEqual(4, deserialized.BullScoredMessages);
            Assert.AreEqual(5, deserialized.BearScoredMessages);
            Assert.AreEqual(6, deserialized.BullBearMessageRatio);
            Assert.AreEqual(7, deserialized.TotalScoredMessages);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time.AddMinutes(1).AddSeconds(15), deserialized.EndTime);
        }
    }
}
