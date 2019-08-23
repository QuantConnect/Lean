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
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PythonIndicatorTests : CommonIndicatorTests<IBaseData>
    {
        protected override IndicatorBase<IBaseData> CreateIndicator()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(
                    Guid.NewGuid().ToString(),
                    @"
from clr import AddReference
AddReference('QuantConnect.Indicators')

from QuantConnect.Indicators import PythonIndicator
from collections import deque
from datetime import datetime, timedelta
from numpy import sum

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = sum(self.queue) / count
        return count == self.queue.maxlen
"
                );
                var indicator = module.GetAttr("CustomSimpleMovingAverage")
                    .Invoke("custom".ToPython(), 14.ToPython());

                return new PythonIndicator(indicator);
            }
        }

        protected override string TestFileName => "spy_with_indicators.txt";

        protected override string TestColumnName => "SMA14";

        protected override void RunTestIndicator(IndicatorBase<IBaseData> indicator)
        {
            var first = true;
            var closeIndex = -1;
            var targetIndex = -1;
            foreach (var line in File.ReadLines(Path.Combine("TestData", TestFileName)))
            {
                var parts = line.Split(new[] { ',' }, StringSplitOptions.None);

                if (first)
                {
                    first = false;
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Trim() == "Close")
                        {
                            closeIndex = i;
                        }
                        if (parts[i].Trim() == TestColumnName)
                        {
                            targetIndex = i;
                        }
                    }
                    if (closeIndex * targetIndex < 0)
                    {
                        Assert.Fail($"Didn't find one of 'Close' or '{line}' in the header: ", TestColumnName);
                    }

                    continue;
                }

                var close = decimal.Parse(parts[closeIndex], CultureInfo.InvariantCulture);
                var date = Time.ParseDate(parts[0]);

                var data = new IndicatorDataPoint(date, close);
                indicator.Update(data);

                if (!indicator.IsReady || parts[targetIndex].Trim() == string.Empty)
                {
                    continue;
                }

                var expected = double.Parse(parts[targetIndex], CultureInfo.InvariantCulture);
                Assertion.Invoke(indicator, expected);
            }
        }

        protected override Action<IndicatorBase<IBaseData>, double> Assertion => (indicator, expected) =>
            Assert.AreEqual(expected, (double) indicator.Current.Value, 1e-2);

        [Test]
        public void SmaComputesCorrectly()
        {
            var sma = new SimpleMovingAverage(4);
            var data = new[] {1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m};

            var seen = new List<decimal>();
            for (int i = 0; i < data.Length; i++)
            {
                var datum = data[i];
                seen.Add(datum);
                sma.Update(new IndicatorDataPoint(DateTime.Now.AddSeconds(i), datum));
                Assert.AreEqual(Enumerable.Reverse(seen).Take(sma.Period).Average(), sma.Current.Value);
            }
        }

        [Test]
        public void IsReadyAfterPeriodUpdates()
        {
            var sma = new SimpleMovingAverage(3);

            sma.Update(DateTime.UtcNow, 1m);
            sma.Update(DateTime.UtcNow, 1m);
            Assert.IsFalse(sma.IsReady);
            sma.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(sma.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var sma = new SimpleMovingAverage(3);

            foreach (var data in TestHelper.GetDataStream(4))
            {
                sma.Update(data);
            }
            Assert.IsTrue(sma.IsReady);
            
            sma.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(sma);
            TestHelper.AssertIndicatorIsInDefaultState(sma.RollingSum);
            sma.Update(DateTime.UtcNow, 2.0m);
            Assert.AreEqual(sma.Current.Value, 2.0m);
        }

        [Test]
        public void RegisterPythonCustomIndicatorProperly()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spy = algorithm.AddEquity("SPY").Symbol;

            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(
                    Guid.NewGuid().ToString(),
                    @"
from clr import AddReference
AddReference('QuantConnect.Indicators')
from QuantConnect.Indicators import PythonIndicator
class GoodCustomIndicator(PythonIndicator):
    def __init__(self):
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True
class BadCustomIndicator(PythonIndicator):
    def __init__(self):
        self.Valeu = 0
    def Update(self, input):
        self.Value = input.Value
        return True"
                );

                var goodIndicator = module.GetAttr("GoodCustomIndicator").Invoke();
                Assert.DoesNotThrow(() => algorithm.RegisterIndicator(spy, goodIndicator, Resolution.Minute));

                var actual = algorithm.SubscriptionManager.Subscriptions.FirstOrDefault().Consolidators.Count;
                Assert.AreEqual(1, actual);

                var badIndicator = module.GetAttr("BadCustomIndicator").Invoke();
                Assert.Throws<NotImplementedException>(() => algorithm.RegisterIndicator(spy, badIndicator, Resolution.Minute));
            }
        }
    }
}