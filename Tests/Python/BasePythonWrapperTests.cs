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
using QuantConnect.Python;

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

        public interface ITestModel
        {
        }

        public class TestModel : ITestModel
        {
        }
    }
}
