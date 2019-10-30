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

using System.Collections.Generic;
using NUnit.Framework;
using Python.Runtime;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class PythonCustomDataTests
    {
        [Test]
        public void IsSparseDataDefaultValue()
        {
            dynamic instance;
            using (Py.GIL())
            {
                PyObject test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""System"")
AddReference(""QuantConnect.Common"")

from QuantConnect import *
from QuantConnect.Python import *

class Test(PythonData):
    def Pepe(self):
        return 1").GetAttr("Test");
                instance = test.CreateType().GetBaseDataInstance();
                instance.Symbol = Symbol.CreateBase(typeof(decimal), Symbols.SPY, QuantConnect.Market.USA);
            }

            Assert.IsTrue(instance.IsSparseData());
        }

        [TestCase("True", true)]
        [TestCase("False", false)]
        public void OverridesIsSparseData(string value, bool booleanValue)
        {
            dynamic instance;
            using (Py.GIL())
            {
                PyObject test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""System"")
AddReference(""QuantConnect.Common"")

from QuantConnect import *
from QuantConnect.Python import *

class Test(PythonData):
    def IsSparseData(self):
        return " + $"{value}").GetAttr("Test");
                instance = test.CreateType().GetBaseDataInstance();
            }

            Assert.AreEqual(booleanValue, instance.IsSparseData());
        }

        [Test]
        public void OverridesDefaultResolution()
        {
            dynamic instance;
            using (Py.GIL())
            {
                PyObject test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""System"")
AddReference(""QuantConnect.Common"")

from QuantConnect import *
from QuantConnect.Python import *

class Test(PythonData):
    def DefaultResolution(self):
        return Resolution.Tick").GetAttr("Test");
                instance = test.CreateType().GetBaseDataInstance();
            }

            Assert.AreEqual(Resolution.Tick, instance.DefaultResolution());
        }

        [Test]
        public void OverridesSupportedResolutions()
        {
            dynamic instance;
            using (Py.GIL())
            {
                PyObject test = PythonEngine.ModuleFromString("testModule",
                    @"
from clr import AddReference
AddReference(""System"")
AddReference(""QuantConnect.Common"")

from QuantConnect import *
from QuantConnect.Python import *

class Test(PythonData):
    def SupportedResolutions(self):
        return [ Resolution.Tick, Resolution.Daily ]").GetAttr("Test");
                instance = test.CreateType().GetBaseDataInstance();
            }

            var res = instance.SupportedResolutions();
            Assert.AreEqual(new List<Resolution> { Resolution.Tick, Resolution.Daily }, res);
        }
    }
}
