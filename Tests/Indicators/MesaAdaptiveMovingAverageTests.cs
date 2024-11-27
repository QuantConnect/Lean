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
    public class MesaAdaptiveMovingAverageTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new MesaAdaptiveMovingAverage("MAMA");
        }
        protected override string TestFileName => "spy_mama.csv";

        protected override string TestColumnName => "mama";

        [Test]
        public void DoesNotThrowDivisionByZero()
        {
            var mama = new MesaAdaptiveMovingAverage("MAMA");

            for (var i = 0; i < 500; i++)
            {
                var data = new TradeBar
                {
                    Symbol = Symbol.Empty,
                    Time = DateTime.Now.AddSeconds(i),
                    Open = 0,
                    Low = 0,
                    High = 0,
                    Close = 0
                };
                Assert.DoesNotThrow(() => mama.Update(data));
            }
        }
    }
}