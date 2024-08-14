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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Orders.Fees;
using QuantConnect.Python;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public class BasePythonWrapperTests
    {
        [Test]
        public void EqualsReturnsTrueForWrapperAndUnderlyingModel()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString("EqualsReturnsTrueForWrapperAndUnderlyingModel", @"
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import BasePythonWrapperTests

class PythonDerivedTestModel(BasePythonWrapperTests.TestModel):
    pass

class PythonTestModel:
    pass
");
            var pyDerivedModel = module.GetAttr("PythonDerivedTestModel").Invoke();
            var wrapper = new BasePythonWrapper<ITestModel>(pyDerivedModel);
            var pyModel = module.GetAttr("PythonTestModel").Invoke();

            Assert.IsTrue(wrapper.Equals(pyDerivedModel));
            Assert.IsTrue(wrapper.Equals(new BasePythonWrapper<ITestModel>(pyDerivedModel)));
            Assert.IsFalse(wrapper.Equals(pyModel));
        }

        [TestFixture]
        public class RuntimeChecks
        {
            [TestFixture]
            public class InvokingMethod
            {
                public interface ITestInvokeMethodModel
                {
                    int IntReturnTypeMethod();
                }

                [Test]
                public void ThrowsWhenWhenWrongReturnType([Values] bool withValidReturnType)
                {
                    using var _ = Py.GIL();
                    using var module = PyModule.FromString(nameof(ThrowsWhenWhenWrongReturnType), @"
class PythonTestInvokeMethodModel():
    def __init__(self):
        self._return_valid_type = True

    def set_return_valid_type(self, value):
        self._return_valid_type = value

    def int_return_type_method(self):
        if self._return_valid_type:
            return 1

        # Should return a integer to properly match the interface
        return ""string""
");

                    using var pyInstance = module.GetAttr("PythonTestInvokeMethodModel").Invoke();
                    using var pyWithValidReturnType = withValidReturnType.ToPython();
                    pyInstance.GetAttr("set_return_valid_type").Invoke(pyWithValidReturnType);

                    var wrapper = new BasePythonWrapper<ITestInvokeMethodModel>(pyInstance);

                    if (withValidReturnType)
                    {
                        var result = -1;
                        Assert.DoesNotThrow(() => result = wrapper.InvokeMethod<int>("IntReturnTypeMethod"));
                        Assert.AreEqual(1, result);
                    }
                    else
                    {
                        Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethod<int>("IntReturnTypeMethod"));
                    }
                }

                [TestFixture]
                public class WithOutParameters
                {
                    public interface ITestInvokeMethodWithOutParamsModel
                    {
                        DateTime MethodWithOutParams(out int intOutParam, out string stringOutParam);
                    }

                    [Test]
                    public void ThrowsWhenWrongOutParamType([Values] bool withValidOutParamsTypes)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenWrongOutParamType), @"
from datetime import datetime

class PythonTestInvokeMethodWithOutParamsModel():
    def __init__(self):
        self._return_valid_out_param_type = True

    def set_return_valid_out_param_type(self, value):
        self._return_valid_out_param_type = value

    def method_with_out_params(self, int_out_param, string_out_param):
        if self._return_valid_out_param_type:
            int_out_param = 1
            string_out_param = 'string'
        else:
            int_out_param = 'string'    # Invalid type
            string_out_param = 'string'

        return datetime(2024, 6, 21), int_out_param, string_out_param
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodWithOutParamsModel").Invoke();
                        using var pyWithValidOutParamsTypes = withValidOutParamsTypes.ToPython();
                        pyInstance.GetAttr("set_return_valid_out_param_type").Invoke(pyWithValidOutParamsTypes);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodWithOutParamsModel>(pyInstance);

                        AssertInvoke(wrapper, withValidOutParamsTypes);
                    }

                    [Test]
                    public void ThrowsWhenWrongOutParamCount([Values] bool withValidOutParamCount)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenWrongOutParamCount), @"
from datetime import datetime

class PythonTestInvokeMethodWithOutParamsModel():
    def __init__(self):
        self._return_valid_out_params_count = True

    def set_return_valid_out_params_count(self, value):
        self._return_valid_out_params_count = value

    def method_with_out_params(self, int_out_param, string_out_param):
        int_out_param = 1
        string_out_param = 'string'
        if self._return_valid_out_params_count:
            return datetime(2024, 6, 21), int_out_param, string_out_param
        else:
            return datetime(2024, 6, 21), int_out_param
");
                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodWithOutParamsModel").Invoke();
                        using var pyWithValidOutParamCount = withValidOutParamCount.ToPython();
                        pyInstance.GetAttr("set_return_valid_out_params_count").Invoke(pyWithValidOutParamCount);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodWithOutParamsModel>(pyInstance);

                        AssertInvoke<ArgumentException>(wrapper, withValidOutParamCount);
                    }

                    [Test]
                    public void ThrowsWhenReturnedTypeIsNotATuple([Values] bool withValidReturnType)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenWrongReturnType), @"
