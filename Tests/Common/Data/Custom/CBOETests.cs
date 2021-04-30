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
using NUnit.Framework;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class CBOETests
    {
        [Test]
        public void EndTimeShiftedOneDayForward()
        {
            var date = new DateTime(2020, 5, 21);
            var cboe = new QuantConnect.Data.Custom.CBOE.CBOE();
            var cboeData = "2020-05-21,1,1,1,1";
            var symbol = new Symbol(SecurityIdentifier.GenerateBase(typeof(QuantConnect.Data.Custom.CBOE.CBOE), "VIX", QuantConnect.Market.USA), "VIX");
            var actual = cboe.Reader(new SubscriptionDataConfig(
                typeof(QuantConnect.Data.Custom.CBOE.CBOE),
                symbol,
                Resolution.Daily,
                QuantConnect.TimeZones.Utc,
                QuantConnect.TimeZones.Utc,
                false,
                false,
                false,
                true), cboeData, date, false);

            Assert.AreEqual(date, actual.Time);
            Assert.AreEqual(date.AddDays(1), actual.EndTime);
        }
    }
}
