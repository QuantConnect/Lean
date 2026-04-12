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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class YangZhangVolatilityTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            return new YangZhangVolatility(9);
        }

        protected override string TestFileName => "spy_with_yzv.csv";

        protected override string TestColumnName => "YZV9";

        [Test]
        public void YzvComputesCorrectly()
        {
            // Hand-computed values for YZV(3) with known OHLC bars
            // Period 3, k = 0.34 / (1.34 + (3+1)/(3-1)) = 0.34 / (1.34 + 2) = 0.34 / 3.34
            var yzv = new YangZhangVolatility(3);
            var time = new DateTime(2024, 1, 1);

            // Bar 1 (seed): O=100, H=102, L=99, C=101
            yzv.Update(new TradeBar(time, Symbols.SPY, 100m, 102m, 99m, 101m, 1000));
            Assert.IsFalse(yzv.IsReady);
            Assert.AreEqual(0m, yzv.Current.Value);

            // Bar 2: O=101.5, H=103, L=100.5, C=102 (prev close = 101)
            yzv.Update(new TradeBar(time.AddDays(1), Symbols.SPY, 101.5m, 103m, 100.5m, 102m, 1000));
            Assert.IsFalse(yzv.IsReady);
            Assert.AreEqual(0m, yzv.Current.Value);

            // Bar 3: O=102.5, H=104, L=101.5, C=103 (prev close = 102)
            yzv.Update(new TradeBar(time.AddDays(2), Symbols.SPY, 102.5m, 104m, 101.5m, 103m, 1000));
            Assert.IsFalse(yzv.IsReady);
            Assert.AreEqual(0m, yzv.Current.Value);

            // Bar 4: O=103.5, H=105, L=102, C=104 (prev close = 103)
            // Now IsReady (Samples = 4 >= 3 + 1)
            yzv.Update(new TradeBar(time.AddDays(3), Symbols.SPY, 103.5m, 105m, 102m, 104m, 1000));
            Assert.IsTrue(yzv.IsReady);

            // Hand-compute expected value:
            // overnight returns: ln(101.5/101), ln(102.5/102), ln(103.5/103)
            var o1 = Math.Log(101.5 / 101.0);  // 0.004950...
            var o2 = Math.Log(102.5 / 102.0);  // 0.004889...
            var o3 = Math.Log(103.5 / 103.0);  // 0.004845...

            // intraday returns: ln(102/101.5), ln(103/102.5), ln(104/103.5)
            var c1 = Math.Log(102.0 / 101.5);  // 0.004914...
            var c2 = Math.Log(103.0 / 102.5);  // 0.004866...
            var c3 = Math.Log(104.0 / 103.5);  // 0.004819...

            // RS values: ln(H/C)*ln(H/O) + ln(L/C)*ln(L/O)
            var rs1 = Math.Log(103.0 / 102.0) * Math.Log(103.0 / 101.5) + Math.Log(100.5 / 102.0) * Math.Log(100.5 / 101.5);
            var rs2 = Math.Log(104.0 / 103.0) * Math.Log(104.0 / 102.5) + Math.Log(101.5 / 103.0) * Math.Log(101.5 / 102.5);
            var rs3 = Math.Log(105.0 / 104.0) * Math.Log(105.0 / 103.5) + Math.Log(102.0 / 104.0) * Math.Log(102.0 / 103.5);

            var n = 3.0;
            var k = 0.34 / (1.34 + (n + 1.0) / (n - 1.0));

            // Sample variance for overnight
            var sumO = o1 + o2 + o3;
            var sumOSq = o1 * o1 + o2 * o2 + o3 * o3;
            var oVar = (sumOSq - sumO * sumO / n) / (n - 1.0);

            // Sample variance for intraday
            var sumC = c1 + c2 + c3;
            var sumCSq = c1 * c1 + c2 * c2 + c3 * c3;
            var cVar = (sumCSq - sumC * sumC / n) / (n - 1.0);

            // RS mean
            var rsVar = (rs1 + rs2 + rs3) / n;

            var yzVar = oVar + k * cVar + (1.0 - k) * rsVar;
            var expected = Math.Sqrt(Math.Max(0, yzVar));

            Assert.AreEqual(expected, (double)yzv.Current.Value, 1e-10,
                "YZV at bar 4 should match hand-computed value");
        }
    }
}