from datetime import datetime

class PythonTestInvokeMethodWithOutParamsModel():
    def __init__(self):
        self._use_valid_return_type = True

    def set_use_valid_return_type(self, value):
        self._use_valid_return_type = value

    def method_with_out_params(self, int_out_param, string_out_param):
        int_out_param = 1
        string_out_param = 'string'
        if self._use_valid_return_type:
            return datetime(2024, 6, 21), int_out_param, string_out_param
        else:
            return 1   # Invalid return type, not a tuple
");
                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodWithOutParamsModel").Invoke();
                        using var pyWithValidOutParamCount = withValidReturnType.ToPython();
                        pyInstance.GetAttr("set_use_valid_return_type").Invoke(pyWithValidOutParamCount);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodWithOutParamsModel>(pyInstance);

                        AssertInvoke<ArgumentException>(wrapper, withValidReturnType);
                    }

                    [Test]
                    public void ThrowsWhenWrongReturnType([Values] bool withValidReturnType)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenWrongReturnType), @"
from datetime import datetime

class PythonTestInvokeMethodWithOutParamsModel():
    def __init__(self):
        self._use_valid_return_type = True

    def set_use_valid_return_type(self, value):
        self._use_valid_return_type = value

    def method_with_out_params(self, int_out_param, string_out_param):
        int_out_param = 1
        string_out_param = 'string'
        if self._use_valid_return_type:
            return datetime(2024, 6, 21), int_out_param, string_out_param
        else:
            return 1, int_out_param, string_out_param
");
                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodWithOutParamsModel").Invoke();
                        using var pyWithValidOutParamCount = withValidReturnType.ToPython();
                        pyInstance.GetAttr("set_use_valid_return_type").Invoke(pyWithValidOutParamCount);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodWithOutParamsModel>(pyInstance);

                        AssertInvoke(wrapper, withValidReturnType);
                    }

                    private static void AssertInvoke<TException>(BasePythonWrapper<ITestInvokeMethodWithOutParamsModel> wrapper, bool validCase)
                        where TException : Exception
                    {
                        var outParametersTypes = new Type[] { typeof(int), typeof(string) };
                        var intOutParameter = -1;
                        var stringOutParameter = string.Empty;

                        if (validCase)
                        {
                            var result = wrapper.InvokeMethodWithOutParameters<DateTime>("MethodWithOutParams", outParametersTypes,
                                out var outParameters, intOutParameter, stringOutParameter);
                            Assert.AreEqual(new DateTime(2024, 6, 21), result);
                            Assert.AreEqual(1, outParameters[0]);
                            Assert.AreEqual("string", outParameters[1]);
                        }
                        else
                        {
                            Assert.Throws<TException>(() => wrapper.InvokeMethodWithOutParameters<DateTime>("MethodWithOutParams",
                                outParametersTypes, out var _, intOutParameter, stringOutParameter));
                        }
                    }

                    private static void AssertInvoke(BasePythonWrapper<ITestInvokeMethodWithOutParamsModel> wrapper, bool validCase)
                    {
                        AssertInvoke<InvalidCastException>(wrapper, validCase);
                    }
                }

                [TestFixture]
                public class WithEnumerableReturnType
                {
                    public interface ITestInvokeMethodReturningIterable
                    {
                        IEnumerable<int> Range(int min, int max);
                    }

                    [Test]
                    public void ThrowsWhenReturnTypeIsNotIterable([Values] bool withValidReturnType)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenReturnTypeIsNotIterable), @"
