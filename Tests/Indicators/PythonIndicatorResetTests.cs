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
    public class PythonIndicatorResetSnakeCaseTests : PythonIndicatorResetTests
    {
        protected override bool SnakeCase => true;
    }

    /// <summary>
    /// Reset cases that build their own python class, so they do not use the indicator
    /// the surrounding fixtures create. Kept separate from <see cref="PythonIndicatorTests"/>
    /// for that reason: inheriting them there would run each case in four more fixtures
    /// without changing anything it exercises.
    /// </summary>
    [TestFixture]
    public class PythonIndicatorResetTests
    {
        protected virtual bool SnakeCase => false;

        private const int RecursionCap = 200;

        private static PythonIndicator CreateIndicatorFrom(string source, string className)
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(), source);
                var instance = module.GetAttr(className).Invoke();

                return new PythonIndicator(instance);
            }
        }

        [Test]
        public void ResetIsNotReenteredByASubclassCallingSuper()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    $@"
from AlgorithmImports import *
from collections import deque

class RecursiveReset(PythonIndicator):
    depth = 0
    max_depth = 0

    def __init__(self):
        self.{(SnakeCase ? "name" : "Name")} = 'recursive'
        self.{(SnakeCase ? "value" : "Value")} = 0
        self.queue = deque(maxlen=3)

    def {(SnakeCase ? "update" : "Update")}(self, input):
        self.queue.appendleft(input.Value)
        self.{(SnakeCase ? "value" : "Value")} = sum(self.queue) / len(self.queue)
        return len(self.queue) == self.queue.maxlen

    def {(SnakeCase ? "reset" : "Reset")}(self):
        cls = type(self)
        cls.depth += 1
        cls.max_depth = max(cls.max_depth, cls.depth)
        if cls.depth < {RecursionCap}:
            super().{(SnakeCase ? "reset" : "Reset")}()
        cls.depth -= 1
        self.queue.clear()
"
                );

                var pythonIndicator = module.GetAttr("RecursiveReset").Invoke();

                // An inheriting class converts to its own CSharp part, so SetIndicator points the
                // wrapper at this same object. This is what WrapPythonIndicator does on registration.
                pythonIndicator.TryConvert(out PythonIndicator indicator);
                Assert.IsNotNull(indicator);
                indicator.SetIndicator(pythonIndicator);

                indicator.Update(new IndicatorDataPoint(new DateTime(2024, 1, 1), 100m));
                indicator.Reset();

                // Zero would mean the python reset was never reached, the cap would mean it recursed.
                var depth = module.GetAttr("RecursiveReset").GetAttr("max_depth").As<int>();
                Assert.AreEqual(1, depth, $"python reset() ran {depth} deep");
                Assert.AreEqual(0, indicator.Samples);
                Assert.AreEqual(0, pythonIndicator.GetAttr("queue").Length());
            }
        }

        [Test]
        public void ResetSkipsANonMethodResetAttribute()
        {
            using (Py.GIL())
            {
                var indicator = CreateIndicatorFrom($@"
class PlainResetAttribute():
    def __init__(self):
        self.{(SnakeCase ? "name" : "Name")} = 'plain'
        self.{(SnakeCase ? "value" : "Value")} = 0
        self.{(SnakeCase ? "is_ready" : "IsReady")} = False
        self.{(SnakeCase ? "reset" : "Reset")} = False

    def {(SnakeCase ? "update" : "Update")}(self, input):
        self.{(SnakeCase ? "value" : "Value")} = input.Value
        self.{(SnakeCase ? "is_ready" : "IsReady")} = True
        return True
", "PlainResetAttribute");

                indicator.Update(new IndicatorDataPoint(new DateTime(2024, 1, 1), 100m));

                Assert.DoesNotThrow(() => indicator.Reset());
                Assert.AreEqual(0, indicator.Samples);
            }
        }

        [Test]
        public void ResetClearsTheCSharpStateWhenPythonRaises()
        {
            using (Py.GIL())
            {
                var indicator = CreateIndicatorFrom($@"
from AlgorithmImports import *
from collections import deque

class RaisingReset(PythonIndicator):
    def __init__(self):
        self.{(SnakeCase ? "name" : "Name")} = 'raising'
        self.{(SnakeCase ? "value" : "Value")} = 0
        self.queue = deque(maxlen=3)

    def {(SnakeCase ? "update" : "Update")}(self, input):
        self.queue.appendleft(input.Value)
        self.{(SnakeCase ? "value" : "Value")} = sum(self.queue) / len(self.queue)
        return len(self.queue) == self.queue.maxlen

    def {(SnakeCase ? "reset" : "Reset")}(self):
        raise ValueError('boom')
", "RaisingReset");

                indicator.Update(new IndicatorDataPoint(new DateTime(2024, 1, 1), 100m));
                Assert.AreEqual(1, indicator.Samples);

                Assert.Throws<PythonException>(() => indicator.Reset());

                Assert.AreEqual(0, indicator.Samples);
                Assert.IsFalse(indicator.IsReady);
            }
        }
    }
}
