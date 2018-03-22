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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RelativeStrengthIndexTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var rsiSimple = new RelativeStrengthIndex("rsi", 14, MovingAverageType.Simple);
            TestHelper.TestIndicator(rsiSimple, "RSI 14");

            var rsiWilder = new RelativeStrengthIndex("rsi", 14, MovingAverageType.Wilders);
            TestHelper.TestIndicator(rsiWilder, "RSI 14 Wilder");
        }

        [Test]
        public void ResetsProperly()
        {
            var rsi = new RelativeStrengthIndex(2);
            rsi.Update(DateTime.Today, 1m);
            rsi.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsFalse(rsi.IsReady);

            rsi.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(rsi);
            TestHelper.AssertIndicatorIsInDefaultState(rsi.AverageGain);
            TestHelper.AssertIndicatorIsInDefaultState(rsi.AverageLoss);
        }
    }
}