class PythonTestInvokeMethodReturningIterable():
    def __init__(self):
        self._use_valid_return_type = True

    def set_use_valid_return_type(self, value):
        self._use_valid_return_type = value

    def range(self, min, max):
        if self._use_valid_return_type:
            return range(min, max)
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodReturningIterable").Invoke();
                        using var pyWithValidReturnType = withValidReturnType.ToPython();
                        pyInstance.GetAttr("set_use_valid_return_type").Invoke(pyWithValidReturnType);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodReturningIterable>(pyInstance);

                        if (withValidReturnType)
                        {
                            var result = wrapper.InvokeMethodAndEnumerate<int>("Range", 5, 10).ToList();
                            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9 }, result);
                        }
                        else
                        {
                            Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethodAndEnumerate<int>("Range", 5, 10).ToList());
                        }
                    }

                    [Test]
                    public void ThrowsWhenIteratorItemIsOfWrongType()
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenIteratorItemIsOfWrongType), @"
class PythonTestInvokeMethodReturningIterable():
    def range(self, min, max):
        for i in range(min, max):
            yield i
        yield 'string'
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodReturningIterable").Invoke();
                        var wrapper = new BasePythonWrapper<ITestInvokeMethodReturningIterable>(pyInstance);

                        Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethodAndEnumerate<int>("Range", 5, 10).ToList());
                    }
                }

                [TestFixture]
                public class WithDictionaryReturnType
                {
                    public interface ITestInvokeMethodReturningDictionary
                    {
                        Dictionary<Symbol, List<double>> GetDictionary();
                    }

                    [TestCase(true, false)]
                    [TestCase(true, true)]
                    [TestCase(false)]
                    public void ThrowsWhenReturnTypeIsNotDictionary(bool withValidReturnType, bool returnNone = false)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenReturnTypeIsNotDictionary), @"
from QuantConnect.Tests import Symbols

class PythonTestInvokeMethodReturningDictionary():
    def __init__(self):
        self._use_valid_return_type = True
        self._return_none = False

    def set_use_valid_return_type(self, value):
        self._use_valid_return_type = value

    def set_return_none(self, value):
        self._return_none = value

    def get_dictionary(self):
        if self._use_valid_return_type:
            if not self._return_none:
                return {
                    Symbols.SPY: [1.1, 2.2],
                    Symbols.USDJPY: [3.3, 4.4, 5.5],
                    Symbols.SPY_C_192_Feb19_2016: [6.6],
                }
            else:
                # None is a valid value for a Dictionary
                return None
        else:
            return [1, 2, 3]
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodReturningDictionary").Invoke();
                        using var pyWithValidReturnType = withValidReturnType.ToPython();
                        pyInstance.GetAttr("set_use_valid_return_type").Invoke(pyWithValidReturnType);
                        using var pyReturnNone = returnNone.ToPython();
                        pyInstance.GetAttr("set_return_none").Invoke(pyReturnNone);

                        var wrapper = new BasePythonWrapper<ITestInvokeMethodReturningDictionary>(pyInstance);

                        if (withValidReturnType)
                        {
                            var result = wrapper.InvokeMethodAndGetDictionary<Symbol, List<double>>("GetDictionary");

                            if (returnNone)
                            {
                                Assert.IsNull(result);
                            }
                            else
                            {
                                var expectedDictionary = new Dictionary<Symbol, List<double>>()
                                {
                                    { Symbols.SPY, new() { 1.1, 2.2 } },
                                    { Symbols.USDJPY, new() { 3.3, 4.4, 5.5 } },
                                    { Symbols.SPY_C_192_Feb19_2016, new() { 6.6 } },
                                };

                                Assert.IsNotNull(result);
                                Assert.AreEqual(expectedDictionary.Count, result.Count);

                                foreach (var kvp in expectedDictionary)
                                {
                                    Assert.IsTrue(result.TryGetValue(kvp.Key, out var resultValue));
                                    CollectionAssert.AreEqual(kvp.Value, resultValue);
                                }
                            }
                        }
                        else
                        {
                            Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethodAndGetDictionary<Symbol, List<double>>("GetDictionary"));
                        }
                    }

                    [Test]
                    public void ThrowsWhenDictionaryKeyIsOfWrongType()
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenDictionaryKeyIsOfWrongType), @"
from datetime import datetime
from QuantConnect.Tests import Symbols

