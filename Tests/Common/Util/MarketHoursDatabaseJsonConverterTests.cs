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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class MarketHoursDatabaseJsonConverterTests
    {
        [Test]
        public void HandlesRoundTrip()
        {
            var database = MarketHoursDatabase.FromDataFolder();
            var result = JsonConvert.SerializeObject(database, Formatting.Indented);
            var deserializedDatabase = JsonConvert.DeserializeObject<MarketHoursDatabase>(result);

            var originalListing = database.ExchangeHoursListing.ToDictionary();
            foreach (var kvp in deserializedDatabase.ExchangeHoursListing)
            {
                var original = originalListing[kvp.Key];
                Assert.AreEqual(original.DataTimeZone, kvp.Value.DataTimeZone);
                foreach (var value in Enum.GetValues(typeof(DayOfWeek)))
                {
                    var day = (DayOfWeek) value;
                    var o = original.ExchangeHours.MarketHours[day];
                    var d = kvp.Value.ExchangeHours.MarketHours[day];
                    foreach (var pair in o.Segments.Zip(d.Segments, Tuple.Create))
                    {
                        Assert.AreEqual(pair.Item1.State, pair.Item2.State);
                        Assert.AreEqual(pair.Item1.Start, pair.Item2.Start);
                        Assert.AreEqual(pair.Item1.End, pair.Item2.End);
                    }
                }
            }
        }
    }
}
