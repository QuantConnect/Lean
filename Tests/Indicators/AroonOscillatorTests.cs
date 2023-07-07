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
    public class AroonOscillatorTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            RenkoBarSize = 1m;
            VolumeRenkoBarSize = 0.5m;
            return new AroonOscillator(14, 14);
        }

        protected override string TestFileName => "spy_aroon_oscillator.txt";

        protected override string TestColumnName => "Aroon Oscillator 14";

        [Test]
        public override void ResetsProperly()
        {
            var aroon = new AroonOscillator(3, 3);
            aroon.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today,
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            aroon.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today.AddSeconds(1),
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            aroon.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today.AddSeconds(2),
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            Assert.IsFalse(aroon.IsReady);
            aroon.Update(new TradeBar
            {
                Symbol = Symbols.SPY,
                Time = DateTime.Today.AddSeconds(3),
                Open = 3m,
                High = 7m,
                Low = 2m,
                Close = 5m,
                Volume = 10
            });
            Assert.IsTrue(aroon.IsReady);

            aroon.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(aroon);
            TestHelper.AssertIndicatorIsInDefaultState(aroon.AroonUp);
            TestHelper.AssertIndicatorIsInDefaultState(aroon.AroonDown);
        }
    }
}