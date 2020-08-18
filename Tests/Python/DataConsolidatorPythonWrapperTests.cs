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
                    "from QuantConnect.Data.Consolidators import *\n" +
                    "from datetime import timedelta\n" +
                    "class CustomConsolidator(QuoteBarConsolidator):\n" +
                    "   def __init__(self,timespan):\n" +
                    "       super().__init__(timespan)\n" +
                    "       self.UpdateWasCalled = False\n" +
                    "   def Update(self, data):\n" +
                    "       self.UpdateWasCalled = True\n" +
                    "timeVar = timedelta(days=1)");

                var timeVar = module.GetAttr("timeVar");
                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke(timeVar);
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
                    "from QuantConnect.Data.Market import *\n" +
                    "from QuantConnect.Data.Consolidators import *\n" +
                    "from datetime import timedelta\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.ScanWasCalled = False\n" +    
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
                    "from QuantConnect.Data.Market import *\n" +
                    "from QuantConnect.Data.Consolidators import *\n" +
                    "from datetime import timedelta\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.InputType = QuoteBar\n");

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
                    "from QuantConnect.Data.Market import *\n" +
                    "from QuantConnect.Data.Consolidators import *\n" +
                    "from datetime import timedelta\n" +
                    "class CustomConsolidator():\n" +
                    "   def __init__(self):\n" +
                    "       self.OutputType = QuoteBar\n");

                var customConsolidator = module.GetAttr("CustomConsolidator").Invoke();
                var wrapper = new DataConsolidatorPythonWrapper(customConsolidator);

                var time = DateTime.Today;
                var period = TimeSpan.FromMinutes(1);

                var type = wrapper.OutputType;
                Assert.True(type == typeof(QuoteBar));
            }
        }

        [Test, Category("TravisExclude")]
        public void RunRegressionAlgorithm()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters("CustomConsolidatorRegressionAlgorithm",
                new Dictionary<string, string> {
                    {"Total Trades", "45"},
                    {"Average Win", "0.45%"},
                    {"Average Loss", "-0.03%"},
                    {"Compounding Annual Return", "7.964%"},
                    {"Drawdown", "0.900%"},
                    {"Expectancy", "-0.335"},
                    {"Net Profit", "0.161%"},
                    {"Sharpe Ratio", "1.383"},
                    {"Probabilistic Sharpe Ratio", "51.675%"},
                    {"Loss Rate", "95%"},
                    {"Win Rate", "5%"},
                    {"Profit-Loss Ratio", "13.63"},
                    {"Alpha", "-0.039"},
                    {"Beta", "0.234"},
                    {"Annual Standard Deviation", "0.062"},
                    {"Annual Variance", "0.004"},
                    {"Information Ratio", "-2.698"},
                    {"Tracking Error", "0.166"},
                    {"Treynor Ratio", "0.368"},
                    {"Total Fees", "$73.35"}
                },
                Language.Python,
                AlgorithmStatus.Completed);

            AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);
        }
    }
}