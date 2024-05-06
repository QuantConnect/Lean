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
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class PythonIndicatorTestsSnakeCase : PythonIndicatorTests
    {
        protected override bool SnakeCase => true;
    }

    [TestFixture]
    public class PythonIndicatorTests : CommonIndicatorTests<IBaseData>
    {
        [SetUp]
        public void SetUp()
        {
            SymbolCache.Clear();
        }

        protected virtual bool SnakeCase => false;

        private PyObject CreatePythonIndicator(int period = 14)
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    $@"
from AlgorithmImports import *
from collections import deque

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        self.{(SnakeCase ? "name" : "Name")} = name
        self.{(SnakeCase ? "value" : "Value")} = 0
        self.{(SnakeCase ? "period" : "Period")} = period
        self.{(SnakeCase ? "warm_up_period" : "WarmUpPeriod")} = period
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def {(SnakeCase ? "update" : "Update")}(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.{(SnakeCase ? "value" : "Value")} = np.sum(self.queue) / count
        return count == self.queue.maxlen
"
                );
                var indicator = module.GetAttr("CustomSimpleMovingAverage")
                    .Invoke("custom".ToPython(), period.ToPython());

                return indicator;
            }
        }

        protected override IndicatorBase<IBaseData> CreateIndicator()
        {
            return new PythonIndicator(CreatePythonIndicator());
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
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    @"
from AlgorithmImports import *
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

                var actual = algorithm.SubscriptionManager.Subscriptions
                    .FirstOrDefault(config => config.TickType == TickType.Trade)
                    .Consolidators.Count;
                Assert.AreEqual(1, actual);

                var badIndicator = module.GetAttr("BadCustomIndicator").Invoke();
                Assert.Throws<NotImplementedException>(() => algorithm.RegisterIndicator(spy, badIndicator, Resolution.Minute));
            }
        }

        [Test]
        public void AllPythonRegisterIndicatorCases()
        {
            //This test covers all three cases of registering a indicator through Python

            //Setup algorithm and Equity
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spy = algorithm.AddEquity("SPY").Symbol;

            //Setup Python Indicator and Consolidator
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "consolidator = QuoteBarConsolidator(timedelta(days = 5)) \n" +
                    "timeDelta = timedelta(days=2)\n" +
                    "class CustomIndicator(PythonIndicator):\n" +
                    "   def __init__(self):\n" +
                    "       self.value = 0\n" +
                    "   def update(self, input):\n" +
                    "       self.value = input.value\n" +
                    "       return True\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       pass\n" +
                    "   def scan(self, time):\n" +
                    "       pass\n"
                );

                //Get our variables from Python
                var PyIndicator = module.GetAttr("CustomIndicator").Invoke();
                var PyConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var Consolidator = module.GetAttr("consolidator");
                algorithm.SubscriptionManager.AddConsolidator(spy, Consolidator);
                var TimeDelta = module.GetAttr("timeDelta");

                //Test 1: Using a C# Consolidator; Should convert consolidator into IDataConsolidator
                Assert.DoesNotThrow(() => algorithm.RegisterIndicator(spy, PyIndicator, Consolidator));

                //Test 2: Using a Python Consolidator; Should wrap consolidator
                Assert.DoesNotThrow(() => algorithm.RegisterIndicator(spy, PyIndicator, PyConsolidator));

                //Test 3: Using a timedelta object; Should convert timedelta to timespan
                Assert.DoesNotThrow(() => algorithm.RegisterIndicator(spy, PyIndicator, TimeDelta));
            }
        }

        //Test 1: Using a C# Consolidator; Should convert consolidator into IDataConsolidator and fail because of the InputType
        [TestCase("consolidator", false, "Type mismatch found between consolidator and symbol. Symbol: SPY does not support input type: QuoteBar. Supported types: TradeBar.")]
        //Test 2: Using a Python Consolidator; Should wrap consolidator and fail because of the InputType
        [TestCase("CustomConsolidator", true, "Type mismatch found between consolidator and symbol. Symbol: SPY does not support input type: QuoteBar. Supported types: TradeBar.")]
        //Test 3: Using an invalid consolidator; Should try to convert into C#, Python Consolidator and timedelta and fail as the type is invalid
        [TestCase("InvalidConsolidator", true, "Invalid third argument, should be either a valid consolidator or timedelta object. The following exception was thrown: ")]
        public void AllPythonRegisterIndicatorBadCases(string consolidatorName, bool needsInvoke, string expectedMessage)
        {
            //This test covers all three bad cases of registering a indicator through Python

            //Setup algorithm and Equity
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            algorithm.AddData<TradeBar>("SPY", Resolution.Daily);
            var spy = "SPY";

            //Setup Python Indicator and Consolidator
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "consolidator = QuoteBarConsolidator(timedelta(days = 5)) \n" +
                    "class CustomIndicator(PythonIndicator):\n" +
                    "   def __init__(self):\n" +
                    "       self.value = 0\n" +
                    "   def update(self, input):\n" +
                    "       self.value = input.value\n" +
                    "       return True\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       pass\n" +
                    "   def scan(self, time):\n" +
                    "       pass\n" +
                    "class InvalidConsolidator:\n" +
                    "   pass\n"
                );

                //Get our variables from Python
                var PyIndicator = module.GetAttr("CustomIndicator").Invoke();
                var Consolidator = module.GetAttr(consolidatorName);
                if (needsInvoke)
                {
                    Consolidator = Consolidator.Invoke();
                }

                var exception = Assert.Throws<ArgumentException>(() => algorithm.RegisterIndicator(spy, PyIndicator, Consolidator));
                Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            }
        }

        [Test]
        public void WarmsUpProperlyPythonIndicator()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    @"
