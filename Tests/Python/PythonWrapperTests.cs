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
using QuantConnect.Python;
using System.Collections.Generic;
using QuantConnect.Statistics;
using System.Reflection;

namespace QuantConnect.Tests.Python
{
    public static class PythonWrapperTests
    {
        [TestFixture]
        public class ValidateImplementationOf
        {
            [TestCase(nameof(MissingMethodOne), "ModelMissingMethodOne", "MethodOne")]
            [TestCase(nameof(MissingProperty), "ModelMissingProperty", "PropertyOne")]
            public void ThrowsOnMissingMember(string moduleName, string className, string missingMemberName)
            {
                using (Py.GIL())
                {
                    var moduleStr = GetFieldValue(moduleName);
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), moduleStr);
                    var model = module.GetAttr(className).Invoke();
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws
                        .Exception.InstanceOf<NotImplementedException>().With.Message.Contains(missingMemberName));
                }
            }

            [TestCase(nameof(FullyImplemented), "FullyImplementedModel")]
            [TestCase(nameof(FullyImplementedSnakeCase), "FullyImplementedSnakeCaseModel")]
            [TestCase(nameof(FullyImplementedWithPropertyAsField), "FullyImplementedModelWithPropertyAsField")]
            [TestCase(nameof(DerivedFromCsharp), "DerivedFromCSharpModel")]
            public void DoesNotThrowWhenInterfaceFullyImplemented(string moduleName, string className)
            {
                using (Py.GIL())
                {
                    var moduleStr = GetFieldValue(moduleName);
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), moduleStr);
                    var model = module.GetAttr(className).Invoke();
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws.Nothing);
                }
            }

            [Test]
            public void SettlementModelPythonWrapperWorks()
            {
                var results = AlgorithmRunner.RunLocalBacktest("CustomSettlementModelRegressionAlgorithm",
                    new Dictionary<string, string>()
                    {
                        {PerformanceMetrics.TotalOrders, "0"},
                        {"Average Win", "0%"},
                        {"Average Loss", "0%"},
                        {"Compounding Annual Return", "108.257%"},
                        {"Drawdown", "0%"},
                        {"Expectancy", "0"},
                        {"Net Profit", "1.010%"},
                        {"Sharpe Ratio", "10.983"},
                        {"Sortino Ratio", "0"},
                        {"Probabilistic Sharpe Ratio", "95.977%"},
                        {"Loss Rate", "0%"},
                        {"Win Rate", "0%"},
                        {"Profit-Loss Ratio", "0"},
                        {"Alpha", "1.42"},
                        {"Beta", "-0.273"},
                        {"Annual Standard Deviation", "0.08"},
                        {"Annual Variance", "0.006"},
                        {"Information Ratio", "-3.801"},
                        {"Tracking Error", "0.288"},
                        {"Treynor Ratio", "-3.226"},
                        {"Total Fees", "$0.00"},
                        {"Estimated Strategy Capacity", "$0"},
                        {"Lowest Capacity Asset", ""},
                        {"Portfolio Turnover", "0%"},
                        {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed,
                    algorithmLocation: "../../../Algorithm.Python/CustomSettlementModelRegressionAlgorithm.py"
                );
            }

            [Test]
            public void BenchmarkModelPythonWrapperWorks()
            {
                var results = AlgorithmRunner.RunLocalBacktest("CustomBenchmarkRegressionAlgorithm",
                    new Dictionary<string, string>()
                    {
                        {PerformanceMetrics.TotalOrders, "0"},
                        {"Average Win", "0%"},
                        {"Average Loss", "0%"},
                        {"Compounding Annual Return", "0%"},
                        {"Drawdown", "0%"},
                        {"Expectancy", "0"},
                        {"Net Profit", "0%"},
                        {"Sharpe Ratio", "0"},
                        {"Sortino Ratio", "0"},
                        {"Probabilistic Sharpe Ratio", "0%"},
                        {"Loss Rate", "0%"},
                        {"Win Rate", "0%"},
                        {"Profit-Loss Ratio", "0"},
                        {"Alpha", "0"},
                        {"Beta", "0"},
                        {"Annual Standard Deviation", "0"},
                        {"Annual Variance", "0"},
                        {"Information Ratio", "-1.9190768915765233E+23"},
                        {"Tracking Error", "13.748"},
                        {"Treynor Ratio", "0"},
                        {"Total Fees", "$0.00"},
                        {"Estimated Strategy Capacity", "$0"},
                        {"Lowest Capacity Asset", ""},
                        {"Portfolio Turnover", "0%"},
                        {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed,
                    algorithmLocation: "../../../Algorithm.Python/CustomBenchmarkRegressionAlgorithm.py"
                );
            }

            [Test]
            public void PEP8StyleAlgorithmsImplementationsWork()
            {
                AlgorithmRunner.RunLocalBacktest("PEP8StyleBasicAlgorithm",
                    new Dictionary<string, string>()
                    {
                        {"Total Orders", "1"},
                        {"Average Win", "0%"},
                        {"Average Loss", "0%"},
                        {"Compounding Annual Return", "271.453%"},
                        {"Drawdown", "2.200%"},
                        {"Expectancy", "0"},
                        {"Start Equity", "100000"},
                        {"End Equity", "101691.92"},
                        {"Net Profit", "1.692%"},
                        {"Sharpe Ratio", "8.854"},
                        {"Sortino Ratio", "0"},
                        {"Probabilistic Sharpe Ratio", "67.609%"},
                        {"Loss Rate", "0%"},
                        {"Win Rate", "0%"},
                        {"Profit-Loss Ratio", "0"},
                        {"Alpha", "-0.005"},
                        {"Beta", "0.996"},
                        {"Annual Standard Deviation", "0.222"},
                        {"Annual Variance", "0.049"},
                        {"Information Ratio", "-14.565"},
                        {"Tracking Error", "0.001"},
                        {"Treynor Ratio", "1.97"},
                        {"Total Fees", "$3.44"},
                        {"Estimated Strategy Capacity", "$56000000.00"},
                        {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
                        {"Portfolio Turnover", "19.93%"},
                        {"OrderListHash", "3da9fa60bf95b9ed148b95e02e0cfc9e"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed,
                    algorithmLocation: "../../../Algorithm.Python/PEP8StyleBasicAlgorithm.py"
                );
            }

            [Test]
            public void PEP8StyleCustomModelsWork()
            {
                AlgorithmRunner.RunLocalBacktest("CustomModelsPEP8Algorithm",
                    new Dictionary<string, string>()
                    {
                        {"Total Orders", "63"},
                        {"Average Win", "0.11%"},
                        {"Average Loss", "-0.06%"},
                        {"Compounding Annual Return", "-7.236%"},
                        {"Drawdown", "2.400%"},
                        {"Expectancy", "-0.187"},
                        {"Start Equity", "100000"},
                        {"End Equity", "99370.95"},
                        {"Net Profit", "-0.629%"},
                        {"Sharpe Ratio", "-1.47"},
                        {"Sortino Ratio", "-2.086"},
                        {"Probabilistic Sharpe Ratio", "21.874%"},
                        {"Loss Rate", "70%"},
                        {"Win Rate", "30%"},
                        {"Profit-Loss Ratio", "1.73"},
                        {"Alpha", "-0.102"},
                        {"Beta", "0.122"},
                        {"Annual Standard Deviation", "0.04"},
                        {"Annual Variance", "0.002"},
                        {"Information Ratio", "-4.126"},
                        {"Tracking Error", "0.102"},
                        {"Treynor Ratio", "-0.479"},
                        {"Total Fees", "$62.25"},
                        {"Estimated Strategy Capacity", "$52000000.00"},
                        {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
                        {"Portfolio Turnover", "197.95%"},
                        {"OrderListHash", "709bbf9af9ec6b43a10617dc192a6a5b"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed,
                    algorithmLocation: "../../../Algorithm.Python/CustomModelsPEP8Algorithm.py"
                );
            }

            private static string GetFieldValue(string name)
            {
                return typeof(ValidateImplementationOf).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as string;
            }

            private const string FullyImplemented =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class FullyImplementedModel:
    def MethodOne():
        pass
    def MethodTwo():
        pass
    @property
    def PropertyOne(self):
        return 'value'

";

            private const string FullyImplementedSnakeCase =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class FullyImplementedSnakeCaseModel:
    def method_one():
        pass
    def method_two():
        pass
    @property
    def property_one(self):
        pass

";

            private const string FullyImplementedWithPropertyAsField =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class FullyImplementedModelWithPropertyAsField:
    def method_one():
        pass
    def method_two():
        pass
    def __init__(self):
        self.property_one = 'value'
";

            private const string DerivedFromCsharp =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class DerivedFromCSharpModel(PythonWrapperTests.ValidateImplementationOf.Model):
    def MethodOne():
        pass
    @property
    def PropertyOne(self):
        return 'value'

";

            private const string MissingMethodOne =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class ModelMissingMethodOne:
    def MethodTwo():
        pass
    @property
    def PropertyOne(self):
        return 'value'
";

            private const string MissingProperty =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class ModelMissingProperty:
    def MethodOne():
        pass
    def MethodTwo():
        pass
";

            interface IModel
            {
                string PropertyOne { get; set; }
                void MethodOne();
                void MethodTwo();
            }

            public class Model : IModel
            {
                public string PropertyOne { get; set; }

                public void MethodOne()
                {
                }

                public void MethodTwo()
                {
                }
            }
        }


        [TestFixture]
        public class InvokeTests
        {
            [Test]
            public void InvokesCSharpMethod()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(InvokeTests), InvokeModule);
                    var model = module.GetAttr("PythonInvokeTestsModel").Invoke();
                    Assert.That(model.InvokeMethod<int>("AddThreeNumbers", 1, 2, 3), Is.EqualTo(6));
                }
            }

            [Test]
            public void InvokesPythonMethod()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(InvokeTests), InvokeModule);
                    var model = module.GetAttr("PythonInvokeTestsModel").Invoke();
                    Assert.That(model.InvokeMethod<int>("AddTwoNumbers", 1, 2), Is.EqualTo(3));
                }
            }

            private const string InvokeModule =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class PythonInvokeTestsModel(PythonWrapperTests.InvokeTests.InvokeTestsModel):
    def add_two_numbers(self, a, b):
        return a + b
";

            public class InvokeTestsModel
            {
                public int AddTwoNumbers(int a, int b)
                {
                    throw new NotImplementedException();
                }

                public int AddThreeNumbers(int a, int b, int c)
                {
                    return a + b + c;
                }
            }
        }
    }
}
