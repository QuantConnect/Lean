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
                    var module = PyModule.FromString(nameof(ValidateImplementationOf), MissingMethod1);
                    var model = module.GetAttr("ModelMissingMethod1");
                    Assert.That(() => model.ValidateImplementationOf<IModel>(), Throws
                        .Exception.InstanceOf<NotImplementedException>().With.Message.Contains("Method1"));
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
                    {"Total Trades", "0"},
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
                algorithmLocation: "../../../Algorithm.Python/CustomSettlementModelRegressionAlgorithm.py");
            }

            [Test]
            public void BenchmarkModelPythonWrapperWorks()
            {
                var results = AlgorithmRunner.RunLocalBacktest("CustomBenchmarkRegressionAlgorithm",
                new Dictionary<string, string>()
                {
                    {"Total Trades", "0"},
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
                algorithmLocation: "../../../Algorithm.Python/CustomBenchmarkRegressionAlgorithm.py");
            }

            private const string FullyImplemented =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class FullyImplementedModel:
    def Method1():
        pass
    def Method2():
        pass

";

            private const string DerivedFromCsharp =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class DerivedFromCSharpModel(PythonWrapperTests.ValidateImplementationOf.Model):
    def Method1():
        pass

";

            private const string MissingMethod1 =
                @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import *

class ModelMissingMethod1:
    def Method2():
        pass

";

            interface IModel
            {
                void Method1();
                void Method2();
            }

            public class Model : IModel
            {
                public void Method1()
                {
                }

                public void Method2()
                {
                }
            }
        }
    }
}
