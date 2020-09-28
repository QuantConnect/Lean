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
using QuantConnect.Data.Market;
using QuantConnect.Python;

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
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect import *\n" +
                    "from QuantConnect.Data.Market import *\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.UpdateWasCalled = False\n" +
                    "       self.InputType = QuoteBar\n" +
                    "       self.OutputType = QuoteBar\n" +
                    "       self.Consolidated = None\n" +
                    "       self.WorkingData = None\n" +
                    "   def Update(self, data):\n" +
                    "       self.UpdateWasCalled = True\n");

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
                customConsolidator.GetAttr("UpdateWasCalled").TryConvert(out called);
                Assert.True(called);
            }
        }

        [Test]
        public void ScanPyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect import *\n" +
                    "from QuantConnect.Data.Market import *\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.ScanWasCalled = False\n" +
                    "       self.InputType = QuoteBar\n" +
                    "       self.OutputType = QuoteBar\n" +
                    "       self.Consolidated = None\n" +
                    "       self.WorkingData = None\n" +
                    "   def Scan(self,time):\n" +
                    "       self.ScanWasCalled = True\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);

                wrapper.Scan(DateTime.Now);

                bool called;
                customConsolidator.GetAttr("ScanWasCalled").TryConvert(out called);
                Assert.True(called);
            }
        }

        [Test]
        public void InputTypePyConsolidator()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect import *\n" +
                    "from QuantConnect.Data.Market import *\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.InputType = QuoteBar\n" +
                    "       self.OutputType = QuoteBar\n" +
                    "       self.Consolidated = None\n" +
                    "       self.WorkingData = None\n");

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
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect import *\n" +
                    "from QuantConnect.Data.Market import *\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.InputType = QuoteBar\n" +
                    "       self.OutputType = QuoteBar\n" +
                    "       self.Consolidated = None\n" +
                    "       self.WorkingData = None\n");

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
                    {"Total Trades", "32"},
                    {"Average Win", "0.42%"},
                    {"Average Loss", "-0.02%"},
                    {"Compounding Annual Return", "66.060%"},
                    {"Drawdown", "0.300%"},
                    {"Expectancy", "2.979"},
                    {"Net Profit", "1.071%"},
                    {"Sharpe Ratio", "8.939"},
                    {"Probabilistic Sharpe Ratio", "88.793%"},
                    {"Loss Rate", "81%"},
                    {"Win Rate", "19%"},
                    {"Profit-Loss Ratio", "20.22"},
                    {"Alpha", "0.528"},
                    {"Beta", "0.35"},
                    {"Annual Standard Deviation", "0.08"},
                    {"Annual Variance", "0.006"},
                    {"Information Ratio", "1.287"},
                    {"Tracking Error", "0.141"},
                    {"Treynor Ratio", "2.045"},
                    {"Total Fees", "$51.40"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }

        [Test]
        public void AttachAndTriggerEvent()
        {
            using (Py.GIL())
            {
                var module = PythonEngine.ModuleFromString(Guid.NewGuid().ToString(),
                    "from clr import AddReference\n" +
                    "AddReference(\"QuantConnect.Common\")\n" +
                    "from QuantConnect import *\n" +
                    "from QuantConnect.Data.Consolidators import *\n" +
                    "from datetime import *\n" +
                    "class ImplementingClass():\n" +
                    "   def __init__(self):\n" +
                    "       self.EventCalled = False\n" +
                    "       self.Consolidator = CustomConsolidator(timedelta(minutes=1))\n" +
                    "       self.Consolidator.DataConsolidated += self.ConsolidatorEvent\n" +
                    "   def ConsolidatorEvent(self, sender, bar):\n" +
                    "       self.EventCalled = True\n" +
                    "class CustomConsolidator(QuoteBarConsolidator):\n" +
                    "   def __init__(self,span):\n" +
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
                wrapper.Scan(time.AddMinutes(1));
                implementingClass.GetAttr("EventCalled").TryConvert(out called);
                Assert.True(called);
            }
        }
    }
}