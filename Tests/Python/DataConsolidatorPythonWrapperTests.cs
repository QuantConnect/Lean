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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using QuantConnect.Algorithm;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class DataConsolidatorPythonWrapperTests
    {
        [Test]
        public void UpdatePyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.update_was_called = False\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       self.update_was_called = True\n" +
                    "   def scan(self, time):\n" +
                    "       pass\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);
                var bar1 = new QuoteBar
                {
                    Time = time,
                    Symbol = Symbols.SPY,
                    Bid = new Bar(1, 2, 0.75m, 1.25m),
                    LastBidSize = 3,
                    Ask = null,
                    LastAskSize = 0,
                    Value = 1,
                    Period = period
                };

                wrapper.Update(bar1);

                bool called;
                customConsolidator.GetAttr("update_was_called").TryConvert(out called);
                Assert.True(called);
            }
        }

        [Test]
        public void ScanPyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.scan_was_called = False\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       pass\n" +
                    "   def scan(self, time):\n" +
                    "       self.scan_was_called = True\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);

                wrapper.Scan(DateTime.Now);

                bool called;
                customConsolidator.GetAttr("scan_was_called").TryConvert(out called);
                Assert.True(called);
            }
        }

        [Test]
        public void InputTypePyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       pass\n" +
                    "   def scan(self, time):\n" +
                    "       pass\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);

                var type = wrapper.InputType;
                Assert.True(type == typeof(QuoteBar));
            }
        }

        [Test]
        public void OutputTypePyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "class CustomConsolidator(PythonConsolidator):\n" +
                    "   def __init__(self):\n" +
                    "       self.input_type = QuoteBar\n" +
                    "       self.output_type = QuoteBar\n" +
                    "       self.consolidated = None\n" +
                    "       self.working_data = None\n" +
                    "   def update(self, data):\n" +
                    "       pass\n" +
                    "   def scan(self, time):\n" +
                    "       pass\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);

                var type = wrapper.OutputType;
                Assert.True(type == typeof(QuoteBar));
            }
        }

        [Test]
        public void RunRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("CustomConsolidatorRegressionAlgorithm",
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "15"},
                    {"Average Win", "0.42%"},
                    {"Average Loss", "-0.03%"},
                    {"Compounding Annual Return", "76.673%"},
                    {"Drawdown", "0.200%"},
                    {"Expectancy", "4.239"},
                    {"Net Profit", "1.203%"},
                    {"Sharpe Ratio", "7.908"},
                    {"Probabilistic Sharpe Ratio", "95.063%"},
                    {"Loss Rate", "62%"},
                    {"Win Rate", "38%"},
                    {"Profit-Loss Ratio", "12.97"},
                    {"Alpha", "0.408"},
                    {"Beta", "0.35"},
                    {"Annual Standard Deviation", "0.067"},
                    {"Annual Variance", "0.005"},
                    {"Information Ratio", "1.484"},
                    {"Tracking Error", "0.117"},
                    {"Treynor Ratio", "1.526"},
                    {"Total Fees", "$24.34"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        [Test]
        public void AttachAndTriggerEvent()
        {
            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "class ImplementingClass():\n" +
                    "   def __init__(self):\n" +
                    "       self.EventCalled = False\n" +
                    "       self.Consolidator = CustomConsolidator(timedelta(minutes=2))\n" +
                    "       self.Consolidator.DataConsolidated += self.ConsolidatorEvent\n" +
                    "   def ConsolidatorEvent(self, sender, bar):\n" +
                    "       self.EventCalled = True\n" +
                    "class CustomConsolidator(QuoteBarConsolidator):\n" +
                    "   def __init__(self,span):\n" +
                    "       super().__init__(span)\n" +
                    "       self.Span = span");

                var implementingClass = module.GetAttr("ImplementingClass").Invoke();
                var customConsolidator = implementingClass.GetAttr("Consolidator");
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                bool called;
                implementingClass.GetAttr("EventCalled").TryConvert(out called);
                Assert.False(called);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);
                var bar1 = new QuoteBar
                {
                    Time = time,
                    Symbol = Symbols.SPY,
                    Bid = new Bar(1, 2, 0.75m, 1.25m),
                    LastBidSize = 3,
                    Ask = null,
                    LastAskSize = 0,
                    Value = 1,
                    Period = period
                };

                wrapper.Update(bar1);
                wrapper.Scan(time.AddMinutes(2));
                implementingClass.GetAttr("EventCalled").TryConvert(out called);
                Assert.True(called);
            }
        }

        [Test]
        public void SubscriptionManagedDoesNotWrapCSharpConsolidators()
        {
            //Setup algorithm and Equity
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var spy = algorithm.AddEquity("SPY").Symbol;

            using (Py.GIL())
            {
                var module = PyModule.FromString(Guid.NewGuid().ToString(),
                    "from AlgorithmImports import *\n" +
                    "consolidator = QuoteBarConsolidator(timedelta(5))");

                var pyConsolidator = module.GetAttr("consolidator");

                algorithm.SubscriptionManager.AddConsolidator(spy, pyConsolidator);

                pyConsolidator.TryConvert(out IDataConsolidator consolidator);
                algorithm.SubscriptionManager.RemoveConsolidator(spy, consolidator);

                var count = algorithm.SubscriptionManager
                    .SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(spy)
                    .Sum(x => x.Consolidators.Count);

                Assert.AreEqual(0, count);
            }

        }

    }
}
