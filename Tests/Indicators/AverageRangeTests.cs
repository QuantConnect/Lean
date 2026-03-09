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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AverageRangeTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new AverageRange(20);
        }
        protected override string TestFileName => "spy_adr.csv";

        protected override string TestColumnName => "adr";

        [Test]
        public void ComputesCorrectly()
        {
            var period = 20;
            var adr = new AverageRange(period);
            var values = new List<TradeBar>();
            for (int i = 0; i < period; i++)
            {
                var value = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Now.AddSeconds(i),
                    High = 2 * i,
                    Low = i
                };
                adr.Update(value);
                values.Add(value);
            }
            var expected = values.Average(x => x.High - x.Low);
            Assert.AreEqual(expected, adr.Current.Value);
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var period = 5;
            var adr = new AverageRange(period);
            for (int i = 0; i < period; i++)
            {
                Assert.IsFalse(adr.IsReady);
                var value = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Now.AddSeconds(i),
                    High = 2 * i,
                    Low = i
                };
                adr.Update(value);
            }
            Assert.IsTrue(adr.IsReady);
        }
    }
}