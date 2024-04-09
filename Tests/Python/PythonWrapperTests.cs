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

namespace QuantConnect.Tests.Python
{
    public static class PythonWrapperTests
    {
        [TestFixture]
        public class ValidateImplementationOf
        {
            [Test]
            public void ThrowsOnMissingMember()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), MissingMethodOne);
                    var model = module.GetAttr("ModelMissingMethodOne");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws
                        .Exception.InstanceOf<NotImplementedException>().With.Message.Contains("MethodOne"));
                }
            }

            [Test]
            public void DoesNotThrowWhenInterfaceFullyImplemented()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), FullyImplemented);
                    var model = module.GetAttr("FullyImplementedModel");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws.Nothing);
                }
            }

            [Test]
            public void DoesNotThrowWhenInterfaceFullyImplementedSnakeCaseStyle()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), FullyImplementedSnakeCase);
                    var model = module.GetAttr("FullyImplementedSnakeCaseModel");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws.Nothing);
                }
            }

            [Test]
            public void DoesNotThrowWhenDerivedFromCSharpModel()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), DerivedFromCsharp);
                    var model = module.GetAttr("DerivedFromCSharpModel");
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
                        {"OrderListHash", "0c0f9328786b0c9e8f88d271673d16c3"}
                    },
                    Language.Python,
                    AlgorithmStatus.Completed,
                    algorithmLocation: "../../../Algorithm.Python/PEP8StyleBasicAlgorithm.py"
                );
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

";

            private const string DerivedFromCsharp =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class DerivedFromCSharpModel(PythonWrapperTests.ValidateImplementationOf.Model):
    def MethodOne():
        pass

";

            private const string MissingMethodOne =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class ModelMissingMethodOne:
    def MethodTwo():
        pass

";

            interface IModel
            {
                void MethodOne();
                void MethodTwo();
            }

            public class Model : IModel
            {
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
                    Assert.That(model.Invoke<int>("AddThreeNumbers", 1, 2, 3), Is.EqualTo(6));
                }
            }

            [Test]
            public void InvokesPythonMethod()
            {
                using (Py.GIL())
                {
                    var module = PyModule.FromString(nameof(InvokeTests), InvokeModule);
                    var model = module.GetAttr("PythonInvokeTestsModel").Invoke();
                    Assert.That(model.Invoke<int>("AddTwoNumbers", 1, 2), Is.EqualTo(3));
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
