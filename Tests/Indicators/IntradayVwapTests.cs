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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IntradayVwapTests
    {
        private static TradeBar[] Session(DateTime day)
        {
            return new[]
            {
                new TradeBar { Time = day.AddHours(10), Open = 100m, High = 102m, Low = 99m, Close = 101m, Volume = 100 },
                new TradeBar { Time = day.AddHours(11), Open = 101m, High = 104m, Low = 100m, Close = 103m, Volume = 300 },
                new TradeBar { Time = day.AddHours(12), Open = 103m, High = 106m, Low = 102m, Close = 105m, Volume = 200 }
            };
        }

        [Test]
        public void ResetsProperly()
        {
            var vwap = new IntradayVwap("VWAP");
            foreach (var bar in Session(new DateTime(2024, 1, 2)))
            {
                vwap.Update(bar);
            }

            Assert.IsTrue(vwap.IsReady);

            vwap.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(vwap);
        }

        [Test]
        public void ProducesTheSameValuesAfterReset()
        {
            var vwap = new IntradayVwap("VWAP");
            var bars = Session(new DateTime(2024, 1, 2));

            var expected = new List<decimal>();
            foreach (var bar in bars)
            {
                vwap.Update(bar);
                expected.Add(vwap.Current.Value);
            }

            vwap.Reset();

            for (var i = 0; i < bars.Length; i++)
            {
                vwap.Update(bars[i]);
                Assert.AreEqual(expected[i], vwap.Current.Value);
            }
        }

        [Test]
        public void CarriesNoVolumeAcrossAResetWithinTheSameSession()
        {
            var day = new DateTime(2024, 1, 2);
            var bars = Session(day);

            var vwap = new IntradayVwap("VWAP");
            foreach (var bar in bars)
            {
                vwap.Update(bar);
            }
            vwap.Reset();
            vwap.Update(bars[0]);

            var fresh = new IntradayVwap("VWAP");
            fresh.Update(bars[0]);

            Assert.AreEqual(fresh.Current.Value, vwap.Current.Value);
        }
    }
}
