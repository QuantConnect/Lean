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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmGetParameterTests
    {
        [TestCase(Language.CSharp, "numeric_parameter", 2, false)]
        [TestCase(Language.CSharp, "not_a_parameter", 2, true)]
        [TestCase(Language.CSharp, "string_parameter", 2, true)]
        [TestCase(Language.Python, "numeric_parameter", 2, false)]
        [TestCase(Language.Python, "not_a_parameter", 2, true)]
        [TestCase(Language.Python, "string_parameter", 2, true)]
        public void GetParameterConvertsToNumericTypes(
            Language language,
            string parameterName,
            int defaultValue,
            bool shouldReturnDefaultValue
        )
        {
            var parameters = new Dictionary<string, string>
            {
                { "numeric_parameter", "1" },
                { "string_parameter", "string value" },
            };
            var doubleDefaultValue = Convert.ToDouble(defaultValue);
            var decimalDefaultValue = Convert.ToDecimal(defaultValue);

            if (language == Language.CSharp)
            {
                var algorithm = new QCAlgorithm();
                algorithm.SetParameters(parameters);

                var intValue = algorithm.GetParameter(parameterName, defaultValue);
                var doubleValue = algorithm.GetParameter(parameterName, doubleDefaultValue);
                var decimalValue = algorithm.GetParameter(parameterName, decimalDefaultValue);

                Assert.AreEqual(typeof(int), intValue.GetType());
                Assert.AreEqual(typeof(double), doubleValue.GetType());
                Assert.AreEqual(typeof(decimal), decimalValue.GetType());

                // If the parameter is not found or is not numeric, the default value should be returned
                if (
                    !shouldReturnDefaultValue
                    && parameters.TryGetValue(parameterName, out var parameterValue)
                )
                {
                    Assert.AreEqual(int.Parse(parameterValue), intValue);
                    Assert.AreEqual(double.Parse(parameterValue), doubleValue);
                    Assert.AreEqual(decimal.Parse(parameterValue), decimalValue);
                }
                else
                {
                    Assert.AreEqual(defaultValue, intValue);
                    Assert.AreEqual(doubleDefaultValue, doubleValue);
                    Assert.AreEqual(decimalDefaultValue, decimalValue);
                }
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString(
                        "testModule",
                        @"
from AlgorithmImports import *

def getAlgorithm():
    return QCAlgorithm()

def isInt(value):
    return isinstance(value, int)

def isFloat(value):
    return isinstance(value, float)
        "
                    );

                    var getAlgorithm = testModule.GetAttr("getAlgorithm");
                    var algorithm = getAlgorithm.Invoke();
                    algorithm.GetAttr("SetParameters").Invoke(PyDict.FromManagedObject(parameters));

                    var intValue = algorithm
                        .GetAttr("GetParameter")
                        .Invoke(parameterName.ToPython(), defaultValue.ToPython());
                    var doubleValue = algorithm
                        .GetAttr("GetParameter")
                        .Invoke(parameterName.ToPython(), doubleDefaultValue.ToPython());
                    var decimalValue = algorithm
                        .GetAttr("GetParameter")
                        .Invoke(parameterName.ToPython(), decimalDefaultValue.ToPython());

                    Assert.IsTrue(
                        testModule.GetAttr("isInt").Invoke(intValue).As<bool>(),
                        $"Expected 'intValue' to be of type int but was {intValue.GetPythonType().ToString()} instead"
                    );
                    Assert.IsTrue(
                        testModule.GetAttr("isFloat").Invoke(doubleValue).As<bool>(),
                        $"Expected 'doubleValue' to be of type float but was {doubleValue.GetPythonType().ToString()} instead"
                    );
                    Assert.IsTrue(
                        testModule.GetAttr("isFloat").Invoke(decimalValue).As<bool>(),
                        $"Expected 'decimalValue' to be of type float but was {decimalValue.GetPythonType().ToString()} instead"
                    );

                    // If the parameter is not found or is not numeric, the default value should be returned
                    if (
                        !shouldReturnDefaultValue
                        && parameters.TryGetValue(parameterName, out var parameterValue)
                    )
                    {
                        Assert.AreEqual(int.Parse(parameterValue), intValue.As<int>());
                        Assert.AreEqual(double.Parse(parameterValue), doubleValue.As<double>());
                        Assert.AreEqual(decimal.Parse(parameterValue), decimalValue.As<decimal>());
                    }
                    else
                    {
                        Assert.AreEqual(defaultValue, intValue.As<int>());
                        Assert.AreEqual(doubleDefaultValue, doubleValue.As<double>());
                        Assert.AreEqual(decimalDefaultValue, decimalValue.As<decimal>());
                    }
                }
            }
        }

        [TestCase(Language.CSharp, "numeric_parameter")]
        [TestCase(Language.CSharp, "string_parameter")]
        [TestCase(Language.CSharp, "not_a_parameter")]
        [TestCase(Language.Python, "numeric_parameter")]
        [TestCase(Language.Python, "string_parameter")]
        [TestCase(Language.Python, "not_a_parameter")]
        public void GetsParameterWithoutADefaultValue(Language language, string parameterName)
        {
            var parameters = new Dictionary<string, string>
            {
                { "numeric_parameter", "1" },
                { "string_parameter", "string value" },
            };

            if (language == Language.CSharp)
            {
                var algorithm = new QCAlgorithm();
                algorithm.SetParameters(parameters);
                var parameterWithoutDefault = algorithm.GetParameter(parameterName);
                var parameterWithNullDefault = algorithm.GetParameter(parameterName, null);

                if (parameters.TryGetValue(parameterName, out var parameterValue))
                {
                    Assert.AreEqual(typeof(string), parameterWithoutDefault.GetType());
                    Assert.AreEqual(typeof(string), parameterWithNullDefault.GetType());
                    Assert.AreEqual(parameterValue, parameterWithoutDefault);
                    Assert.AreEqual(parameterValue, parameterWithNullDefault);
                }
                else
                {
                    Assert.IsNull(parameterWithoutDefault);
                    Assert.IsNull(parameterWithNullDefault);
                }
            }
            else
            {
                using (Py.GIL())
                {
                    var testModule = PyModule.FromString(
                        "testModule",
                        @"
from AlgorithmImports import *

def getAlgorithm():
    return QCAlgorithm()

def isString(value):
    return isinstance(value, str)
        "
                    );

                    dynamic getAlgorithm = testModule.GetAttr("getAlgorithm");
                    dynamic algorithm = getAlgorithm();
                    algorithm.SetParameters(PyDict.FromManagedObject(parameters));
                    dynamic parameterWithoutDefault = algorithm.GetParameter(
                        parameterName.ToPython()
                    );
                    dynamic parameterWithNullDefault = algorithm.GetParameter(
                        parameterName.ToPython(),
                        null
                    );

                    if (parameters.TryGetValue(parameterName, out var parameterValue))
                    {
                        dynamic isString = testModule.GetAttr("isString");
                        Assert.IsTrue(
                            isString(parameterWithoutDefault).As<bool>(),
                            $"Expected 'parameterWithoutDefault' to be of type string but was {parameterWithoutDefault.GetPythonType().ToString()} instead"
                        );
                        Assert.IsTrue(
                            isString(parameterWithNullDefault).As<bool>(),
                            $"Expected 'parameterWithNullDefault' to be of type string but was {parameterWithNullDefault.GetPythonType().ToString()} instead"
                        );
                        Assert.AreEqual(parameterValue, parameterWithoutDefault.As<string>());
                        Assert.AreEqual(parameterValue, parameterWithNullDefault.As<string>());
                    }
                    else
                    {
                        Assert.IsNull(parameterWithoutDefault);
                        Assert.IsNull(parameterWithNullDefault);
                    }
                }
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetParameterCannotConvertoToNumberFromNonNumericValues(Language language)
        {
            var parameters = new Dictionary<string, string>
            {
                { "date_parameter", "2022-8-8" },
                { "string_parameter", "string value" },
            };
            int defaultInt = 0;
            double defaultDouble = 0;
            decimal defaultDecimal = 0;
            int intValue;
            double doubleValue;
            decimal decimalValue;

            foreach (var parameterName in parameters.Keys)
            {
                if (language == Language.CSharp)
                {
                    var algorithm = new QCAlgorithm();
                    algorithm.SetParameters(parameters);
                    intValue = algorithm.GetParameter(parameterName, defaultInt);
                    doubleValue = algorithm.GetParameter(parameterName, defaultDouble);
                    decimalValue = algorithm.GetParameter(parameterName, defaultDecimal);
                }
                else
                {
                    using (Py.GIL())
                    {
                        var testModule = PyModule.FromString(
                            "testModule",
                            @"
from AlgorithmImports import *

def getAlgorithm():
    return QCAlgorithm()
            "
                        );

                        var getAlgorithm = testModule.GetAttr("getAlgorithm");
                        dynamic algorithm = getAlgorithm.Invoke();
                        algorithm
                            .GetAttr("SetParameters")
                            .Invoke(PyDict.FromManagedObject(parameters));
                        intValue = algorithm
                            .GetParameter(parameterName.ToPython(), defaultInt.ToPython())
                            .As<int>();
                        doubleValue = algorithm
                            .GetParameter(parameterName.ToPython(), defaultDouble.ToPython())
                            .As<double>();
                        decimalValue = algorithm
                            .GetParameter(parameterName.ToPython(), defaultDecimal.ToPython())
                            .As<decimal>();
                    }
                }

                Assert.AreEqual(
                    defaultInt,
                    intValue,
                    $"Expected '{parameterName}' to be {defaultInt} but was {intValue} instead"
                );
                Assert.AreEqual(
                    defaultDouble,
                    doubleValue,
                    $"Expected '{parameterName}' to be {defaultDouble} but was {doubleValue} instead"
                );
                Assert.AreEqual(
                    defaultDecimal,
                    decimalValue,
                    $"Expected '{parameterName}' to be {defaultDecimal} but was {decimalValue} instead"
                );
            }
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void GetParameterConvertsToFloatingPointTypesFromDifferentFormats(Language language)
        {
            var parameters = new Dictionary<string, string>
            {
                { "int_parameter", "1" },
                { "float_parameter", "1.0" },
                { "comma_parameter", "1,234.56" },
                { "scientific_parameter", "-1.643e6" },
            };
            var expectedDoubles = new Dictionary<string, double>
            {
                { "int_parameter", 1.0 },
                { "float_parameter", 1.0 },
                { "comma_parameter", 1234.56 },
                { "scientific_parameter", -1643000.0 },
            };
            var expectedDecimals = new Dictionary<string, decimal>
            {
                { "int_parameter", 1m },
                { "float_parameter", 1m },
                { "comma_parameter", 1234.56m },
                { "scientific_parameter", -1643000m },
            };
            double defaultDouble = 0;
            decimal defaultDecimal = 0;
            double doubleValue;
            decimal decimalValue;

            foreach (var parameterName in parameters.Keys)
            {
                if (language == Language.CSharp)
                {
                    var algorithm = new QCAlgorithm();
                    algorithm.SetParameters(parameters);
                    doubleValue = algorithm.GetParameter(parameterName, defaultDouble);
                    decimalValue = algorithm.GetParameter(parameterName, defaultDecimal);
                }
                else
                {
                    using (Py.GIL())
                    {
                        var testModule = PyModule.FromString(
                            "testModule",
                            @"
from AlgorithmImports import *

def getAlgorithm():
    return QCAlgorithm()
            "
                        );

                        var getAlgorithm = testModule.GetAttr("getAlgorithm");
                        dynamic algorithm = getAlgorithm.Invoke();
                        algorithm
                            .GetAttr("SetParameters")
                            .Invoke(PyDict.FromManagedObject(parameters));
                        doubleValue = algorithm
                            .GetParameter(parameterName.ToPython(), defaultDouble.ToPython())
                            .As<double>();
                        decimalValue = algorithm
                            .GetParameter(parameterName.ToPython(), defaultDecimal.ToPython())
                            .As<decimal>();
                    }
                }

                Assert.AreEqual(
                    expectedDoubles[parameterName],
                    doubleValue,
                    $"Expected '{parameterName}' to be {expectedDoubles[parameterName]} but was {doubleValue} instead"
                );
                Assert.AreEqual(
                    expectedDecimals[parameterName],
                    decimalValue,
                    $"Expected '{parameterName}' to be {expectedDecimals[parameterName]} but was {decimalValue} instead"
                );
            }
        }
    }
}
