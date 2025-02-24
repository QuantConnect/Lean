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
using Python.Runtime;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class CompositeIndicatorTests
    {
        [Test]
        public void CompositeIsReadyWhenBothAre()
        {
            var left = new Delay(1);
            var right = new Delay(2);
            var composite = CreateCompositeIndicator(left, right, (l, r) => l.Current.Value + r.Current.Value);

            left.Update(DateTime.Today.AddSeconds(0), 1m);
            right.Update(DateTime.Today.AddSeconds(0), 1m);
            Assert.IsFalse(composite.IsReady);
            Assert.IsFalse(composite.Left.IsReady);
            Assert.IsFalse(composite.Right.IsReady);

            left.Update(DateTime.Today.AddSeconds(1), 2m);
            right.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsFalse(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsFalse(composite.Right.IsReady);

            left.Update(DateTime.Today.AddSeconds(2), 3m);
            right.Update(DateTime.Today.AddSeconds(2), 3m);
            Assert.IsTrue(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsTrue(composite.Right.IsReady);

            left.Update(DateTime.Today.AddSeconds(3), 4m);
            right.Update(DateTime.Today.AddSeconds(3), 4m);
            Assert.IsTrue(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsTrue(composite.Right.IsReady);
        }

        [Test]
        public void CallsDelegateCorrectly()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = CreateCompositeIndicator(left, right, (l, r) =>
            {
                Assert.AreEqual(left, l);
                Assert.AreEqual(right, r);
                return l.Current.Value + r.Current.Value;
            });

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(2m, composite.Current.Value);
        }

        [Test]
        public virtual void ResetsProperly()
        {
            var left = new Maximum("left", 2);
            var right = new Minimum("right", 2);
            var composite = CreateCompositeIndicator(left, right, (l, r) => l.Current.Value + r.Current.Value);

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
        }

        [TestCase("sum", 5, 10, 15, false)]
        [TestCase("min", -12, 52, -12, false)]
        [TestCase("sum", 5, 10, 15, true)]
        [TestCase("min", -12, 52, -12, true)]
        public virtual void PythonCompositeIndicatorConstructorValidatesBehavior(string operation, decimal leftValue, decimal rightValue, decimal expectedValue, bool usePythonIndicator)
        {
            var left = new SimpleMovingAverage("SMA", 10);
            var right = new SimpleMovingAverage("SMA", 10);
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                    @"
from AlgorithmImports import *
from QuantConnect.Indicators import *

def create_composite_indicator(left, right, operation):
    if operation == 'sum':
        def composer(l, r):
            return IndicatorResult(l.current.value + r.current.value)
    elif operation == 'min':
        def composer(l, r):
            return IndicatorResult(min(l.current.value, r.current.value))
    return CompositeIndicator(left, right, composer)

def update_indicators(left, right, value_left, value_right):
    left.update(IndicatorDataPoint(DateTime.Now, value_left))
    right.update(IndicatorDataPoint(DateTime.Now, value_right))
            ");

                using var createCompositeIndicator = testModule.GetAttr("create_composite_indicator");
                using var updateIndicators = testModule.GetAttr("update_indicators");

                using var leftPy = usePythonIndicator ? CreatePyObjectIndicator(10) : left.ToPython();
                using var rightPy = usePythonIndicator ? CreatePyObjectIndicator(10) : right.ToPython();

                // Create composite indicator using Python logic
                using var composite = createCompositeIndicator.Invoke(leftPy, rightPy, operation.ToPython());

                // Update the indicator with sample values (left, right)
                updateIndicators.Invoke(leftPy, rightPy, leftValue.ToPython(), rightValue.ToPython());

                // Verify composite indicator name and properties
                using var name = composite.GetAttr("Name");
                Assert.AreEqual($"COMPOSE({left.Name},{right.Name})", name.ToString());

                // Validate the composite indicator's computed value
                using var value = composite.GetAttr("Current").GetAttr("Value");
                Assert.AreEqual(expectedValue, value.As<decimal>());
            }
        }

        private static PyObject CreatePyObjectIndicator(int period)
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    "custom_indicator",
                    @"
from AlgorithmImports import *
from collections import deque

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, period):
        self.name = 'SMA'
        self.value = 0
        self.period = period
        self.warm_up_period = period
        self.queue = deque(maxlen=period)
        self.current = IndicatorDataPoint(DateTime.Now, self.value)

    def update(self, input):
        self.queue.appendleft(input.value)
        count = len(self.queue)
        self.value = sum(self.queue) / count
        self.current = IndicatorDataPoint(input.time, self.value)
        self.on_updated(IndicatorDataPoint(DateTime.Now, input.value))
"
                );

                var indicator = module.GetAttr("CustomSimpleMovingAverage")
                                  .Invoke(period.ToPython());

                return indicator;
            }
        }

        protected virtual CompositeIndicator CreateCompositeIndicator(IndicatorBase left, IndicatorBase right, QuantConnect.Indicators.CompositeIndicator.IndicatorComposer composer)
        {
            return new CompositeIndicator(left, right, composer);
        }
    }
}
