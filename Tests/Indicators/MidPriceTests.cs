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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class MidPriceTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new MidPrice(5);
        }

        protected override string TestFileName
        {
            get { return "spy_midprice.txt"; }
        }

        protected override string TestColumnName
        {
            get { return "MIDPRICE_5"; }
        }

        [Test]
        public void ProducesTheSameValuesAfterReset()
        {
            var midPrice = new MidPrice(3);
            var reference = new DateTime(2024, 1, 1);
            var bars = new[]
            {
                new TradeBar { High = 110m, Low = 100m },
                new TradeBar { High = 111m, Low = 101m },
                new TradeBar { High = 112m, Low = 102m },
                new TradeBar { High = 105m, Low = 95m }
            };

            var expected = new List<decimal>();
            for (var i = 0; i < bars.Length; i++)
            {
                bars[i].Time = reference.AddDays(i);
                midPrice.Update(bars[i]);
                expected.Add(midPrice.Current.Value);
            }

            midPrice.Reset();

            for (var i = 0; i < bars.Length; i++)
            {
                midPrice.Update(bars[i]);
                Assert.AreEqual(expected[i], midPrice.Current.Value);
            }
        }
    }
}
