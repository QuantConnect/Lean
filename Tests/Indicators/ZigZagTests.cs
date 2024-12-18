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
    [TestFixture]
    public class ZigZagTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new ZigZag("ZigZag", 0.05m, 10);
        }
        protected override string TestFileName => "spy_zigzag.csv";

        protected override string TestColumnName => "zigzag";

        [Test]
        public void HighPivotShouldBeZeroWhenAllValuesAreEqual()
        {
            var zigzag = new ZigZag("ZigZag", 0.05m, 5);
            var date = new DateTime(2024, 12, 2, 12, 0, 0);

            for (int i = 0; i < 10; i++)
            {
                var data = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = date,
                    Open = 5,
                    Low = 5,
                    High = 5,
                };
                zigzag.Update(data);
            }
            Assert.AreEqual(0m, zigzag.HighPivot);
        }

        [Test]
        public void LowPivotReflectsInputWhenValuesAreEqual()
        {
            var zigzag = new ZigZag("ZigZag", 0.05m, 5);
            var date = new DateTime(2024, 12, 2, 12, 0, 0);
            var value = 5m;

            for (int i = 0; i < 10; i++)
            {
                var data = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = date,
                    Open = value,
                    Low = value,
                    High = value,
                };
                zigzag.Update(data);
            }
            Assert.AreEqual(value, zigzag.LowPivot);
        }
    }
}