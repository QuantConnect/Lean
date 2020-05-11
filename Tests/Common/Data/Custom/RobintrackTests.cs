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
using QuantConnect.Data.Custom.Robintrack;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class RobintrackTests
    {
        [Test]
        public void SerializeRoundTrip()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var time = new DateTime(2020, 3, 19, 10, 0, 0);
            var underlyingSymbol = Symbols.AAPL;
            var symbol = Symbol.CreateBase(typeof(RobintrackHoldings), underlyingSymbol, QuantConnect.Market.USA);

            var item = new RobintrackHoldings
            {
                Symbol = symbol,
                EndTime = time,
                UsersHolding = 1,
                TotalUniqueHoldings = 1000
            };

            var serialized = JsonConvert.SerializeObject(item, settings);
            var deserialized = JsonConvert.DeserializeObject<RobintrackHoldings>(serialized, settings);

            var percentBefore = item.UniverseHoldingPercent;
            var percentAfter = deserialized.UniverseHoldingPercent;

            Assert.AreEqual(symbol, deserialized.Symbol);
            Assert.AreEqual(symbol.Underlying, deserialized.Symbol.Underlying);
            Assert.AreEqual(1, deserialized.UsersHolding);
            Assert.AreEqual(1000, deserialized.TotalUniqueHoldings);
            Assert.AreEqual(percentBefore, percentAfter);
            Assert.AreEqual(time, deserialized.Time);
            Assert.AreEqual(time, deserialized.EndTime);
        }

        [Test]
        public void ReadsLine()
        {
            var line = "20200224 12:00:00,1234,2468.0";
            var instance = RobintrackHoldings.Read(line);

            var expectedDate = DateTime.SpecifyKind(new DateTime(2020, 2, 24, 12, 0, 0), DateTimeKind.Utc);

            Assert.AreEqual(expectedDate, instance.Time);
            Assert.AreEqual(expectedDate, instance.EndTime);
            Assert.AreEqual(1234, instance.UsersHolding);
            Assert.AreEqual(2468, instance.TotalUniqueHoldings);
            Assert.AreEqual(0.5m, instance.UniverseHoldingPercent);
        }
    }
}
