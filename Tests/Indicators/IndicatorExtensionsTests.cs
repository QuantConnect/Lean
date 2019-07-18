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
using System.Linq;
using Python.Runtime;
using QuantConnect.Data;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class IndicatorExtensionsTests
    {
        [Test]
        public void PipesDataUsingOfFromFirstToSecond()
        {
            var first = new SimpleMovingAverage(2);
            var second = new Delay(1);

            // this is a configuration step, but returns the reference to the second for method chaining
            second.Of(first);

            var data1 = new IndicatorDataPoint(DateTime.UtcNow, 1m);
            var data2 = new IndicatorDataPoint(DateTime.UtcNow, 2m);
            var data3 = new IndicatorDataPoint(DateTime.UtcNow, 3m);
            var data4 = new IndicatorDataPoint(DateTime.UtcNow, 4m);

            // sma has one item
            first.Update(data1);
            Assert.IsFalse(first.IsReady);
            Assert.AreEqual(0m, second.Current.Value);

            // sma is ready, delay will repeat this value
            first.Update(data2);
            Assert.IsTrue(first.IsReady);
            Assert.IsFalse(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // delay is ready, and repeats its first input
            first.Update(data3);
            Assert.IsTrue(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // now getting the delayed data
            first.Update(data4);
            Assert.AreEqual(2.5m, second.Current.Value);
        }

        [Test]
        public void PipesDataFirstWeightedBySecond()
        {
            const int period = 4;
            var value = new Identity("Value");
            var weight = new Identity("Weight");

            var third = value.WeightedBy(weight, period);

            var data = Enumerable.Range(1, 10).ToList();
            var window = Enumerable.Reverse(data).Take(period);
            var current = window.Sum(x => 2 * x * x) / (decimal)window.Sum(x => x);

            foreach (var item in data)
            {
                value.Update(new IndicatorDataPoint(DateTime.UtcNow, Convert.ToDecimal(2 * item)));
                weight.Update(new IndicatorDataPoint(DateTime.UtcNow, Convert.ToDecimal(item)));
            }

            Assert.AreEqual(current, third.Current.Value);
        }

        [Test]
        public void NewDataPushesToDerivedIndicators()
        {
            var identity = new Identity("identity");
            var sma = new SimpleMovingAverage(3);

            identity.Updated += (sender, consolidated) =>
            {
                sma.Update(consolidated);
            };

            identity.Update(DateTime.UtcNow, 1m);
            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(sma.IsReady);
            Assert.AreEqual(2m, sma);
        }

        [Test]
        public void MultiChainSMA()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the SMA of the delay of the identity
            var sma = delay.Of(identity).SMA(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(sma.IsReady);

            Assert.AreEqual(1.5m, sma);
        }

        [Test]
        public void MultiChainEMA()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the EMA of chained methods
            var ema = delay.Of(identity).EMA(2, 1);

            // Assert.IsTrue(ema. == 1);
            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(ema.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(ema.IsReady);
        }

        [Test]
        public void MultiChainMAX()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the MAX of the delay of the identity
            var max = delay.Of(identity).MAX(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(max.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(max.IsReady);
        }

        [Test]
        public void MultiChainMIN()
        {
            var identity = new Identity("identity");
            var delay = new Delay(2);

            // create the MIN of the delay of the identity
            var min = delay.Of(identity).MIN(2);

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(min.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(min.IsReady);
        }

        [Test]
        public void PlusAddsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Plus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(2m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(7m, composite.Current.Value);
        }

        [Test]
        public void PlusAddsLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Plus(5);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(6m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(7m, composite.Current.Value);
        }

        [Test]
        public void MinusSubtractsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Minus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(0m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(-1m, composite.Current.Value);
        }

        [Test]
        public void MinusSubtractsLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Minus(1);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);
        }

        [Test]
        public void OverDividesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Over(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(3m / 4m, composite.Current.Value);
        }

        [Test]
        public void OverDividesLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Over(2);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 4m);
            Assert.AreEqual(2m, composite.Current.Value);
        }

        [Test]
        public void OverHandlesDivideByZero()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Over(right);
            var updatedEventFired = false;
            composite.Updated += delegate { updatedEventFired = true; };

            left.Update(DateTime.Today, 1m);
            Assert.IsFalse(updatedEventFired);
            right.Update(DateTime.Today, 0m);
            Assert.IsFalse(updatedEventFired);

            // submitting another update to right won't cause an update without corresponding update to left
            right.Update(DateTime.Today, 1m);
            Assert.IsFalse(updatedEventFired);
            left.Update(DateTime.Today, 1m);
            Assert.IsTrue(updatedEventFired);
        }

        [Test]
        public void TimesMultipliesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Times(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(12m, composite.Current.Value);
        }

        [Test]
        public void TimesMultipliesLeftAndConstant()
        {
            var left = new Identity("left");
            var composite = left.Times(10);

            left.Update(DateTime.Today, 1m);
            Assert.AreEqual(10m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(20m, composite.Current.Value);

        }

        [Test]
        public void WorksForIndicatorsOfDifferentTypes()
        {
            var indicatorA1 = new TestIndicatorA("1");
            var indicatorA2 = new TestIndicatorA("2");

            indicatorA1.Over(indicatorA2);
            indicatorA1.Minus(indicatorA2);
            indicatorA1.Times(indicatorA2);
            indicatorA1.Plus(indicatorA2);
            indicatorA1.Of(indicatorA2);

            var indicatorB1 = new TestIndicatorB("1");
            var indicatorB2 = new TestIndicatorB("2");
            indicatorB1.Over(indicatorB2);
            indicatorB1.Minus(indicatorB2);
            indicatorB1.Times(indicatorB2);
            indicatorB1.Plus(indicatorB2);
            indicatorB1.Of(indicatorB2);
        }

        [Test]
        public void MinusSubtractsLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator) IndicatorExtensions.Minus(left.ToPython(), 10);

                left.Update(DateTime.Today, 1);
                Assert.AreEqual(-9, composite.Current.Value);

                left.Update(DateTime.Today, 2);
                Assert.AreEqual(-8, composite.Current.Value);
            }
        }

        [Test]
        public void PlusAddsLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Plus(left.ToPython(), 10);

                left.Update(DateTime.Today, 1);
                Assert.AreEqual(11, composite.Current.Value);

                left.Update(DateTime.Today, 2);
                Assert.AreEqual(12, composite.Current.Value);
            }
        }

        [Test]
        public void OverDivdesLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Over(left.ToPython(), 5);

                left.Update(DateTime.Today, 10);
                Assert.AreEqual(2, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(4, composite.Current.Value);
            }
        }

        [Test]
        public void TimesMultipliesLeftAndConstant_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var composite = (IIndicator)IndicatorExtensions.Times(left.ToPython(), 5);

                left.Update(DateTime.Today, 10);
                Assert.AreEqual(50, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(100, composite.Current.Value);
            }
        }

        [Test]
        public void TimesMultipliesLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Times(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(100, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(100, composite.Current.Value);
            }
        }

        [Test]
        public void OverDividesLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Over(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(1, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(1, composite.Current.Value);
            }
        }

        [Test]
        public void PlusAddsLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Plus(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(20, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(20, composite.Current.Value);
            }
        }

        [Test]
        public void MinusSubstractsLeftAndRight_Py()
        {
            using (Py.GIL())
            {
                var left = new Identity("left");
                var right = new Identity("right");
                var composite = (IIndicator)IndicatorExtensions.Minus(left.ToPython(), right.ToPython());

                left.Update(DateTime.Today, 10);
                right.Update(DateTime.Today, 10);
                Assert.AreEqual(0, composite.Current.Value);

                left.Update(DateTime.Today, 20);
                Assert.AreEqual(0, composite.Current.Value);
            }
        }

        private class TestIndicatorA : IndicatorBase<IBaseData>
        {
            public TestIndicatorA(string name) : base(name)
            {
            }
            public override bool IsReady { get; }
            protected override decimal ComputeNextValue(IBaseData input)
            {
                throw new NotImplementedException();
            }
        }

        private class TestIndicatorB : IndicatorBase<IndicatorDataPoint>
        {
            public TestIndicatorB(string name) : base(name)
            {
            }
            public override bool IsReady
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
            protected override decimal ComputeNextValue(IndicatorDataPoint input)
            {
                throw new NotImplementedException();
            }
        }
    }
}