class PythonTestInvokeMethodReturningDictionary():
    def get_dictionary(self):
        date = datetime(2024, 8, 14)
        return {
            Symbols.SPY: [1.1, 2.2],
            Symbols.USDJPY: [3.3, 4.4, 5.5],
            date: [6.6],
        }
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodReturningDictionary").Invoke();
                        var wrapper = new BasePythonWrapper<ITestInvokeMethodReturningDictionary>(pyInstance);

                        Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethodAndGetDictionary<Symbol, List<double>>("GetDictionary"));
                    }

                    [Test]
                    public void ThrowsWhenDictionaryValueIsOfWrongType()
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(ThrowsWhenDictionaryValueIsOfWrongType), @"
from QuantConnect.Tests import Symbols

class PythonTestInvokeMethodReturningDictionary():
    def get_dictionary(self):
        return {
            Symbols.SPY: [1.1, 2.2],
            Symbols.USDJPY: [3.3, 4.4, 5.5],
            Symbols.SPY_C_192_Feb19_2016: 6.6,
        }
");

                        using var pyInstance = module.GetAttr("PythonTestInvokeMethodReturningDictionary").Invoke();
                        var wrapper = new BasePythonWrapper<ITestInvokeMethodReturningDictionary>(pyInstance);

                        Assert.Throws<InvalidCastException>(() => wrapper.InvokeMethodAndGetDictionary<Symbol, List<double>>("GetDictionary"));
                    }
                }

                [TestFixture]
                public class WrappingResult
                {
                    public interface ITestModel
                    {
                        IFeeModel GetFeeModel();
                    }

                    public class TestModel : ITestModel
                    {
                        public IFeeModel GetFeeModel()
                        {
                            return new FeeModel();
                        }
                    }

                    public class TestModelPythonWrapper : BasePythonWrapper<ITestModel>
                    {
                        public TestModelPythonWrapper(PyObject pyInstance) : base(pyInstance)
                        {
                        }
                    }

                    [Test]
                    public void WrapsResult([Values] bool withWrappedResult)
                    {
                        using var _ = Py.GIL();
                        using var module = PyModule.FromString(nameof(WrapsResult), @"
from AlgorithmImports import *
from clr import AddReference
AddReference('QuantConnect.Tests')

from QuantConnect.Tests.Python import BasePythonWrapperTests

class PythonFeeModel(FeeModel):
    pass

class PythonTestModel(BasePythonWrapperTests.RuntimeChecks.InvokingMethod.WrappingResult.TestModel):
    def __init__(self):
        self._use_wrapped_result = True

    def set_use_wrapped_result(self, value):
        self._use_wrapped_result = value

    def get_fee_model(self):
        if self._use_wrapped_result:
            return PythonFeeModel()

        return FeeModel()
");

                        using var pyInstance = module.GetAttr("PythonTestModel").Invoke();
                        using var pyWithWrappedResult = withWrappedResult.ToPython();
                        pyInstance.GetAttr("set_use_wrapped_result").Invoke(pyWithWrappedResult);

                        var wrapper = new TestModelPythonWrapper(pyInstance);
                        var wrappingFunctionCalled = false;
                        var feeModel = wrapper.InvokeMethodAndWrapResult<IFeeModel>("GetFeeModel", (pyInstance) =>
                        {
                            wrappingFunctionCalled = true;
                            return new FeeModelPythonWrapper(pyInstance);
                        });

                        if (withWrappedResult)
                        {
                            Assert.IsTrue(wrappingFunctionCalled);
                            Assert.IsInstanceOf<FeeModelPythonWrapper>(feeModel);
                        }
                        else
                        {
                            Assert.IsFalse(wrappingFunctionCalled);
                            Assert.IsInstanceOf<FeeModel>(feeModel);
                            Assert.IsNotInstanceOf<FeeModelPythonWrapper>(feeModel);
                        }
                    }
                }
            }
        }

        public interface ITestModel
        {
        }

        public class TestModel : ITestModel
        {
        }
    }
}
