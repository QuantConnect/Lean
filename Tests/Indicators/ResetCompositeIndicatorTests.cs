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
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    public class ResetCompositeIndicatorTests : CompositeIndicatorTests
    {
        [Test]
        public override void ResetsProperly()
        {
            var left = new Maximum("left", 2);
            var right = new Minimum("right", 2);
            var resetActionExectuted = false;
            var resetAction = () =>
            {
                resetActionExectuted = true;
            };
            var composite = new ResetCompositeIndicator(left, right, (l, r) => l.Current.Value + r.Current.Value, resetAction);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, -1m);

            left.Update(DateTime.Today.AddDays(1), -1m);
            right.Update(DateTime.Today.AddDays(1), 1m);

            Assert.AreEqual(left.PeriodsSinceMaximum, 1);
            Assert.AreEqual(right.PeriodsSinceMinimum, 1);

            composite.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(composite);
            TestHelper.AssertIndicatorIsInDefaultState(left);
            TestHelper.AssertIndicatorIsInDefaultState(right);
            Assert.AreEqual(left.PeriodsSinceMaximum, 0);
            Assert.AreEqual(right.PeriodsSinceMinimum, 0);
            Assert.IsTrue(resetActionExectuted);
        }

        protected override CompositeIndicator CreateCompositeIndicator(IndicatorBase left, IndicatorBase right, CompositeIndicator.IndicatorComposer composer)
        {
            return new ResetCompositeIndicator(left, right, composer, () => { });
        }
    }
}
