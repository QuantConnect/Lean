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

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class UltimateOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 0.1m;
            VolumeRenkoBarSize = 10000000m;
            return new UltimateOscillator(7, 14, 28);
        }

        [TestCase(0f, 56)]
        public void IndicatorWorksAsExpectedWhenPricesDontVary(float price, int n)
        {
            var prices = Enumerable.Repeat(price, n);
            var indicator = CreateIndicator();
            var time = new DateTime(2000, 5, 28);

            var days = 1;
            foreach (var p in prices)
            {
                Assert.DoesNotThrow(() => indicator.Update(new TradeBar() { Time=time.AddDays(days), Close = (decimal)p, Low = (decimal)p, High = (decimal)p, Value = (decimal)p}));
                days++;
            }

            Assert.AreEqual((decimal)50, indicator.Current.Value);
        }

        protected override string TestFileName => "spy_ultosc.txt";

        protected override string TestColumnName => "ULTOSC_7_14_28";
    }
}