from AlgorithmImports import *
from collections import deque

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)
        self.WarmUpPeriod = period

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
"
                );
                var pythonIndicator = module.GetAttr("CustomSimpleMovingAverage")
                    .Invoke("custom".ToPython(), 14.ToPython());
                var SMAWithWarmUpPeriod = new PythonIndicator(pythonIndicator);
                var reference = new DateTime(2000, 1, 1, 0, 0, 0);
                var period = ((IIndicatorWarmUpPeriodProvider)SMAWithWarmUpPeriod).WarmUpPeriod;

                // Check the WarmUpPeriod parameter is the one defined in the constructor of the custom indicator
                Assert.AreEqual(14, period);

                for (var i = 0; i < period; i++)
                {
                    SMAWithWarmUpPeriod.Update(new TradeBar() { Symbol = Symbols.AAPL, Low = 1, High = 2, Volume = 100, Time = reference.AddDays(1 + i) });
                    Assert.AreEqual(i == period - 1, SMAWithWarmUpPeriod.IsReady);
                }
            }
        }

        [Test]
        public void SetDefaultWarmUpPeriodProperly()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(
                    Guid.NewGuid().ToString(),
                    @"
from AlgorithmImports import *
from collections import deque

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        self.Name = name
        self.Value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def Update(self, input):
        self.queue.appendleft(input.Value)
        count = len(self.queue)
        self.Value = np.sum(self.queue) / count
        return count == self.queue.maxlen
"
                );
                var pythonIndicator = module.GetAttr("CustomSimpleMovingAverage")
                    .Invoke("custom".ToPython(), 14.ToPython());
                var indicator = new PythonIndicator(pythonIndicator);

                Assert.AreEqual(0, indicator.WarmUpPeriod);
            }
        }

        [Test]
        public void PythonIndicatorDoesntRequireWrappingToWork()
        {
            var data = new[] { 1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m, 2468m, 13579m };
            var seen = new List<decimal>();
            var start = new DateTime(2022, 11, 15);
            var period = 4;

            using (Py.GIL())
            {
                using dynamic customSma = CreatePythonIndicator(period);
                var wrapper = new PythonIndicator(customSma);

                for (int i = 0; i < data.Length; i++)
                {
                    var datum = data[i];
                    seen.Add(datum);

                    wrapper.Update(new IndicatorDataPoint(start.AddSeconds(i), datum));

                    var value = SnakeCase ? (decimal)customSma.value : (decimal)customSma.Value;

                    Assert.AreEqual(Enumerable.Reverse(seen).Take(period).Average(), value);
                }
            }
        }

        [Test]
        public void IndicatorExtensionsWorkForPythonIndicators()
        {
            var data = new[] { 1m, 10m, 100m, 1000m, 10000m, 1234m, 56789m, 2468m, 13579m };
            var seen = new List<decimal>();
            var start = new DateTime(2022, 11, 15);

            var period = 4;
            var sma = new SimpleMovingAverage(period);

            using (Py.GIL())
            {
                using dynamic customSma = CreatePythonIndicator(period);
                IndicatorExtensions.Of(customSma, sma.ToPython());

                for (int i = 0; i < data.Length; i++)
                {
                    var datum = data[i];

                    sma.Update(new IndicatorDataPoint(start.AddSeconds(i), datum));

                    if (i < 2 * period - 2)
                    {
                        Assert.IsFalse((bool)customSma.IsReady);
                    }
                    else
                    {
                        Assert.IsTrue((bool)customSma.IsReady);
                    }

                    var value = SnakeCase ? (decimal)customSma.value : (decimal)customSma.Value;
                    if (i < period - 1)
                    {
                        Assert.AreEqual(0m, value);
                    }
                    else
                    {
                        seen.Add(sma.Current.Value);
                        Assert.AreEqual(Enumerable.Reverse(seen).Take(period).Average(), value);
                    }
                }
            }
        }

        [Test]
        public void PythonIndicatorExtensionInRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(
                "CustomIndicatorWithExtensionAlgorithm",
                new (),
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        /// <summary>
        /// The external test file of this indicator does not define market data. Therefore
        /// we skip the test
        /// </summary>
        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
        }

        /// <summary>
        /// The external test file of this indicator does not define market data. Therefore
        /// we skip the test
        /// </summary>
        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
        }
    }
}
